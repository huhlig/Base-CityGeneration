﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using ClipperLib;
using EpimetheusPlugins.Procedural.Utilities;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Plan.Geometric
{
    /// <summary>
    /// Represents an enclosed area of space with rooms. Has operations for adding rooms and querying neighbourhood relationships between rooms
    /// </summary>
    public class GeometricFloorplan
        : IFloorPlanBuilder
    {
        #region fields/properties
        private const float SCALE = 100000;
        internal const float SAFE_DISTANCE = 0.01f;

        private uint _nextId;

        private bool _isFrozen;
        private readonly Clipper _clipper = new Clipper();

        private readonly IReadOnlyList<Vector2> _externalFootprint;
        public IReadOnlyList<Vector2> ExternalFootprint
        {
            get
            {
                
                return _externalFootprint;
            }
        }

        private readonly List<IRoomPlan> _rooms = new List<IRoomPlan>();
        public IEnumerable<IRoomPlan> Rooms
        {
            get
            {
                return _rooms;
            }
        }

        private readonly NeighbourCalculator _neighbourhood;
        #endregion

        public GeometricFloorplan(IReadOnlyList<Vector2> footprint)
        {
            Contract.Requires(footprint != null);
            Contract.Requires(footprint.Count >= 3);

            _externalFootprint = footprint;

            _neighbourhood = new NeighbourCalculator(this);
        }

        [ContractInvariantMethod]
        private void ObjectInvariants()
        {
            Contract.Invariant(_clipper != null);
        }

        /// <summary>
        /// Freeze the floorplan builder (making it immutable).
        /// </summary>
        /// <returns>An immutable view of this floorplan</returns>
        public IFloorPlan Freeze()
        {
            _isFrozen = true;

            return this;
        }

        /// <summary>
        /// Calculate what shape would be created if you tried to add the given room to the plan
        /// </summary>
        /// <param name="roomFootprint"></param>
        /// <param name="split"></param>
        /// <returns></returns>
        public IReadOnlyList<IReadOnlyList<Vector2>> TestRoom(IEnumerable<Vector2> roomFootprint, bool split = false)
        {
            //Generate shapes for this room footprint, early exit if null
            var solution = ShapesForRoom(roomFootprint, split);
            if (solution == null)
                return Array.Empty<Vector2[]>();

            //Convert shapes into vector2 shapes (scale properly)
            return solution;
        }

        private IReadOnlyList<IReadOnlyList<Vector2>> ShapesForRoom(IEnumerable<Vector2> roomFootprint, bool split = false)
        {
            Contract.Requires(roomFootprint != null);

            //Winding check
            var clipperRoomFootprint = roomFootprint.Select(ToPoint).ToList();
            if (Clipper.Orientation(clipperRoomFootprint))
                clipperRoomFootprint.Reverse();

            //Contain within floor outer edge
            var solution = ClipToFloor(clipperRoomFootprint, split);
            if (solution == null)
                return null;

            //Clip against other rooms
            if (_rooms.Count > 0)
            {
                var clips = solution.Select(a => ClipToRooms(a, split));

                solution = clips.SelectMany(a => a).ToList();
                if (solution.Count > 1 && !split)
                    return null;
            }

            //Ensure shapes are still clockwise wound (mutate in place to reverse)
            foreach (var shape in solution.Where(Clipper.Orientation))
                shape.Reverse();

            //Convert back to vectors and apply SAFE_DISTANCE shrink (all rooms are shrunk by this distance)
            return solution
                .Select(shape =>
                    shape
                        .Select(ToVector2)
                        .Shrink(SAFE_DISTANCE)
                        .ToArray()
                ).ToArray();
        }

        /// <summary>
        /// Add a room to the floorplan. This will clip the room to the outer wall and other rooms, which may result in the room being split into two or more parts
        /// </summary>
        /// <param name="roomFootprint">The footprint of the room to try and add</param>
        /// <param name="wallThickness">The thickness of the walls of this room</param>
        /// <param name="split">If true false and this room is split into two parts no room will be added</param>
        /// <returns></returns>
        public IReadOnlyList<IRoomPlan> Add(IEnumerable<Vector2> roomFootprint, float wallThickness, bool split = false)
        {
            if (_isFrozen)
                throw new InvalidOperationException("Cannot add rooms to floorplan once it is frozen");

            var solution = ShapesForRoom(roomFootprint, split);
            if (solution == null)
                return new IRoomPlan[0];

            var result = new List<IRoomPlan>();
            foreach (var shape in solution)
            {
                //Reject rooms with zero points
                if (shape.Count == 0)
                    continue;

                //Mark the neighbourhood cache as dirty...
                //...this means it will recalculate all neighbourhood relationships in the entire plan next time there is a query
                _neighbourhood.Dirty = true;

                //Try to create a room, this may fail in certain circumstances. e.g. if the wallThickness > size of room
                //We simply skip failing rooms
                RoomPlan room;
                if (!RoomPlan.TryCreate(this, shape, wallThickness, _nextId++, out room))
                    continue;

                //Insert the new room into the appropriate data structures
                result.Add(room);
                _rooms.Add(room);
            }

            return result;
        }

        #region clipping
        private List<List<IntPoint>> ClipToRooms(List<IntPoint> roomFootprint, bool allowSplit)
        {
            _clipper.Clear();
            _clipper.AddPath(roomFootprint, PolyType.ptSubject, true);
            _clipper.AddPaths(_rooms.Select(r => r.OuterFootprint.Select(ToPoint).ToList()).ToList(), PolyType.ptClip, true);

            var solution = new PolyTree();
            _clipper.Execute(ClipType.ctDifference, solution);

            //Rooms with holes are not supported
            if (HasHole(solution))
            {
                //Rooms with holes are not supported (issue #166 - Will Not Fix)
                return new List<List<IntPoint>>();
            }

            var shapes = Clipper.ClosedPathsFromPolyTree(solution);

            if (shapes.Count > 1 && !allowSplit)
                return new List<List<IntPoint>>();

            return shapes;
        }

        private static bool HasHole(PolyNode tree)
        {
            Contract.Requires(tree != null && tree.Contour != null && tree.Childs != null);

            if (tree.Contour.Count > 0 && tree.IsHole)
                return true;

            return tree.Childs.Any(HasHole);
        }

        private List<List<IntPoint>> ClipToFloor(List<IntPoint> roomFootprint, bool allowSplit)
        {
            _clipper.Clear();
            _clipper.AddPath(roomFootprint, PolyType.ptSubject, true);
            _clipper.AddPath(_externalFootprint.Select(ToPoint).ToList(), PolyType.ptClip, true);

            var solution = new List<List<IntPoint>>();
            _clipper.Execute(ClipType.ctIntersection, solution);

            if (solution.Count > 1 && !allowSplit)
                return null;
            if (solution.Count == 0)
                return null;

            return solution;
        }
        #endregion

        public IEnumerable<Neighbour> GetNeighbours(RoomPlan room)
        {
            Contract.Requires(room != null);
            Contract.Ensures(Contract.Result<IEnumerable<Neighbour>>() != null);

            return _neighbourhood[room];
        }

        #region static helpers
        private static IntPoint ToPoint(Vector2 v)
        {
            return new IntPoint((int)(v.X * SCALE), (int)(v.Y * SCALE));
        }

        private static Vector2 ToVector2(IntPoint v)
        {
            return new Vector2(v.X / SCALE, v.Y / SCALE);
        }
        #endregion
    }
}
