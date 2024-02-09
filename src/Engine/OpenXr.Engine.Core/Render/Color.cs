using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public struct Color 
    {
        public Color()
        {
        }

        public Color(float r, float g, float b, float a = 1f)
        {
            R = r; 
            G = g; 
            B = b;
            A = a;
        }

        public void Rgb(float value)
        {
            R = value;
            G = value;
            B = value;
        }

        public static implicit operator Vector3(Color color)
        {
            return new Vector3(color.R, color.G, color.B);
        }

        public float R;

        public float G;
        
        public float B;

        public float A;
    }
}
