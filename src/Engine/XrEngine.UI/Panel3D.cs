using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine.UI
{
    public class Panel3D : Canvas3D
    {
        public Panel3D()
        {
            AddComponent(new MeshCollider());
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();

            Panel?.SetViewport(0, 0, _pixelSize.Width / _dpiScale, _pixelSize.Height / _dpiScale);
        }

        protected override void Draw(SKCanvas canvas)
        {
            if (Panel != null && Panel.IsDirty)
            {
                canvas.Clear();
                Panel.Draw(canvas);
            }
        }

        protected override bool NeedDraw => Panel != null && Panel.IsDirty;

        public UIRoot? Panel { get; set; }  
    }
}
