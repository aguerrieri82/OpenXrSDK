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

        public float R;

        public float G;
        
        public float B;

        public float A;
    }
}
