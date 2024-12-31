using CanvasUI;
using SkiaSharp;
using System.ComponentModel;
using System.Windows.Controls;
using XrEngine;
using XrMath;

namespace XrEditor.Plot
{
    [Panel("58cd65ac-f832-4cad-84ae-4bfcb3375c77")]
    [DisplayName("Draw")]
    [StateManager(StateManagerMode.Explicit)]
    public class DrawPanel : BasePanel
    {
        readonly List<IDraw2D> _draws = [];

        #region DrawElement

        public class DrawElement : UiElement
        {
            readonly Action<SKCanvas, Rect2> _draw;

            public DrawElement(Action<SKCanvas, Rect2> draw)
            {
                _draw = draw;
            }

            protected override void DrawWork(SKCanvas canvas)
            {
                _draw(canvas, _contentRect);
            }
        }

        #endregion

        public DrawPanel()
        {
            Content = new DrawElement(Draw);
        }

        protected void Draw(SKCanvas canvas, Rect2 rect)
        {
            canvas.Clear();
            foreach (var draw in _draws)
                draw.Draw(canvas, rect);
        }

        public DrawElement Content { get; }

        public override string? Title => "Draw";
    }
}
