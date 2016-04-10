﻿using Base_CityGeneration.Elements.Building.Design.Spec;
using EpimetheusPlugins.Scripts;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Base_CityGeneration.Utilities;

namespace Base_CityGeneration.Elements.Building.Design
{
    public class FloorSelection
    {
        private readonly string _id;
        public string Id { get { return _id; } }

        private readonly IReadOnlyList<KeyValuePair<string, string>> _tags;
        public IReadOnlyList<KeyValuePair<string, string>> Tags
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<KeyValuePair<string, string>>>() != null);
                return _tags;
            }
        }

        private readonly BaseFloorSelector _selector;
        public BaseFloorSelector Selector
        {
            get
            {
                Contract.Ensures(Contract.Result<BaseFloorSelector>() != null);
                return _selector;
            }
        }

        private readonly ScriptReference _script;
        public ScriptReference Script
        {
            get
            {
                Contract.Ensures(Contract.Result<ScriptReference>() != null);
                return _script;
            }
        }

        readonly float _height;
        /// <summary>
        /// Height of this floor (from floor to ceiling)
        /// </summary>
        public float Height
        {
            get
            {
                return _height;
            }
        }

        /// <summary>
        /// The sum of the height of all floors between this one and the ground floor
        /// </summary>
        public float CompoundHeight { get; internal set; }

        public int Index { get; internal set; }

        internal FloorSelection(string id, BaseFloorSelector selector, float height, TagsContainerExtensions.ScriptSelection script, int index = 0)
            : this(id, script.Tags, selector, script.Script, height, index)
        {
            Contract.Requires(selector != null);
        }

        public FloorSelection(string id, IReadOnlyList<KeyValuePair<string, string>> tags, BaseFloorSelector selector, ScriptReference script, float height, int index = 0)
        {
            Contract.Requires(tags != null);
            Contract.Requires(selector != null);
            Contract.Requires(script != null);

            _id = id;
            _tags = tags;
            _script = script;
            _selector = selector;
            _height = height;
            Index = index;
        }

        public FloorSelection(FloorSelection selection, int index)
        {
            Contract.Requires(selection != null);

            _id = selection.Id;
            _tags = selection.Tags.ToArray();
            _script = selection.Script;
            _height = selection.Height;
            _selector = selection.Selector;
            Index = index;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(_tags != null);
            Contract.Invariant(_script != null);
            Contract.Invariant(_selector != null);
        }

        internal FloorSelection Clone()
        {
            return new FloorSelection(this, Index);
        }
    }
}
