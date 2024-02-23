using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public struct Spherical
    {
        public float R;

        public float Pol;

        public float Azm;

        public void Normalize()
        {
            Azm = Azm % (MathF.PI * 2);
            Pol = Pol % MathF.PI;   
        }

        public static Spherical FromCartesian(Vector3 vector)
        {
            var res = new Spherical
            {
                R = vector.Length(),
                Pol = MathF.Atan2(vector.Z, vector.X)
            };
            res.Azm = MathF.Acos(vector.Y / res.R);

            return res;
        }

        public Vector3 ToCartesian()
        {
            return new Vector3(
                R * MathF.Sin(Azm) * MathF.Cos(Pol),
                R * MathF.Cos(Azm),
                R * MathF.Sin(Azm) * MathF.Sin(Pol)
            );
        }
    }
}
