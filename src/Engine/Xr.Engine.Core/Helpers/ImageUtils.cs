using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine
{
    public static class ImageUtils
    {
        public static SKBitmap ChangeColorSpace(SKBitmap src, SKColorType dest)
        {
            if (src.ColorType == dest)
                return src;

            var newInfo = new SKImageInfo(src.Width, src.Height, dest);

            var newBitmap = new SKBitmap(newInfo);

            using var canvas = new SKCanvas(newBitmap);

            canvas.Clear(SKColors.Transparent);

            canvas.DrawBitmap(src, 0, 0);

            src.Dispose();

            return newBitmap;
        }

    }
}
