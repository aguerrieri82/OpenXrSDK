using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
        public static extern IntPtr tjInitDecompress();


        [DllImport(DllName)]
        public static  extern void tjDecompressHeader2(IntPtr handle, byte* jpegBuf, ulong jpegSize, out int width, out int height, out TJSAMP jpegSubsamp);

        [DllImport(DllName)]
        public static extern void tjDecompress2(IntPtr handle, byte* jpegBuf, ulong jpegSize, byte* dstBuf, int width, int pitch, int height, TJPF pixelFormat, TJFLAG flags);

        [DllImport(DllName)]
        public static extern void tjDestroy(IntPtr handle);


        public static ImageData Decompress(byte[] data)
        {
            var res = new ImageData();
            
            var handler = tjInitDecompress();

            fixed (byte* pIn = data)
            {
                tjDecompressHeader2(handler, pIn, (ulong)data.Length, out var width, out var height, out var subSamp);
                res.Width = width;
                res.Height = height;
                res.Data = new byte[width * height * 4];
 
                fixed (byte* pOut = res.Data)
                {
                    tjDecompress2(handler, pIn, (ulong)data.Length, pOut, width, 0, height, TJPF.TJPF_RGBA, TJFLAG.TJFLAG_FASTDCT);
                }
            }

            tjDestroy(handler);

            return res;

        }
    }
}
