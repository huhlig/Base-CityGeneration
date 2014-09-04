﻿using System;
using System.Collections.Generic;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;
using EpimetheusPlugins.Services.CSG;
using Microsoft.Xna.Framework;
using Myre;
using Myre.Collections;
using Myre.Extensions;

namespace Base_CityGeneration.Elements.Building.Facades
{
    /// <summary>
    /// A facade is a section of wall divided into sections along it's (horizontal) length
    /// </summary>
    [Script("3AB052C6-D54C-4AE0-905E-9C99FAC9C0F7", "Base Facade")]
    public class BaseFacade
        : ProceduralScript, IFacade
    {
        public Walls.Section Section { get; set; }

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return bounds.Footprint.Length == 4;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            var facade = CreateFacade(bounds, geometry, hierarchicalParameters);

            var stamps = EmbossingStamps(hierarchicalParameters, Section.Width, bounds.Height);
            foreach (var stamp in stamps)
            {
                var brush = ConvertStampToBrush(Section, stamp, geometry, hierarchicalParameters);
                if (brush == null)
                    continue;

                facade = stamp.Additive ? facade.Union(brush) : facade.Subtract(brush);
            }

            geometry.Union(facade);
        }

        /// <summary>
        /// Given a wall section and a stamp, convert it into a brush which will apply the stamp to the wall
        /// </summary>
        /// <param name="section"></param>
        /// <param name="stamp"></param>
        /// <param name="geometry"></param>
        /// <param name="hierarchicalParameters"></param>
        /// <returns></returns>
        private static ICsgShape ConvertStampToBrush(Walls.Section section, Stamp stamp, ICsgFactory geometry, INamedDataCollection hierarchicalParameters)
        {
            var material = stamp.Material ?? hierarchicalParameters.GetValue(new TypedName<string>("material"));

            //Establish basis vectors
            var vOut = section.Normal;
            var vLeft = vOut.Perpendicular();

            //Calculate absolute thickness of stamp
            var thickness = (stamp.EndDepth - stamp.StartDepth) * section.Thickness;

            //Zero thickness stamp does nothing, early exit with null
            if (Math.Abs(thickness - 0) < float.Epsilon)
                return null;

            //Calculate transform from prism lying flat in plane to vertically aligned in plane of facade
            var transform = Matrix.CreateTranslation(0, -(section.Thickness - thickness) / 2 + stamp.StartDepth * section.Thickness, 0) *
                            Matrix.CreateRotationX(-MathHelper.PiOver2) *
                            new Matrix { Forward = new Vector3(vOut.X, 0, vOut.Y), Left = new Vector3(vLeft.X, 0, vLeft.Y), Up = Vector3.Up, M44 = 1 };

            //create brush
            return geometry.CreatePrism(material, stamp.Shape, thickness).Transform(transform);
        }

        /// <summary>
        /// Create the base block for this facade (which embossing stamps will be applied to)
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="geometry"></param>
        /// <param name="hierarchicalParameters"></param>
        /// <returns></returns>
        protected virtual ICsgShape CreateFacade(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            return geometry.CreatePrism(hierarchicalParameters.GetValue(new TypedName<string>("material")), bounds.Footprint, bounds.Height);
        }

        /// <summary>
        /// Get the set of stamps to be applied to the surface of this facade
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<Stamp> EmbossingStamps(INamedDataCollection hierarchicalParameters, float width, float height)
        {
            yield break;
        }

        /// <summary>
        /// A shape pressed into or popped out of a surface
        /// </summary>
        public struct Stamp
        {
            /// <summary>
            /// The shape of this stamp in the plane. 0,0 indicates the center, negative values are to the left and down, positive values are to the right and up
            /// </summary>
            public readonly Vector2[] Shape;

            /// <summary>
            /// The depth into the facade to start the emboss. 0 indicate the inside, 1 indicates the outside
            /// </summary>
            public readonly float StartDepth;

            /// <summary>
            /// The depth into the facade to end the emboss. 0 indicate the inside, 1 indicates the outside
            /// </summary>
            public readonly float EndDepth;

            /// <summary>
            /// If true, this emboss will add material, if false this emboss will cut material away
            /// </summary>
            public readonly bool Additive;

            /// <summary>
            /// The material of this stamp
            /// </summary>
            public readonly string Material;

            /// <summary>
            /// Create a new embossing stamp
            /// </summary>
            /// <param name="startDepth">The depth into the facade to start the emboss. 0 indicate the inside, 1 indicates the outside</param>
            /// <param name="endDepth">The depth into the facade to end the emboss. 0 indicate the inside, 1 indicates the outside</param>
            /// <param name="additive">If true, this emboss will add material, if false this emboss will cut material away</param>
            /// <param name="material">The material of this stamp</param>
            /// <param name="shape">The shape of this stamp in the plane. 0,0 indicates the center, negative values are to the left and down, positive values are to the right and up</param>
            public Stamp(float startDepth, float endDepth, bool additive, string material, params Vector2[] shape)
            {
                StartDepth = startDepth;
                EndDepth = endDepth;
                Additive = additive;
                Material = material;
                Shape = shape;
            }
        }
    }
}