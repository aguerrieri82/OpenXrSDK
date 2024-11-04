using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public struct Circle2D : ICurve2D
    {

        public readonly Vector2 GetPointAtTime(float t)
        {
            var angle = MathF.PI * 2 * t;   
            return new Vector2(Center.X + MathF.Cos(angle) * Radius, Center.Y + MathF.Sin(angle) * Radius);
        }

        public readonly Vector2 GetTangentAtTime(float t)
        {
            var angle = MathF.PI * 2 * t;
            return Vector2.Normalize(new Vector2(-MathF.Sin(angle), MathF.Cos(angle)));
        }

        public readonly float GetTimeAtLength(float length)
        {
            return length / Length; 
        }

        public readonly IEnumerable<CurvePoint> Sample(float tolerance, int maxPoints)
        {
            double angle = Math.Acos(1 - tolerance / Radius);
            int steps = (int)Math.Ceiling(Math.PI / angle);

            for (var i = 0; i <= steps; i++)
            {
                float t = (float)((1d / steps) * i);
                if (i == steps)
                    t = 1f;
                yield return new CurvePoint
                {
                    Length = Length * t,
                    Position = GetPointAtTime(t),
                    Tangent = GetTangentAtTime(t),
                    Time = t
                };
            }
        }

        public readonly float Length => Radius * 2 * MathF.PI;

        public readonly bool IsClosed => true;


        public float Radius;

        public Vector2 Center;
    }
}
