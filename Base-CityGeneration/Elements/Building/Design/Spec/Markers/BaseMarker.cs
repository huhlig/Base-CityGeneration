﻿using System;
using System.Collections;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using EpimetheusPlugins.Scripts;
using Myre.Collections;
using Poly2Tri.Utility;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Markers
{
    public abstract class BaseMarker
        : BaseFloorSelector
    {
        public override float MinHeight
        {
            get
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                Contract.Ensures(Contract.Result<float>() == 0);
                return 0;
            }
        }

        public override float MaxHeight
        {
            get
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                Contract.Ensures(Contract.Result<float>() == 0);
                return 0;
            }
        }

        private readonly BaseFootprintAlgorithm[] _footprintAlgorithms;
        public IEnumerable<BaseFootprintAlgorithm> FootprintAlgorithms
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<BaseFootprintAlgorithm>>() != null);
                return _footprintAlgorithms;
            }
        }

        protected BaseMarker(BaseFootprintAlgorithm[] footprintAlgorithms)
        {
            Contract.Requires(footprintAlgorithms != null);

            _footprintAlgorithms = footprintAlgorithms;
        }

        public IReadOnlyList<Vector2> Apply(Func<double> random, INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> lot)
        {
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Requires(footprint != null);
            Contract.Requires(lot != null);
            Contract.Ensures(Contract.Result<IReadOnlyList<Vector2>>() != null);

            var wip = footprint;
            foreach (var alg in _footprintAlgorithms)
            {
                wip = alg.Apply(random, metadata, wip, footprint, lot);
                wip = Reduce(wip);
            }

            return wip;
        }

        /// <summary>
        /// Reduce the number of sides in this footprint
        /// </summary>
        /// <param name="footprint"></param>
        /// <returns></returns>
        private static IReadOnlyList<Vector2> Reduce(IReadOnlyList<Vector2> footprint)
        {
            Contract.Requires(footprint != null);
            Contract.Ensures(Contract.Result<IReadOnlyList<Vector2>>() != null);
            Contract.Ensures(Contract.Result<IReadOnlyList<Vector2>>().Count <= footprint.Count);

            //Early exit, we can't do anything useful with a line!
            if (footprint.Count <= 3)
                return footprint;

            //Create a list with the points in
            var p = new Point2DList();
            p.AddRange(footprint.Select(a => new Point2D(a.X, a.Y)).ToArray());

            //If two consecutive points are in the same position, remove one
            p.RemoveDuplicateNeighborPoints();

            //Merge edges which are parallel (with a tolerance of 1 degree)
            //p.MergeParallelEdges(0.01745240643);
            p.Simplify();

            //Ensure shape is clockwise wound
            p.CalculateWindingOrder();
            if (p.WindingOrder != Point2DList.WindingOrderType.Clockwise)
            {
                if (p.WindingOrder != Point2DList.WindingOrderType.AntiClockwise)
                    throw new InvalidOperationException("Winding order is neither clockwise or anticlockwise");

                //We're done (but we need to correct the winding)
                return p.Select(a => new Vector2(a.Xf, a.Yf)).ToArray();
            }

            //We're done :D
            return p.Select(a => new Vector2(a.Xf, a.Yf)).ToArray();
        }

        public override IEnumerable<FloorRun> Select(Func<double> random, INamedDataCollection metadata, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder)
        {
            return new FloorRun[1] {
                new FloorRun(new FloorSelection[0], this)
            };
        }

        internal abstract class BaseContainer
            : ISelectorContainer, IList<BaseFootprintAlgorithm.BaseContainer>
        {
            private readonly List<BaseFootprintAlgorithm.BaseContainer> _algorithms = new List<BaseFootprintAlgorithm.BaseContainer>();

            public abstract BaseFloorSelector Unwrap();

            #region ilist
            public IEnumerator<BaseFootprintAlgorithm.BaseContainer> GetEnumerator()
            {
                return _algorithms.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_algorithms).GetEnumerator();
            }

            public void Add(BaseFootprintAlgorithm.BaseContainer item)
            {
                _algorithms.Add(item);
            }

            public void Clear()
            {
                Contract.Ensures(Count == 0);

                _algorithms.Clear();
            }

            public bool Contains(BaseFootprintAlgorithm.BaseContainer item)
            {
                return _algorithms.Contains(item);
            }

            public void CopyTo(BaseFootprintAlgorithm.BaseContainer[] array, int arrayIndex)
            {
                Contract.Assume(arrayIndex < array.Length);
                Contract.Assume(arrayIndex >= 0);
                Contract.Assume(arrayIndex + _algorithms.Count < array.Length);

                _algorithms.CopyTo(array, arrayIndex);
            }

            public bool Remove(BaseFootprintAlgorithm.BaseContainer item)
            {
                return _algorithms.Remove(item);
            }

            public int Count
            {
                get
                {
                    Contract.Ensures(Contract.Result<int>() >= 0);
                    return _algorithms.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    Contract.Ensures(!Contract.Result<bool>());
                    return false;
                }
            }

            public int IndexOf(BaseFootprintAlgorithm.BaseContainer item)
            {
                Contract.Ensures(Contract.Result<int>() == -1 || Contract.Result<int>() < Count);
                Contract.Ensures(Contract.Result<int>() == -1 || Contract.Result<int>() >= 0);

                return _algorithms.IndexOf(item);
            }

            public void Insert(int index, BaseFootprintAlgorithm.BaseContainer item)
            {
                Contract.Assume(index < Count);
                Contract.Assume(index >= 0);

                _algorithms.Insert(index, item);
            }

            public void RemoveAt(int index)
            {
                Contract.Assume(index < Count);
                Contract.Assume(index >= 0);

                _algorithms.RemoveAt(index);
            }

            public BaseFootprintAlgorithm.BaseContainer this[int index]
            {
                get
                {
                    Contract.Assume(index < Count);
                    Contract.Assume(index >= 0);

                    return _algorithms[index];
                }
                set
                {
                    Contract.Assume(index < Count);
                    Contract.Assume(index >= 0);

                    _algorithms[index] = value;
                }
            }
            #endregion
        }
    }
}
