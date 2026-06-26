using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace XrEngine.UI
{


    public class CanvasView2D : CanvasView3D
    {
        ScreenCanvas? screenCanvas;

        public CanvasView2D()
        {
            Flags |= EngineObjectFlags.NoFrustumCulling;
            IsStereo = true;    
        }

        protected override void Draw(SKCanvas canvas, RenderContext? ctx, int activeEye)
        {
            Debug.Assert(ctx?.Camera != null);

            if (activeEye > 0 && (ctx.Camera.Eyes == null || ctx.Camera.Eyes.Length < 2))
                return;

            screenCanvas ??= new ScreenCanvas();

            canvas.Save();

            screenCanvas.Configure(canvas, ctx.Camera, activeEye, _pixelSize);

            canvas.Clear();
   
            DrawCanvas?.Invoke(screenCanvas);

            canvas.Restore();
        }

        protected override Material CreateMaterial(Texture2D leftMain, Texture2D? right)
        {
            return new TextureClipMaterial()
            {
                MainLeftTexture = leftMain,
                RightTexture = right,
                Alpha = AlphaMode.Blend,
                IsStereo = IsStereo,
                DoubleSided = true,
                UseDepth = false,
                WriteDepth = false,
                Priority = 10
            };
        }

        public event Action<ScreenCanvas>? DrawCanvas;

        public override bool NeedDraw => true;
    }
}
