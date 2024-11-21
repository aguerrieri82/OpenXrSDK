using CanvasUI;
using SkiaSharp;
using System.Windows.Controls;
using XrEngine;
using XrMath;

namespace XrEditor.Plot
{
    [Panel("Draw")]
    [StateManager(StateManagerMode.Explicit)]
    public class DrawPanel : BasePanel, IFunctionView
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
            Context.Implement<IFunctionView>(this);
        }

        protected void Draw(SKCanvas canvas, Rect2 rect)
        {
            canvas.Clear();
            foreach (var draw in _draws)
                draw.Draw(canvas, rect);
        }

        public void ShowDft(float[] data, uint sampleRate, uint size)
        {
            _draws.Clear();
            _draws.Add(new DrawDft(data, sampleRate, size));
            Content.Invalidate();
        }

        public DrawElement Content { get; }

        public override string? Title => "Draw";
    }
}
