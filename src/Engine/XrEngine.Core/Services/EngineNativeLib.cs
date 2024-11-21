using System.Numerics;
using System.Runtime.InteropServices;

namespace XrEngine
{
    public static class EngineNativeLib
    {
        [DllImport("xrengine-native")]
        public static extern void ImageFlipY(nint src, nint dst, uint width, uint height, uint rowSize);

        [DllImport("xrengine-native")]
        public static extern void ImageCopyChannel(nint src, nint dst, uint width, uint height, uint rowSize, uint srcOfs, uint dstOfs, uint cSize);

        [DllImport("xrengine-native")]
        public static extern void CopyMemory(nint src, nint dst, uint size);

        [DllImport("xrengine-native")]
        public static extern int CompareMemory(nint src, nint dst, uint size);

        [DllImport("xrengine-native")]
        public static extern unsafe void Dft(float* values, Complex* output, uint size);


        public static unsafe Complex[] Dft(float[] values, int offset, uint size)
        {
            var result = new Complex[size];
            fixed (Complex* pRes = result)
            fixed (float* pValues = &values[offset])
                Dft(pValues, pRes, size);
            return result;
        }
    }
}
