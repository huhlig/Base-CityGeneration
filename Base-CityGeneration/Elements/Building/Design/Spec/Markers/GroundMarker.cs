﻿using System;
using System.Diagnostics.Contracts;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms;
using System.Linq;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Markers
{
    /// <summary>
    /// Marks where the ground is in a sequence
    /// </summary>
    public class GroundMarker
        : BaseMarker
    {
        public GroundMarker(BaseFootprintAlgorithm[] algorithms)
            : base(algorithms)
        {
            Contract.Requires(algorithms != null);
        }

        internal class Container
            : BaseContainer
        {
            public override BaseFloorSelector Unwrap()
            {
                return new GroundMarker(this.Select(a => a.Unwrap()).ToArray());
            }
        }
    }
}
