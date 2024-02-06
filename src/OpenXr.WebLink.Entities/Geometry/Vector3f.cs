using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.WebLink.Entities
{
    public struct Vector3f
    {
        public float X;

        public float Y;

        public float Z;

        public override string ToString()
        {
            return string.Format("({0:0.000}) ({1:0.000}) ({2:0.000})", X, Y, Z);
        }

        public bool Similar(Vector3f other, float epslon)
        {
            return Math.Abs(X - other.X) < epslon &&
                Math.Abs(Y - other.Y) < epslon &&
                Math.Abs(Z - other.Z) < epslon;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is Vector3f other)
                return other.X == X && other.Y == Y && other.Z == Z;
            return false;
        }


        public override int GetHashCode()
        {
            return (int)Math.Round((X + Y + Z) * 100);
        }
    }
}
