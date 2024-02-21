using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine.Geometry
{
    internal class Spherical
    {
        public float R;

        public float Pol;

        public float Azm;

        public static Spherical FromCartesian(Vector3 vector)
        {
            var res = new Spherical();

            res.R = vector.Length();
            res.Pol = MathF.Atan2(vector.Y, vector.X);
            res.Azm = MathF.Acos(vector.Z / res.R);

            return res;
        }

        public Vector3 ToCartesian()
        {
            return new Vector3(
                R * MathF.Sin(Azm) * MathF.Cos(Pol),
                R * MathF.Sin(Azm) * MathF.Sin(Pol),
                R * MathF.Cos(Azm)
            );
        }
    }
}
