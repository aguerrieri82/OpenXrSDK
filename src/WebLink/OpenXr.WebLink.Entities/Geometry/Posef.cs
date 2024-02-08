using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.WebLink.Entities
{
    public struct Posef
    {
        public Quaternionf Orientation;

        public Vector3f Position;

        public override string ToString()
        {
            return string.Format("O: {0} - P: {1}", Orientation, Position);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is Posef other)
                return other.Position.Equals(Position) &&
                       other.Orientation.Equals(Orientation);
            return false;
        }

        public bool Similar(Posef other, float epslon)
        {
            return other.Position.Similar(Position, epslon) &&
                   other.Orientation.Similar(Orientation, epslon);
        }


        public override int GetHashCode()
        {
            return Position.GetHashCode() ^ Orientation.GetHashCode();
        }
    }
}
