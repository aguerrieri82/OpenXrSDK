using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.UI
{
    public class CanvasView2D : CanvasView3D
    {
        ScreenCanvas? screenCanvas;

        public CanvasView2D()
        {
            Flags |= EngineObjectFlags.NoFrustumCulling;
        }

        protected override void Draw(SKCanvas canvas)
        {
            screenCanvas ??= new ScreenCanvas();

            screenCanvas.Configure(canvas, _lastCamera!, _pixelSize);

            canvas.Save();

            canvas.Clear();
   
            DrawCanvas?.Invoke(screenCanvas);

            canvas.Restore();
        }

        protected override Material CreateMaterial(Texture2D texture)
        {
            return new TextureClipMaterial()
            {
                Texture = texture,
                Alpha = AlphaMode.Blend,
                DoubleSided = true,
                UseDepth = false,
                WriteDepth = false, 
                Priority = 10
            };
        }

        [Action]
        public void FilpY()
        {
            _geometry!.FlipYUV();

        }

        public event Action<ScreenCanvas>? DrawCanvas;

        public override bool NeedDraw => true;
    }
}
