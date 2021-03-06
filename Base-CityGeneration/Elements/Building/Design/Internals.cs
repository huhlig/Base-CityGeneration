﻿using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers;
using EpimetheusPlugins.Scripts;
using Myre.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Base_CityGeneration.Elements.Building.Design
{
    public class Internals
    {
        private readonly BuildingDesigner _designer;

        private readonly FloorSelection[][] _aboveGroundFloorRuns;
        /// <summary>
        /// Set of floors to place above ground
        /// </summary>
        public IEnumerable<FloorSelection> AboveGroundFloors
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<FloorSelection>>() != null);
                foreach (var run in _aboveGroundFloorRuns)
                    foreach (var floor in run)
                        yield return floor;
            }
        }

        private readonly FloorSelection[][] _belowGroundFloorRuns;
        /// <summary>
        /// Set of floors to place below ground
        /// </summary>
        public IEnumerable<FloorSelection> BelowGroundFloors
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<FloorSelection>>() != null);
                foreach (var run in _belowGroundFloorRuns)
                    foreach (var floor in run)
                        yield return floor;
            }
        }

        public IEnumerable<FloorSelection> Floors
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<FloorSelection>>() != null);
                foreach (var floor in AboveGroundFloors)
                    yield return floor;
                foreach (var floor in BelowGroundFloors)
                    yield return floor;
            }
        }

        private readonly FootprintSelection[] _footprints;
        public IEnumerable<FootprintSelection> Footprints
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<FootprintSelection>>() != null);
                return _footprints;
            }
        }

        /// <summary>
        /// Vertical elements to place within this building
        /// </summary>
        public IEnumerable<VerticalSelection> Verticals { get; internal set; }

        internal Internals(BuildingDesigner designer, FloorSelection[][] aboveGroundFloors, FloorSelection[][] belowGroundFloors, FootprintSelection[] footprints)
        {
            Contract.Requires(designer != null);
            Contract.Requires(aboveGroundFloors != null);
            Contract.Requires(belowGroundFloors != null);
            Contract.Requires(footprints != null);

            _designer = designer;

            _aboveGroundFloorRuns = aboveGroundFloors;
            _belowGroundFloorRuns = belowGroundFloors;

            _footprints = footprints;
        }

        public Design Externals(Func<double> random, INamedDataCollection metadata, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder, BuildingSideInfo[] sides)
        {
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Requires(finder != null);
            Contract.Requires(sides != null);
            Contract.Ensures(Contract.Result<Design>() != null);

            //No footprints means no floors, early exit with an empty building
            if (!Footprints.Any())
                return new Design(new FloorSelection[0], new VerticalSelection[0], new Design.Wall[0]);

            //Generate footprints up building
            var footprints = GenerateFootprints(random, metadata, sides.Select(a => a.EdgeEnd).ToArray(), Footprints, AboveGroundFloors.Count(), BelowGroundFloors.Count());

            //Results of facade selection
            var walls = new List<Design.Wall>();

            //Generate facades
            foreach (var run in _aboveGroundFloorRuns)
            {
                if (!run.Any())
                    continue;

                var bot = run.Min(a => a.Index);

                //Get the footprint for this run
                var footprint = footprints[bot];

                //Generate facades for each side
                for (var i = 0; i < footprint.Count; i++)
                {
                    //Get start and end points of this wall
                    var s = footprint[i];
                    var e = footprint[(i + 1) % footprint.Count];

                    //Select facades
                    Design.Wall w = new Design.Wall(i, bot, s, e, _designer.SelectFacadesForWall(random, finder, run, sides, s, e));

                    //Save results
                    walls.Add(w);
                }
            }

            return new Design(Floors, Verticals, walls);
        }

        private static IReadOnlyDictionary<int, IReadOnlyList<Vector2>> GenerateFootprints(Func<double> random, INamedDataCollection metadata, IReadOnlyList<Vector2> initial, IEnumerable<FootprintSelection> footprints, int aboveGroundFloorCount, int belowGroundFloorCount)
        {
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Requires(initial != null);
            Contract.Requires(footprints != null && footprints.Any());
            Contract.Ensures(Contract.Result<IReadOnlyDictionary<int, IReadOnlyList<Vector2>>>() != null);

            var results = new Dictionary<int, IReadOnlyList<Vector2>>();

            //Index by floor number
            var footprintLookup = footprints.ToDictionary(a => a.Index, a => a.Marker);

            //Generate initial ground shape
            var ground = footprints.Single(a => a.Index == 0).Marker.Apply(random, metadata, initial, initial);
            results.Add(0, ground);

            //Generate upwards
            var previous = ground;
            for (int i = 1; i < aboveGroundFloorCount; i++)
            {
                BaseMarker gen;
                if (footprintLookup.TryGetValue(i, out gen))
                {
                    previous = gen.Apply(random, metadata, previous, initial);
                    results.Add(i, previous);
                }
            }

            //Generate downwards
            previous = ground;
            for (int i = 1; i < belowGroundFloorCount; i++)
            {
                BaseMarker gen;
                if (footprintLookup.TryGetValue(-i, out gen))
                {
                    previous = gen.Apply(random, metadata, previous, initial);
                    results.Add(-i, previous);
                }
            }

            return results;
        }
    }
}
