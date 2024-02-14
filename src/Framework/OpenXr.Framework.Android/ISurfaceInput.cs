using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework.Android
{
    public struct InputButton
    {
        public bool IsDown;

        public bool IsChanged;
    }

    public interface ISurfaceInput
    {

        bool IsPointerValid { get; }

        public Vector2 Pointer { get; }

        public InputButton MainButton { get; }

        [Obsolete("Test")]
        public InputButton BackButton { get; } 
    }
}
