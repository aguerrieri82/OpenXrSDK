using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework.Android
{
    public interface ISurfaceInput
    {

        bool IsPointerValid { get; }

        public Vector2 Pointer { get; }

        public bool IsPointerDown { get; } 
    }
}
