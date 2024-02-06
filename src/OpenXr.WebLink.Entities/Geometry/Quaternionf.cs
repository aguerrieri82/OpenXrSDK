using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.WebLink.Entities
{
    public struct Quaternionf
    {
        public float X;

        public float Y;

        public float Z;

        public float W;

        public override string ToString()
        {
            return string.Format("({0:0.00}) ({1:0.00}) ({2:0.00}) ({3:0.00})", X, Y, Z, W);
        }

        public bool Similar(Quaternionf other, float epslon)
        {
            return Math.Abs(other.X - other.X) < epslon &&
                Math.Abs(other.Y - other.Y) < epslon &&
                Math.Abs(other.Z - other.Z) < epslon &&
                Math.Abs(other.W - other.W) < epslon;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is Quaternionf other)
                return other.X == X && 
                    other.Y == Y && 
                    other.Z == Z && 
                    other.W == W;
            return false;
        }


        public override int GetHashCode()
        {
            return (int)Math.Round((X + Y + Z + W) * 100);
        }
    }
}
