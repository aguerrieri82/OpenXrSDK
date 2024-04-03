using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public static class NativeLib
    {
        [DllImport("xrengine-native")]
        public static extern void ImageFlipY(nint src, nint dst, uint width, uint height, uint rowSize);
    }
}
