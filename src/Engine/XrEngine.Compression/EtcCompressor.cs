using Common.Interop;
using System.Runtime.InteropServices;

namespace XrEngine.Compression
{
    public static class EtcCompressor
    {
        #region NATIVE

        enum Type
        {
            Etc1,
            Etc2_RGB,
            Etc2_RGBA,
            Etc2_R11,
            Etc2_RG11,
            Bc1,
            Bc3,
            Bc4,
            Bc5,
            Bc7
        };

        enum Format
        {
            Pvr = 0,
            Dds = 1
        };

        [StructLayout(LayoutKind.Sequential)]
        struct EncodeOptions
        {
            public bool MipMap;
            public bool Bgr;
            public bool Linearize;
            public Type Codec;
            public Format Format;
            public bool UseHeuristics;
            public bool Dither;
            public int Test;
        };



        [DllImport("etcpack")]
        static unsafe extern byte* Encode(
        uint width,
        uint height,
        byte* data,
        ref EncodeOptions options,
        out uint outSize);


        [DllImport("etcpack")]
        static unsafe extern void Free(byte* data);

        #endregion


        public static unsafe TextureData Encode(TextureData data)
        {

            using var pData = data.Data!.MemoryLock();

            if (!data.Format.IsBgr())
            {
                EngineNativeLib.RgbToBgr(data.Width, data.Height, pData, pData, data.Format.GetPixelSizeBit() / 8);

                if (data.Format == TextureFormat.Rgba32)
                    data.Format = TextureFormat.Bgra32;

                else if (data.Format == TextureFormat.SRgba32)
                    data.Format = TextureFormat.SBgra32;

                else
                    throw new NotSupportedException();
            }

            var options = new EncodeOptions()
            {
                Bgr = false,
                Linearize = false,
                UseHeuristics = true,
                MipMap = false,
                Format = Format.Pvr,
                Codec = Type.Etc2_RGBA,
                Test = 0x11223344
            };

            var outData = Encode(data.Width, data.Height, pData, ref options, out var outSize);

            var result = data.Clone();
            result.Compression = TextureCompressionFormat.Etc2;
            result.Data = MemoryBuffer.Create<byte>(outData + 52, outSize - 52);

            if (result.Format == TextureFormat.Bgra32)
                result.Format = TextureFormat.Rgba32;

            if (result.Format == TextureFormat.SBgra32)
                result.Format = TextureFormat.SRgba32;

            return result;
        }

    }
}
