using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine.UI
{
    public class UIRoot : UiPanel
    {
        Rect2 _viewport;

        protected void Layout()
        {
            Measure(new Size2(_viewport.Width, _viewport.Height));
            Arrange(_viewport);
        }

        public override void Draw(SKCanvas canvas)
        {
            if (_isLayoutDirty)
                Layout();

            base.Draw(canvas);
        }

        internal void SetViewport(float x, float y, float width, float height)
        {
            _viewport = new Rect2(x, y, width, height);
            _isLayoutDirty = true;
        }
    }
}
