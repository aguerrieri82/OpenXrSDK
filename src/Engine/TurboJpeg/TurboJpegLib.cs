using System.Runtime.InteropServices;

namespace TurboJpeg
{
    public unsafe static class TurboJpegLib
    {
        [Flags]
        public enum TJFLAG
        {
            /**
             * The uncompressed source/destination image is stored in bottom-up (Windows,
             * OpenGL) order, not top-down (X11) order.
             */
            TJFLAG_BOTTOMUP = 2,
            /**
             * Turn off CPU auto-detection and force TurboJPEG to use MMX code (if the
             * underlying codec supports it.)
             */
            TJFLAG_FORCEMMX = 8,
            /**
             * Turn off CPU auto-detection and force TurboJPEG to use SSE code (if the
             * underlying codec supports it.)
             */
            TJFLAG_FORCESSE = 16,
            /**
             * Turn off CPU auto-detection and force TurboJPEG to use SSE2 code (if the
             * underlying codec supports it.)
             */
            TJFLAG_FORCESSE2 = 32,
            /**
             * Turn off CPU auto-detection and force TurboJPEG to use SSE3 code (if the
             * underlying codec supports it.)
             */
            TJFLAG_FORCESSE3 = 128,
            /**
             * When decompressing an image that was compressed using chrominance
             * subsampling, use the fastest chrominance upsampling algorithm available in
             * the underlying codec.  The default is to use smooth upsampling, which
             * creates a smooth transition between neighboring chrominance components in
             * order to reduce upsampling artifacts in the decompressed image.
             */
            TJFLAG_FASTUPSAMPLE = 256,
            /**
             * Disable buffer (re)allocation.  If passed to #tjCompress2() or
             * #tjTransform(), this flag will cause those functions to generate an error if
             * the JPEG image buffer is invalid or too small rather than attempting to
             * allocate or reallocate that buffer.  This reproduces the behavior of earlier
             * versions of TurboJPEG.
             */
            TJFLAG_NOREALLOC = 1024,
            /**
             * Use the fastest DCT/IDCT algorithm available in the underlying codec.  The
             * default if this flag is not specified is implementation-specific.  The
             * libjpeg implementation, for example, uses the fast algorithm by default when
             * compressing, because this has been shown to have only a very slight effect
             * on accuracy, but it uses the accurate algorithm when decompressing, because
             * this has been shown to have a larger effect.
             */
            TJFLAG_FASTDCT = 2048,
            /**
             * Use the most accurate DCT/IDCT algorithm available in the underlying codec.
             * The default if this flag is not specified is implementation-specific.  The
             * libjpeg implementation, for example, uses the fast algorithm by default when
             * compressing, because this has been shown to have only a very slight effect
             * on accuracy, but it uses the accurate algorithm when decompressing, because
             * this has been shown to have a larger effect.
             */
            TJFLAG_ACCURATEDCT = 4096
        }

        public enum TJPF
        {
            /**
             * RGB pixel format.  The red, green, and blue components in the image are
             * stored in 3-byte pixels in the order R, G, B from lowest to highest byte
             * address within each pixel.
             */
            TJPF_RGB = 0,
            /**
             * BGR pixel format.  The red, green, and blue components in the image are
             * stored in 3-byte pixels in the order B, G, R from lowest to highest byte
             * address within each pixel.
             */
            TJPF_BGR,
            /**
             * RGBX pixel format.  The red, green, and blue components in the image are
             * stored in 4-byte pixels in the order R, G, B from lowest to highest byte
             * address within each pixel.  The X component is ignored when compressing
             * and undefined when decompressing.
             */
            TJPF_RGBX,
            /**
             * BGRX pixel format.  The red, green, and blue components in the image are
             * stored in 4-byte pixels in the order B, G, R from lowest to highest byte
             * address within each pixel.  The X component is ignored when compressing
             * and undefined when decompressing.
             */
            TJPF_BGRX,
            /**
             * XBGR pixel format.  The red, green, and blue components in the image are
             * stored in 4-byte pixels in the order R, G, B from highest to lowest byte
             * address within each pixel.  The X component is ignored when compressing
             * and undefined when decompressing.
             */
            TJPF_XBGR,
            /**
             * XRGB pixel format.  The red, green, and blue components in the image are
             * stored in 4-byte pixels in the order B, G, R from highest to lowest byte
             * address within each pixel.  The X component is ignored when compressing
             * and undefined when decompressing.
             */
            TJPF_XRGB,
            /**
             * Grayscale pixel format.  Each 1-byte pixel represents a luminance
             * (brightness) level from 0 to 255.
             */
            TJPF_GRAY,
            /**
             * RGBA pixel format.  This is the same as @ref TJPF_RGBX, except that when
             * decompressing, the X component is guaranteed to be 0xFF, which can be
             * interpreted as an opaque alpha channel.
             */
            TJPF_RGBA,
            /**
             * BGRA pixel format.  This is the same as @ref TJPF_BGRX, except that when
             * decompressing, the X component is guaranteed to be 0xFF, which can be
             * interpreted as an opaque alpha channel.
             */
            TJPF_BGRA,
            /**
             * ABGR pixel format.  This is the same as @ref TJPF_XBGR, except that when
             * decompressing, the X component is guaranteed to be 0xFF, which can be
             * interpreted as an opaque alpha channel.
             */
            TJPF_ABGR,
            /**
             * ARGB pixel format.  This is the same as @ref TJPF_XRGB, except that when
             * decompressing, the X component is guaranteed to be 0xFF, which can be
             * interpreted as an opaque alpha channel.
             */
            TJPF_ARGB
        }

        public enum TJSAMP
        {
            /**
             * 4:4:4 chrominance subsampling (no chrominance subsampling).  The JPEG or
             * YUV image will contain one chrominance component for every pixel in the
             * source image.
             */
            TJSAMP_444 = 0,
            /**
             * 4:2:2 chrominance subsampling.  The JPEG or YUV image will contain one
             * chrominance component for every 2x1 block of pixels in the source image.
             */
            TJSAMP_422,
            /**
             * 4:2:0 chrominance subsampling.  The JPEG or YUV image will contain one
             * chrominance component for every 2x2 block of pixels in the source image.
             */
            TJSAMP_420,
            /**
             * Grayscale.  The JPEG or YUV image will contain no chrominance components.
             */
            TJSAMP_GRAY,
            /**
             * 4:4:0 chrominance subsampling.  The JPEG or YUV image will contain one
             * chrominance component for every 1x2 block of pixels in the source image.
             */
            TJSAMP_440
        }

        public class ImageData
        {
            public int Width { get; set; }

            public int Height { get; set; }

            public byte[]? Data { get; set; }
        }

        const string DllName = "turbojpeg-native";

        [DllImport(DllName)]
        public static extern IntPtr tjInitCompress();

        [DllImport(DllName)]
        public static extern IntPtr tjInitDecompress();

        [DllImport(DllName)]
        public static extern int tjCompress2(
            IntPtr handle,
            byte* srcBuf,
            int width,
            int pitch,
            int height,
            TJPF pixelFormat,
            ref IntPtr jpegBuf,
            ref ulong jpegSize,
            TJSAMP jpegSubsamp,
            int jpegQual,
            TJFLAG flags);

        [DllImport(DllName)]
        public static extern int tjDecompressHeader2(
            IntPtr handle,
            byte* jpegBuf,
            ulong jpegSize,
            out int width,
            out int height,
            out TJSAMP jpegSubsamp);

        [DllImport(DllName)]
        public static extern int tjDecompress2(
            IntPtr handle,
            byte* jpegBuf,
            ulong jpegSize,
            byte* dstBuf,
            int width,
            int pitch,
            int height,
            TJPF pixelFormat,
            TJFLAG flags);

        [DllImport(DllName)]
        public static extern void tjFree(IntPtr buffer);

        [DllImport(DllName)]
        public static extern int tjDestroy(IntPtr handle);

        [DllImport(DllName)]
        private static extern IntPtr tjGetErrorStr();

        public static byte[] Compress(
            ImageData image,
            int quality = 90,
            TJPF pixelFormat = TJPF.TJPF_RGBA,
            TJSAMP subSamp = TJSAMP.TJSAMP_420,
            TJFLAG flags = TJFLAG.TJFLAG_FASTDCT)
        {
            if (image.Data == null)
                throw new ArgumentException("Image data is null", nameof(image));

            return Compress(image.Data, image.Width, image.Height, quality, subSamp, pixelFormat, flags);
        }

        public static byte[] Compress(
            byte[] rgba,
            int width,
            int height,
            int quality = 90,
            TJSAMP subSamp = TJSAMP.TJSAMP_420,
            TJPF pixelFormat = TJPF.TJPF_RGBA,
            TJFLAG flags = TJFLAG.TJFLAG_FASTDCT)
        {
            var handle = tjInitCompress();

            if (handle == IntPtr.Zero)
                throw new InvalidOperationException("tjInitCompress failed");

            var jpegBuf = IntPtr.Zero;
            var jpegSize = 0UL;

            try
            {
                fixed (byte* pIn = rgba)
                {
                    var result = tjCompress2(
                        handle,
                        pIn,
                        width,
                        0,
                        height,
                        pixelFormat,
                        ref jpegBuf,
                        ref jpegSize,
                        subSamp,
                        quality,
                        flags);

                    if (result != 0)
                        throw new InvalidOperationException("tjCompress2 failed: " + GetError());
                }

                if (jpegSize > int.MaxValue)
                    throw new InvalidOperationException("Compressed JPEG is too large");

                var res = new byte[(int)jpegSize];
                Marshal.Copy(jpegBuf, res, 0, res.Length);

                return res;
            }
            finally
            {
                if (jpegBuf != IntPtr.Zero)
                    tjFree(jpegBuf);

                tjDestroy(handle);
            }
        }

        public static ImageData Decompress(byte[] data)
        {
            var handle = tjInitDecompress();

            if (handle == IntPtr.Zero)
                throw new InvalidOperationException("tjInitDecompress failed");

            try
            {
                fixed (byte* pIn = data)
                {
                    var result = tjDecompressHeader2(
                        handle,
                        pIn,
                        (ulong)data.Length,
                        out var width,
                        out var height,
                        out var subSamp);

                    if (result != 0)
                        throw new InvalidOperationException("tjDecompressHeader2 failed: " + GetError());

                    var res = new ImageData
                    {
                        Width = width,
                        Height = height,
                        Data = new byte[width * height * 4]
                    };

                    fixed (byte* pOut = res.Data)
                    {
                        result = tjDecompress2(
                            handle,
                            pIn,
                            (ulong)data.Length,
                            pOut,
                            width,
                            0,
                            height,
                            TJPF.TJPF_RGBA,
                            TJFLAG.TJFLAG_FASTDCT);

                        if (result != 0)
                            throw new InvalidOperationException("tjDecompress2 failed: " + GetError());
                    }

                    return res;
                }
            }
            finally
            {
                tjDestroy(handle);
            }
        }

        private static string GetError()
        {
            var ptr = tjGetErrorStr();

            if (ptr == IntPtr.Zero)
                return "Unknown TurboJPEG error";

            return Marshal.PtrToStringAnsi(ptr) ?? "Unknown TurboJPEG error";
        }
    }
}

