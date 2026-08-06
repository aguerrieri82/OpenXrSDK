using Common.Interop;
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
                        format = isSrgb ? TextureFormat.SRgba8 : TextureFormat.Rgba8;
                    else if (outImg.BitDepth == 16)
                        format = isSrgb ? TextureFormat.SRgbaInt16 : TextureFormat.Rgba16;
                    else
                        throw new NotSupportedException();
                }
                else if (outImg.ColorType == PngLib.ColorTypeRgb)
                {
                    if (outImg.BitDepth == 8)
                        format = isSrgb ? TextureFormat.SRgb8 : TextureFormat.Rgb8;
                    else
                        throw new NotSupportedException();
                }
                else if (outImg.ColorType == PngLib.ColorTypeGray && !isSrgb)
                {
                    if (outImg.BitDepth == 16)
                        format = TextureFormat.Gray16;
                    else if (outImg.BitDepth == 8)
                        format = TextureFormat.Gray8;
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
                    Content = buf
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
