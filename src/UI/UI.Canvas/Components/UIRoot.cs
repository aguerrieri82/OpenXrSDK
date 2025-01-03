﻿using SkiaSharp;
using XrMath;

namespace CanvasUI
{
    public class UIRoot : UiContainer
    {
        Rect2 _viewport;

        public UIRoot()
        {
            Style.LineSize = UnitValue.Get(1, Unit.Em);
            Style.TextAlign = UiAlignment.Start;
            Style.FontSize = UnitValue.Get(16);
            Style.FontFamily = "sans-serif";
            Style.TextWrap = UiTextWrap.Whitespaces;
            Style.Color = Color.Black;
        }

        protected override void OnNeedRedraw()
        {
            NeedRedraw?.Invoke(this, EventArgs.Empty);
        }

        protected void Layout()
        {
            Measure(new Size2(_viewport.Width, _viewport.Height));
            Arrange(_viewport);
            _isLayoutDirty = false;
        }

        public override void Draw(SKCanvas canvas)
        {
            if (_isLayoutDirty)
                Layout();

            base.Draw(canvas);
        }

        public void SetViewport(float x, float y, float width, float height)
        {
            var newView = new Rect2(x, y, width, height);

            if (Equals(newView, _viewport))
                return;

            _viewport = newView;
            _clientRect = _viewport;
            _isLayoutDirty = true;
        }


        public event EventHandler? NeedRedraw;
    }
}
