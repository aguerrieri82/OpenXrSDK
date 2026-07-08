using Common.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using TurboJpeg;
using XrEngine.Compression;

namespace XrEngine.Transcoder
{
    public class PngReader : BaseTextureLoader
    {
        PngReader()
        {

        }

        public override unsafe IList<TextureData> LoadTexture(Stream stream, TextureLoadOptions? options = null)
        {
            var buffer = new byte[stream.Length];
            stream.ReadExactly(buffer);

            fixed (byte* pData = buffer)
            {
                var outImg = new PngLib.ImageData();

                var src = new PngLib.MemoryBuffer
                {
                    Data = pData,
                    Size = buffer.Length,
                    Capacity = buffer.Length
                };

                PngLib.DecodePng(ref src, true, ref outImg);

                TextureFormat format;

                var isSrgb = options?.IsSrgb ?? false;

                if (outImg.ColorType == PngLib.ColorTypeRgba)
                {
                    if (outImg.BitDepth == 8)
                        format = isSrgb ? TextureFormat.SRgba32 : TextureFormat.Rgba32;
                    else if (outImg.BitDepth == 16)
                        format = isSrgb ? TextureFormat.SRgbaInt16 : TextureFormat.RgbaInt16;
                    else
                        throw new NotSupportedException();
                }
                else  if (outImg.ColorType == PngLib.ColorTypeRgb)
                {
                    if (outImg.BitDepth == 8)
                        format = isSrgb ? TextureFormat.SRgb24 : TextureFormat.Rgb24;
                    else
                        throw new NotSupportedException();
                }
                else if (outImg.ColorType == PngLib.ColorTypeGray && !isSrgb)
                {
                    if (outImg.BitDepth == 16)
                        format = TextureFormat.GrayInt16;
                    else if (outImg.BitDepth == 8)
                        format = TextureFormat.GrayInt8;
                    else
                        throw new NotSupportedException();
                }
                else
                    throw new NotSupportedException();

                var buf = MemoryBuffer.Create<byte>((uint)outImg.Data.Size);

                using var dst = buf.MemoryLock();

                EngineNativeLib.CopyMemory((nint)outImg.Data.Data, (nint)dst.Data, (uint)outImg.Data.Size);

                PngLib.FreeMemoryBuffer(ref outImg.Data);

                return [new TextureData
                {
                    Width = (uint)outImg.Width,
                    Height = (uint)outImg.Height,
                    Format = format,
                    Data = buf
                }];
            }
        }

        protected override bool CanHandleExtension(string extension)
        {
            return extension == ".png";
        }

        public static readonly PngReader Instance = new();
    }
}
