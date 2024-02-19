using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public struct Vector2I
    {
        public Vector2I()
        {
        }

        public Vector2I(int x, int y)
        {
            X = x; 
            Y = y;
        }

        public int X;

        public int Y;   
    }
}
