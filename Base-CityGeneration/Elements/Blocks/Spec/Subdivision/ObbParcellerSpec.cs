﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Base_CityGeneration.Elements.Blocks.Spec.Subdivision.Rules;
using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Utilities.Numbers;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Blocks.Spec.Subdivision
{
    public class ObbParcellerSpec
        : BaseSubdivideSpec
    {
        private readonly IValueGenerator _nonOptimalOabbChance;
        private readonly IValueGenerator _nonOptimalOabbMaxRatio;
        private readonly IValueGenerator _splitPointSelection;

        private readonly BaseSubdividerRule[] _rules;
        public override IEnumerable<BaseSubdividerRule> Rules
        {
            get { return _rules; }
        }

        public ObbParcellerSpec(IValueGenerator nonOptimalOabbChance, IValueGenerator nonOptimalOabbMaxRatio, IValueGenerator splitPointGenerator, BaseSubdividerRule[] rules)
        {
            Contract.Requires(rules != null);

            _nonOptimalOabbChance = nonOptimalOabbChance;
            _nonOptimalOabbMaxRatio = nonOptimalOabbMaxRatio;
            _splitPointSelection = splitPointGenerator;

            _rules = rules;
        }

        public override IEnumerable<Parcel> GenerateParcels(Parcel root, Func<double> random, INamedDataCollection metadata)
        {
            var p = new ObbParceller(_splitPointSelection, _nonOptimalOabbChance, _nonOptimalOabbMaxRatio);
            foreach (var rule in _rules)
                p.AddTerminationRule(rule.Rule(random, metadata));

            return p.GenerateParcels(root, random, metadata);
        }

        internal class Container
            : BaseContainer
        {
            public object NonOptimalChance { get; set; }
            public object MaxNonOptimalRatio { get; set; }

            public object SplitRatio { get; set; }

            public override BaseSubdivideSpec Unwrap()
            {
                return new ObbParcellerSpec(
                    NonOptimalChance == null ? null : IValueGeneratorContainer.FromObject(NonOptimalChance),
                    MaxNonOptimalRatio == null ? null : IValueGeneratorContainer.FromObject(MaxNonOptimalRatio),
                    SplitRatio == null ? null : IValueGeneratorContainer.FromObject(SplitRatio),
                    (Rules ?? new BaseSubdividerRule.BaseContainer[0]).Select(a => a.Unwrap()).ToArray()
                );
            }
        }
    }
}
