﻿using System;
using Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Scalars;
using Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Vectors;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Tensors
{
    public class Heightmap
        : ITensorField
    {
        private readonly IVector2Field _gradient;

        public Heightmap(BaseScalarField height)
        {
            _gradient = new Gradient(height);
        }

        public void Sample(ref Vector2 position, out Tensor result)
        {
            var grad = _gradient.Sample(position);

            var theta = (float)Math.Atan2(grad.Y, grad.X) + MathHelper.PiOver2;
            var r = (float)Math.Sqrt(grad.X * grad.X + grad.Y * grad.Y);

            result = Tensor.Normalize(Tensor.FromRTheta(r, theta));
        }
    }
}