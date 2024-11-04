using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public class Poly2D : ICurve2D 
    {
        private float _length;


        public float Length 
        {
            get
            {
                UpdateLength();
                return _length;
            }
        }


        public IEnumerable<CurvePoint> Sample(float tolerance, int maxPoints)
        {
            float curLen = 0;

            var totLen = Length;    

            var pCount = IsClosed ? Points.Length : Points.Length - 1;  

            for (var i = 0; i < pCount; i++)
            {
                var p1 = Points[i]; 
                var p2 = Points[(i + 1) % Points.Length];

                yield return new CurvePoint
                {
                    Position = p1,
                    Tangent = Vector2.Normalize(p2 - p1),
                    Length = curLen,
                    Time = curLen / totLen
                };

                curLen += Vector2.Distance(p1, p2);
            }
        }

        public Vector2 GetPointAtTime(float t)
        {
            throw new NotImplementedException();
        }

        public Vector2 GetTangentAtTime(float t)
        {
            throw new NotImplementedException();
        }

        public float GetTimeAtLength(float length)
        {
            return length / Length; 
        }

        protected void UpdateLength()
        {
            _length = 0;
            
            for (int i = 0; i < Points.Length - 1; i++)
                _length += Vector2.Distance(Points[i], Points[i + 1]);

            if (IsClosed)
                _length += Vector2.Distance(Points[^1], Points[0]);
        }

        bool ICurve2D.IsClosed => IsClosed;

        public bool IsClosed;

        public Vector2[] Points = [];
    }
}
