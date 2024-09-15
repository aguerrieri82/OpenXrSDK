using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Services
{
    public unsafe static class TurboJpeg
    {
        [Flags]
        public enum Flags
        {
            TJFLAG_BOTTOMUP = 2,
            TJFLAG_FORCEMMX = 8,
            TJFLAG_FORCESSE = 16,
            TJFLAG_FORCESSE2 = 32,
            TJFLAG_FORCESSE3 = 128,
            TJFLAG_FASTUPSAMPLE = 256,
            TJFLAG_NOREALLOC = 1024,
            TJFLAG_FASTDCT = 2048,
            TJFLAG_ACCURATEDCT = 4096
        }

        public enum PixelFormat
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

        public class ImageData
        {
            public int Width { get; set; }

            public int Height { get; set; }

            public byte[] Data { get; set; }
        }

        const string DllName = "turbojpeg"; 


        [DllImport(DllName)]
        public static extern IntPtr tjInitDecompress();


        [DllImport(DllName)]
        public static  extern void tjDecompressHeader2(IntPtr jpegDecompressor, byte* data, int size, out int width, out int height, out int subSamp);

        [DllImport(DllName)]
        public static extern void tjDecompress2(IntPtr jpegDecompressor, byte* data, int size, byte* outBuffer, int width, int pitch, int height, PixelFormat format, Flags flags);

        [DllImport(DllName)]
        public static extern void tjDestroy(IntPtr jpegDecompressor);


        public static ImageData Decompress(byte[] data)
        {
            var res = new ImageData();
            
            var hadler = tjInitDecompress();

            fixed (byte* pIn = data)
            {
                tjDecompressHeader2(hadler, pIn, data.Length, out var width, out var height, out var subSamp);
                res.Width = width;
                res.Height = height;
                res.Data = new byte[width * height * 4];
 
                fixed (byte* pOut = res.Data)
                {
                    tjDecompress2(hadler, pIn, data.Length, pOut, width, 0, height, PixelFormat.TJPF_RGBA, Flags.TJFLAG_FASTDCT);
                }
            }

            tjDestroy(hadler);

            return res;

        }
    }
}
