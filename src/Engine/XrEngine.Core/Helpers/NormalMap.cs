using Common.Interop;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public static class NormalMap
    {
        public static Texture2D FromHeightMap(Texture2D map, float strength)
        {
            if (map.Data == null || map.Data!.Count == 0 || map.Data[0].Data == null)
                throw new InvalidOperationException("Texture data is empty");   
            return FromHeightMap(map.Data[0], strength);    
        }

        public static Texture2D FromHeightMap(TextureData data, float strength)
        {
            var pixelSize = ImageUtils.GetPixelSizeByte(data.Format);
            
            using var skImage = ImageUtils.ToBitmap(data, false);

            return FromHeightMap(skImage, strength);
        }

        public static unsafe Texture2D FromHeightMap(SKBitmap img, float strength)
        {
            using var blurred = ImageUtils.ApplyGaussianBlur(img, 5);

            var width = blurred.Width;
            var height = blurred.Height;
            var pixelSize = blurred.BytesPerPixel;

            var res = MemoryBuffer.Create<byte>((uint)(width * height * 4));

            fixed (byte* srcData = blurred.Bytes)
            {
                var dstData = res.Lock();


                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        float tl = srcData[((y - 1) * width + (x - 1)) * pixelSize] / 255f;
                        float t = srcData[((y - 1) * width + x) * pixelSize] / 255f;
                        float tr = srcData[((y - 1) * width + (x + 1)) * pixelSize] / 255f;
                        float l = srcData[(y * width + (x - 1)) * pixelSize] / 255f;
                        float r = srcData[(y * width + (x + 1)) * pixelSize] / 255f;
                        float bl = srcData[((y + 1) * width + (x - 1))   * pixelSize] / 255f;
                        float b = srcData[((y + 1) * width + x) * pixelSize] / 255f;
                        float br = srcData[((y + 1) * width + (x + 1)) * pixelSize] / 255f;

                        // Compute gradients dx and dy using Sobel operator
                        float dX = (tl + 2 * l + bl) - (tr + 2 * r + br);
                        float dY = (tl + 2 * t + tr) - (bl + 2 * b + br);

                        var normal = Vector3.Normalize(new Vector3(dX, dY, strength));

                        var dstOfs = (y * width + x) * 4;

                        dstData[dstOfs] = (byte)((normal.X * 0.5f + 0.5f) * 255);
                        dstData[dstOfs + 1] = (byte)((normal.Y * 0.5f + 0.5f) * 255);
                        dstData[dstOfs + 2] = (byte)(normal.Z * 255);
                        dstData[dstOfs + 3] = 255;
                    }
                }

                res.Unlock();
            } 

            return Texture2D.FromData([new TextureData
            {
                Data = res,
                Width = (uint)width,
                Height = (uint)height,
                Format = TextureFormat.Rgba32,
            }]);
        }
    }
}
