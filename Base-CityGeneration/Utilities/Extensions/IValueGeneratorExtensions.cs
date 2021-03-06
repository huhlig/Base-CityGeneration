﻿using System;
using System.Diagnostics.Contracts;
using Base_CityGeneration.Utilities.Numbers;
using Myre.Collections;

using MathHelperRedux;

namespace Base_CityGeneration.Utilities.Extensions
{
    public static class IValueGeneratorExtensions
    {
        public static int SelectIntValue(this IValueGenerator gen, Func<double> random, INamedDataCollection data)
        {
            Contract.Requires(gen != null);
            Contract.Requires(random != null);
            Contract.Requires(data != null);

            checked
            {
                //Rearrange the min and max to be integers (in a narrower or equal range)
                var min = (long)Math.Ceiling(gen.MinValue);
                var max = (long)Math.Floor(gen.MaxValue);

                //If they're the same we don't have a whole lot of choice!
                if (min == max)
                    return (int)min;

                //If they're inverted the range is too narrow (e.g. Min:0.1 Max:0.9 we can't select any integers in that range)
                if (min > max)
                {
                    //Swap max and min
                    var tmp = max;
                    max = min;
                    min = tmp;
                }

                //Clamp and round the value
                return (int)Math.Round(MathHelper.Clamp(gen.SelectFloatValue(random, data), min, max), MidpointRounding.AwayFromZero);
            }
        }

        public static IValueGenerator Transform(this IValueGenerator gen, Func<float, float> func = null, bool vary = true)
        {
            Contract.Requires(gen != null);
            Contract.Ensures(Contract.Result<IValueGenerator>() != null);

            //If we're not transforming the value, and we're not making it unvarying this method has no effect!
            if (func == null && vary)
                return gen;

            return new WrapperValue(gen, func ?? (a => a), vary);
        }

        public static IValueGenerator Add(this IValueGenerator a, IValueGenerator b)
        {
            Contract.Requires(a != null);
            Contract.Requires(b != null);
            Contract.Ensures(Contract.Result<IValueGenerator>() != null);

            return new FuncValue(
                (r, m) => a.SelectFloatValue(r, m) + b.SelectFloatValue(r, m),
                a.MinValue + b.MinValue,
                a.MinValue + a.MaxValue
            );
        }
    }
}
