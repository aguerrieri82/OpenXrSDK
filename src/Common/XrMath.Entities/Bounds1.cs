using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace XrMath
{
    public struct Bounds1
    {
        public float Max;

        public float Min;


        public readonly float Size => Max - Min;

        public readonly float Center => (Max + Min) / 2;

        public static Bounds1 Zero { get; } = new();
    }
}
