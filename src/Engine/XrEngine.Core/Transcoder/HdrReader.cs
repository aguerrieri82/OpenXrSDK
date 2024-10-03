#pragma warning disable CS0649

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace XrEngine
{
    public class HdrReader : BaseTextureLoader
    {

        HdrReader()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public unsafe override IList<TextureData> LoadTexture(Stream stream, TextureLoadOptions? options = null)
        {
            var buffer = new StringBuilder(256);

            string ReadLine()
            {
                int c;
                buffer.Length = 0;
                while ((c = stream.ReadByte()) != '\n')
                    buffer.Append((char)c);
                return buffer.ToString();
            }

            var line = ReadLine();
            if (line != "#?RADIANCE")
                throw new FormatException();

            string format = "";
            float exposure;
            int width = 0;
            int height = 0;

            while ((line = ReadLine()) != "")
            {
                if (line.StartsWith("FORMAT="))
                    format = line.Substring(7).Trim();
                if (line.StartsWith("EXPOSURE="))
                    exposure = float.Parse(line.Substring(9).Trim(), CultureInfo.InvariantCulture);
            }

            line = ReadLine();

            var parts = line.Split(' ');

            //bool flipY = false;

            for (var i = 0; i < parts.Length; i += 2)
            {
                var value = int.Parse(parts[i + 1]);
                if (parts[i] == "-Y")
                {
                    //flipY = true;
                    height = value;
                }

                else if (parts[i] == "+Y")
                    height = value;

                else if (parts[i] == "+X")
                    width = value;
                else
                    throw new NotSupportedException();
            }

            if (format != "32-bit_rle_rgbe")
                throw new NotSupportedException();

            var block4 = new byte[4];
            var block2 = new byte[2];
            var blockWidth = new byte[width * 4];
            var blockImg = new byte[width * height * 4];

            var rgbe = new Span<byte>(block4);
            var scanline = new Span<byte>(blockWidth);
            var img = new Span<byte>(blockImg);
            var buf2 = new Span<byte>(block2);

            var ipos = 0;

            for (uint j = 0; j < height; j++)
            {
                stream.ReadExactly(rgbe);

                var isNewRLE = (rgbe[0] == 2 && rgbe[1] == 2 && rgbe[2] == ((width >> 8) & 0xFF) && rgbe[3] == (width & 0xFF));

                if (isNewRLE && (width >= 8) && (width < 32768))
                {
                    for (var i = 0; i < 4; i++)
                    {
                        var ptr = i * width;
                        var ptr_end = (i + 1) * width;
                        int count;
                        while (ptr < ptr_end)
                        {
                            stream.ReadExactly(buf2);
                            if (buf2[0] > 128)
                            {
                                count = buf2[0] - 128;
                                while (count-- > 0)
                                    scanline[ptr++] = buf2[1];
                            }
                            else
                            {
                                count = buf2[0] - 1;
                                scanline[ptr++] = buf2[1];
                                while (count-- > 0)
                                    scanline[ptr++] = (byte)stream.ReadByte();
                            }
                        }
                    }
                    for (var i = 0; i < width; i++)
                    {
                        img[ipos++] = scanline[i + 0 * width];
                        img[ipos++] = scanline[i + 1 * width];
                        img[ipos++] = scanline[i + 2 * width];
                        img[ipos++] = scanline[i + 3 * width];
                    }
                }
                else
                {
                    img[ipos++] = rgbe[0];
                    img[ipos++] = rgbe[1];
                    img[ipos++] = rgbe[2];
                    img[ipos++] = rgbe[3];

                    for (var i = 1; i < width; i++)
                    {
                        stream.ReadExactly(rgbe);

                        img[ipos++] = rgbe[0];
                        img[ipos++] = rgbe[1];
                        img[ipos++] = rgbe[2];
                        img[ipos++] = rgbe[3];
                    }
                }

                Debug.Assert(stream.Position <= stream.Length);
            }


            var dst = new float[width * height * 3];
            var dstSpan = new Span<float>(dst);

            int i1 = 0, i2 = 0;

            float s;


            while (i2 < dst.Length)
            {
                s = MathF.Pow(2, img[i1 + 3] - (136));

                dstSpan[i2++] = img[i1++] * s;
                dstSpan[i2++] = img[i1++] * s;
                dstSpan[i2++] = img[i1++] * s;

                i1++;
            }

            fixed (float* pData = dst)
            {
                return [new TextureData {
                    Compression = TextureCompressionFormat.Uncompressed,
                    Data = new Span<byte>((byte*)pData, dst.Length * 4).ToArray(),
                    Format = TextureFormat.RgbFloat32,
                    Height = (uint) height,
                    Width =  (uint)width,
                }];
            }

        }

        protected override bool CanHandleExtension(string extension)
        {
            return extension == ".hdr";
        }

        public static readonly HdrReader Instance = new();
    }
}
