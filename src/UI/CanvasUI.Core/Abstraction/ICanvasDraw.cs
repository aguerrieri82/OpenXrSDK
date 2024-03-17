using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanvasUI
{
    public interface ICanvasDraw
    {
        void Draw(SKCanvas canvas);   
    }
}
