using System;
using System.Collections.Generic;
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
            return string.Format("({0:0.00}) ({1:0.00}) ({2:0.00})", X, Y, Z);
        }
    }
}
