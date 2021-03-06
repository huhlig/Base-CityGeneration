﻿//using System;
//using System.Collections.Generic;
//using System.Diagnostics.Contracts;
//using System.Linq;
//using Base_CityGeneration.Datastructures.HalfEdge;
//using Base_CityGeneration.Elements.Generic;
//using Base_CityGeneration.Styles;
//using EpimetheusPlugins.Procedural;
//using EpimetheusPlugins.Procedural.Utilities;
//using EpimetheusPlugins.Scripts;
//using System.Numerics;
//using Base_CityGeneration.Geometry.Walls;
//using Myre.Collections;
//using SwizzleMyVectors;
//using SwizzleMyVectors.Geometry;

//namespace Base_CityGeneration.Elements.Roads
//{
//    [Script("0C714177-06D9-4E20-8028-FA0CE6519892", "Basic Road")]
//    public class BasicRoad
//        : ProceduralScript, IRoad
//    {
//        public float GroundHeight { get; set; }

//        public HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> HalfEdge { get; set; }

//        public float Width { get; set; }

//        public override bool Accept(Prism bounds, INamedDataProvider parameters)
//        {
//            return true;
//        }

//        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
//        {
//            //Create a flat plane filling the bounds of this area
//            this.CreateFlatPlane(geometry, "tarmac", bounds.Footprint, 1, -1);

//            //Add in footpaths
//            CreateFootpaths(bounds, geometry, hierarchicalParameters, hierarchicalParameters.RoadSidewalkMaterial(Random), HierarchicalParameters.RoadSidewalkHeight(Random));
//        }

//        private void CreateFootpaths(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters, string material, float height)
//        {
//            Contract.Requires(geometry != null);
//            Contract.Requires(hierarchicalParameters != null);

//            //Get a builder for this edge
//            var builder = HalfEdge.BuilderEndingWith(HalfEdge.EndVertex);

//            //Extract sections of sidewalk width into this shape
//            Vector2[] inner;
//            var sections = bounds.Footprint.Sections(builder.SidewalkWidth, out inner).ToArray();
//            var corners = sections.Corners(bounds.Footprint, inner);

//            //builder data is in world space, transform it back to node-local space
//            //the transform is in 3D space, extend out vector2s up to 3D space (X,0,Y) then transform then come back to v2 space (XZ)
//            var leftEnd = Vector3.Transform(builder.LeftEnd.X_Y(0), InverseWorldTransformation).XZ();
//            var rightEnd = Vector3.Transform(builder.RightEnd.X_Y(0), InverseWorldTransformation).XZ();
//            var leftStart = Vector3.Transform(builder.LeftStart.X_Y(0), InverseWorldTransformation).XZ();
//            var rightStart = Vector3.Transform(builder.RightStart.X_Y(0), InverseWorldTransformation).XZ();

//            //Create geometry for edge sections
//            MaterializeEdgeSections(geometry, material, height, sections, builder, leftEnd, rightEnd, leftStart, rightStart);

//            //Create geometry for corner sections
//            MaterializeCornerSections(geometry, material, height, corners, builder, rightEnd, leftEnd, leftStart, rightStart);
//        }

//        private void MaterializeCornerSections(ISubdivisionGeometry geometry, string material, float height, IReadOnlyList<IReadOnlyList<Vector2>> corners, IHalfEdgeBuilder builder, Vector2 rightEnd, Vector2 leftEnd, Vector2 leftStart, Vector2 rightStart)
//        {
//            Contract.Requires(geometry != null);
//            Contract.Requires(corners != null);
//            Contract.Requires(builder != null);

//            foreach (var corner in corners)
//            {
//                //Skip non corner sections
//                if (corner == null)
//                    continue;

//                //which vertex is this corner bordering onto?
//                Vector2 endPointA;
//                Vector2 endPointB;
//                var vertex = DetermineBorderingVertex(corner, builder, rightEnd, leftEnd, leftStart, rightStart, out endPointA, out endPointB);

//                //Determine what to do based on junction type
//                var count = vertex.Edges.Count();
//                switch (count)
//                {
//                    case 0: {
//                        throw new InvalidOperationException("Junction has 0 connecting roads");
//                    }

//                    //Dead end, place the corners
//                    case 1: {
//                        MaterializeSection(geometry, material, corner, height);
//                        break;
//                    }

//                    default: {
//                        //End line of road segment
//                        var lEnd = new Ray2(endPointA, Vector2.Normalize(endPointB - endPointA));

//                        //Direction along footpath
//                        var cDist = Math.Abs(lEnd.DistanceToPoint(corner.C));
//                        var aDist = Math.Abs(lEnd.DistanceToPoint(corner.A));
//                        Vector2 alongFootpath = builder.Direction;

//                        //side lines of footpath
//                        var lInner = new Ray2(corner.D, alongFootpath);

//                        //end point of path (projected out from corner point, perpenducular to direction - this ensures path end is square)
//                        var intersect = lInner.Intersects(new Ray2(corner.B, builder.Direction.Perpendicular()));
//                        if (!intersect.HasValue)
//                            break;

//                        Vector2[] shape = new Vector2[4] {
//                            corner.B,
//                            corner.D,
//                            intersect.Value.Position,
//                            aDist < cDist ? corner.C : corner.A
//                        };

//                        MaterializeSection(geometry, material, shape, height);
//                        break;
//                    }
//                }
//            }
//        }

//        private static Vertex<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> DetermineBorderingVertex(Section section, IHalfEdgeBuilder builder, Vector2 rightEnd, Vector2 leftEnd, Vector2 leftStart, Vector2 rightStart, out Vector2 endPointA, out Vector2 endPointB)
//        {
//            Contract.Requires(builder != null);
//            Contract.Ensures(Contract.Result<Vertex<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder>>() != null);

//            //Point B is the corner point, which of the corners is it?
//            Vertex<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> vertex;

//            if (Vector2.DistanceSquared(section.B, rightEnd) < 0.01f)
//            {
//                vertex = builder.HalfEdge.EndVertex;
//                endPointA = leftEnd;
//                endPointB = rightEnd;
//            }
//            else if (Vector2.DistanceSquared(section.B, leftEnd) < 0.01f)
//            {
//                vertex = builder.HalfEdge.EndVertex;
//                endPointA = leftEnd;
//                endPointB = rightEnd;
//            }
//            else if (Vector2.DistanceSquared(section.B, rightStart) < 0.01f)
//            {
//                vertex = builder.HalfEdge.Pair.EndVertex;
//                endPointA = leftStart;
//                endPointB = rightStart;
//            }
//            else if (Vector2.DistanceSquared(section.B, leftStart) < 0.01f)
//            {
//                vertex = builder.HalfEdge.Pair.EndVertex;
//                endPointA = leftStart;
//                endPointB = rightStart;
//            }
//            else
//            {
//                throw new InvalidOperationException("Footpath corner is not in any of the corners of the road section");
//            }

//            return vertex;
//        }

//        private void MaterializeEdgeSections(ISubdivisionGeometry geometry, string material, float height, IEnumerable<Section> sections, IHalfEdgeBuilder builder, Vector2 leftEnd, Vector2 rightEnd, Vector2 leftStart, Vector2 rightStart)
//        {
//            Contract.Requires(geometry != null);
//            Contract.Requires(sections != null);
//            Contract.Requires(builder != null);

//            //Cache these checks
//            var deadEnd = builder.HalfEdge.EndVertex.Edges.Count() == 1;
//            var deadStart = builder.HalfEdge.Pair.EndVertex.Edges.Count() == 1;

//            foreach (var section in sections)
//            {
//                //Skip over all corner sections
//                if (section.IsCorner)
//                    continue;

//                //If this footpath is along the side of the road
//                //If this footpath is along an end, and this end is a dead end (junction has one road)
//                if (Math.Abs(Vector2.Dot(section.Along, builder.Direction)) > 0.99f ||
//                    (deadEnd && IsAlongLineSegment(section, new LineSegment2(leftEnd, rightEnd))) ||
//                    (deadStart && IsAlongLineSegment(section, new LineSegment2(leftStart, rightStart)))
//                )
//                {
//                    MaterializeSection(geometry, material, section, height);
//                }
//            }
//        }

//        private static bool IsAlongLineSegment(Section section, LineSegment2 segment)
//        {
//            //Check if this section points in the same (or opposite) direction
//            if (!(Math.Abs(Vector2.Dot(section.Along, segment.Line.Direction)) > 0.99f))
//                return false;

//            //Check if the section is actually on the line
//            //C and D are the outside points of the section, so they should lie on the line at the edge of the road
//            if (segment.DistanceToPoint(section.C) > 0.01f)
//                return false;
//            if (segment.DistanceToPoint(section.D) > 0.01f)
//                return false;

//            return true;
//        }

//        private void MaterializeSection(ISubdivisionGeometry geometry, string material, Section section, float height)
//        {
//            Contract.Requires(geometry != null);

//            MaterializeSection(geometry, material, new Vector2[] {section.A, section.B, section.C, section.D}, height);
//        }

//        private void MaterializeSection(ISubdivisionGeometry geometry, string material, IEnumerable<Vector2> shape, float height)
//        {
//            Contract.Requires(geometry != null);
//            Contract.Requires(shape != null);

//            this.CreateFlatPlane(geometry, material, shape.ConvexHull().ToArray(), height);
//        }
//    }
//}
