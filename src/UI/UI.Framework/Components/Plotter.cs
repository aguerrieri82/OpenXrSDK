using SkiaSharp;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Numerics;
using XrMath;

namespace CanvasUI.Components
{
    #region ENUMS

    public enum SerieSampleMode
    {
        Nearest,
        Linear
    }

    public enum AutoScaleYMode
    {
        None,
        Window,
        Serie
    }

    public enum AutoScaleXMode
    {
        None,
        Fit,
        Advance
    }


    #endregion

    #region Bounds1

    public struct Bounds1
    {
        public Bounds1()
        {

        }

        public Bounds1(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Min;

        public float Max;

        public float Length => Max - Min;

        public float Center => (Max + Min) / 2;
    }

    #endregion

    #region IPlotterSerie

    public interface IPlotterSerie
    {
        float ValueAt(float x);

        Bounds1 GetMinMaxX();

        Bounds1 GetMinMaxYAt(Bounds1 xRange);

        IEnumerable<Vector2> Sample(Bounds1 xRange, int sampleCount);

        void Attach(Plotter plotter);

        void Detach(Plotter plotter);

        void NotifyChanged();

        string? Name { get; }

        Color Color { get; }

        bool IsVisible { get; }

        Func<float, string> FormatValue { get; }
    }

    #endregion

    #region BasePlotterSerie

    public abstract class BasePlotterSerie : IPlotterSerie
    {
        Plotter? _host;

        public BasePlotterSerie()
        {
            FormatValue = (v) => v.ToString();
            IsVisible = true;
        }

        public virtual IEnumerable<Vector2> Sample(Bounds1 xRange, int sampleCount)
        {
            float sampleSize = xRange.Length / sampleCount;
            float curX = xRange.Min;

            while (curX <= xRange.Max)
            {
                float y = ValueAt(curX);
                yield return new Vector2(curX, y);
                curX += sampleSize;
            }
        }

        void IPlotterSerie.Attach(Plotter plotter)
        {
            _host = plotter;
            NotifyChanged();
        }

        void IPlotterSerie.Detach(Plotter plotter)
        {
            if (_host == plotter)
                _host = null;
        }

        public void NotifyChanged()
        {
            _host?.NotifyChanged(this);
        }

        public abstract float ValueAt(float x);

        public abstract Bounds1 GetMinMaxX();

        public abstract Bounds1 GetMinMaxYAt(Bounds1 xRange);

        public Func<float, string> FormatValue { get; set; }

        public string? Name { get; set; }

        public Color Color { get; set; }

        public bool IsVisible { get; set; }

    }

    #endregion

    #region FunctionPlotterSerie

    public class FunctionPlotterSerie : BasePlotterSerie
    {
        private readonly Func<float, float> _func;
        Bounds1 _xRange;
        Bounds1 _yRange;

        public FunctionPlotterSerie(Func<float, float> func, Bounds1 xRange, Bounds1 yRange)
        {
            _func = func;
            _xRange = xRange;
            _yRange = yRange;
        }

        public override Bounds1 GetMinMaxX()
        {
            return _xRange;
        }

        public override Bounds1 GetMinMaxYAt(Bounds1 xRange)
        {
            return _yRange;
        }

        public override float ValueAt(float x)
        {
            return _func(x);
        }
    }

    #endregion

    #region DiscretePlotterSerie

    public class DiscretePlotterSerie : BasePlotterSerie
    {
        Bounds1 _yRange;

        public DiscretePlotterSerie()
        {
            Points = [];
            SampleMode = SerieSampleMode.Nearest;
            MaxGapX = float.PositiveInfinity;
        }

        public void AppendValue(float x, float y, bool notify = true)
        {
            if (Points.Count == 0)
            {
                _yRange.Min = y;
                _yRange.Max = y;
            }
            else
            {
                _yRange.Min = MathF.Min(y, _yRange.Min);
                _yRange.Max = MathF.Max(y, _yRange.Max);
            }

            if (Points.Count > 0 && Points[Points.Count - 1].X == x)
                Points[Points.Count - 1] = new Vector2(x, y);
            else
                Points.Add(new Vector2(x, y));

            if (notify)
                NotifyChanged();

        }

        public void Clear()
        {
            _yRange.Min = 0;
            _yRange.Max = 0;
            Points.Clear();
            NotifyChanged();
        }

        public override float ValueAt(float x)
        {
            switch (SampleMode)
            {
                case SerieSampleMode.Nearest:
                    int index = IndexOfClosestX(x);
                    if (index == -1)
                        return float.NaN;
                    return Points[index].Y;
                case SerieSampleMode.Linear:
                    return InterpolateY(x);
                default:
                    throw new NotImplementedException();
            }
        }

        public override IEnumerable<Vector2> Sample(Bounds1 xRange, int sampleCount)
        {
            if (SampleMode == SerieSampleMode.Nearest)
            {
                float curIndex = IndexOfClosestX(xRange.Min);
                if (curIndex == -1)
                    yield break;

                int endIndex = IndexOfClosestX(xRange.Max);

                float skip = (endIndex - curIndex) / sampleCount;

                if (skip < 1)
                    skip = 1;

                Vector2 lastPoint = new Vector2(float.NaN, float.NaN);

                float maxIndex = curIndex;

                while (curIndex < Points.Count)
                {
                    Vector2 curPoint = Points[(int)curIndex];

                    if (curPoint.X > xRange.Max)
                        break;

                    if (!float.IsNaN(lastPoint.X))
                    {
                        if ((curPoint.X - lastPoint.X) > MaxGapX && skip <= 1)
                            yield return new Vector2(float.NaN, float.NaN);
                        else if (curPoint.X != lastPoint.X)
                            yield return new Vector2(curPoint.X, lastPoint.Y);
                    }

                    yield return curPoint;

                    lastPoint = curPoint;

                    curIndex += skip;
                }
            }
            else
            {
                foreach (Vector2 item in base.Sample(xRange, sampleCount))
                    yield return item;
            }
        }

        public float InterpolateY(int index, float targetX)
        {
            if (index == Points.Count - 1)
                return Points[index].Y;

            Vector2 p1 = Points[index];
            Vector2 p2 = Points[index + 1];

            float t = (targetX - p1.X) / (p2.X - p1.X);
            return p1.Y + (p2.Y - p1.Y) * t;
        }

        public float InterpolateY(float targetX)
        {
            int index = IndexOfClosestX(targetX);
            if (index == -1)
                return float.NaN;
            return InterpolateY(index, targetX);
        }

        public int IndexOfClosestX(float targetX)
        {
            if (Points.Count == 0)
                return -1;

            int left = 0;
            int right = Points.Count - 1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;

                if (Points[mid].X == targetX)
                {
                    return mid;
                }
                else if (Points[mid].X < targetX)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            if (right == -1)
                right = 0;

            return right;
        }

        public override Bounds1 GetMinMaxX()
        {
            if (Points.Count == 0)
            {
                return new Bounds1
                {
                    Max = float.NaN,
                    Min = float.NaN
                };
            }
            return new Bounds1
            {
                Min = Points[0].X,
                Max = Points[Points.Count - 1].X
            };
        }

        public override Bounds1 GetMinMaxYAt(Bounds1 xRange)
        {
            if (xRange.Equals(GetMinMaxX()))
                return _yRange;

            Bounds1 result = new Bounds1
            {
                Min = float.PositiveInfinity,
                Max = float.NegativeInfinity
            };

            int index = IndexOfClosestX(xRange.Min);
            if (index == -1)
                return result;

            while (index < Points.Count)
            {
                Vector2 point = Points[index];
                if (point.X > xRange.Max)
                    break;
                result.Min = Math.Min(result.Min, point.Y);
                result.Max = Math.Max(result.Max, point.Y);
                index++;
            }

            return result;
        }

        public float MaxGapX { get; set; }

        public SerieSampleMode SampleMode { get; set; }

        public IList<Vector2> Points { get; set; }

    }

    #endregion

    #region BasePlotterTool

    public abstract class BasePlotterTool
    {
        protected readonly Plotter _plotter;

        protected Rect2 _maxViewRect;
        protected Rect2 _startViewRect;
        protected Vector2 _startPos;
        protected Vector2 _startValue;
        protected bool _isCapture;

        public BasePlotterTool(Plotter plotter)
        {
            _plotter = plotter;
            _plotter.PointerDown += OnPointerDown;
        }

        protected Vector2 GetValue(UiPointerEvent uiEvent)
        {
            Vector2 pos = uiEvent.WindowPosition;
            return new Vector2(_plotter.PixelToValueX(pos.X), _plotter.PixelToValueY(pos.Y));
        }

        protected virtual bool NotifyDown(UiPointerEvent ev, Vector2 value)
        {
            return false;
        }

        protected virtual bool NotifyMove(UiPointerEvent ev, Vector2 value)
        {
            return false;
        }


        protected void OnPointerDown(UiElement sender, UiPointerEvent uiEvent)
        {
            if (NotifyDown(uiEvent, GetValue(uiEvent)))
                StartCapture(uiEvent);
        }

        protected void OnPointerUp(UiElement sender, UiPointerEvent uiEvent)
        {
            EndCapture(uiEvent);
        }

        protected void OnPointerMove(UiElement sender, UiPointerEvent uiEvent)
        {
            if (_isCapture)
            {
                if (!NotifyMove(uiEvent, GetValue(uiEvent)))
                    EndCapture(uiEvent);

                _plotter.ComputeMetrics();
            }

        }

        protected virtual void StartCapture(UiPointerEvent uiEvent)
        {
            if (_isCapture)
                return;

            _startPos = uiEvent.WindowPosition;
            _startValue = GetValue(uiEvent);
            _startViewRect = _plotter.ViewRect;
            _maxViewRect = _plotter.GetMaxViewRect();
            _isCapture = true;

            _plotter.PointerMove += OnPointerMove;
            _plotter.PointerUp += OnPointerUp;

            uiEvent.Pointer!.Capture(_plotter);

            _plotter._activeTool = this;
        }

        protected virtual void EndCapture(UiPointerEvent uiEvent)
        {
            if (!_isCapture)
                return;

            _plotter.PointerMove -= OnPointerMove;
            _plotter.PointerUp -= OnPointerUp;
            _isCapture = false;

            uiEvent.Pointer!.Release();

            _plotter._activeTool = null;
        }
    }

    #endregion

    #region PanPlotterTool

    public class PanPlotterTool : BasePlotterTool
    {
        public PanPlotterTool(Plotter plotter)
            : base(plotter)
        {
            CanPanX = true;
            CanPanY = true;
        }

        protected override bool NotifyDown(UiPointerEvent ev, Vector2 value)
        {
            return ev.Pointer!.Buttons == UiPointerButton.Left;
        }

        protected override bool NotifyMove(UiPointerEvent ev, Vector2 value)
        {
            Vector2 pos = ev.WindowPosition;

            if (CanPanX)
            {
                float minX = _startViewRect.X + (_startPos.X - pos.X) / _plotter.PixelPerUnitX;

                if (minX > _maxViewRect.Right - _startViewRect.Width)
                    minX = _maxViewRect.Right - _startViewRect.Width;

                if (minX < _maxViewRect.X - _startViewRect.Width)
                    minX = _maxViewRect.X - _startViewRect.Width;

                _plotter.MinX = minX;
            }

            if (CanPanY)
            {
                float minY = _startViewRect.Y - (_startPos.Y - pos.Y) / _plotter.PixelPerUnitY;

                if (minY > _maxViewRect.Bottom - _startViewRect.Height)
                    minY = _maxViewRect.Bottom - _startViewRect.Height;

                if (minY < _maxViewRect.Y)
                    minY = _maxViewRect.Y;

                _plotter.MinY = minY;
            }

            _plotter.OnViewChanged();

            return true;
        }

        public bool CanPanX { get; set; }

        public bool CanPanY { get; set; }

    }

    #endregion

    #region ScalePlotterTool

    public class ScalePlotterTool : BasePlotterTool
    {
        Vector2 _startScale;

        public ScalePlotterTool(Plotter plotter)
            : base(plotter)
        {

        }

        protected override bool NotifyDown(UiPointerEvent ev, Vector2 value)
        {
            return ev.Pointer!.Buttons == UiPointerButton.Right;
        }

        protected override void StartCapture(UiPointerEvent uiEvent)
        {
            _startScale.X = _plotter.PixelPerUnitX;
            _startScale.Y = _plotter.PixelPerUnitY;

            base.StartCapture(uiEvent);
        }

        protected override bool NotifyMove(UiPointerEvent ev, Vector2 value)
        {
            Vector2 pos = ev.WindowPosition;

            Vector2 delta = (pos - _startPos);

            bool ctrl = (ev.Modifiers & UiModifier.Ctrl) != 0;

            float scaleFactor = MathF.Pow(2, delta.X * 0.01f); ;

            if (!ctrl)
            {
                _plotter.PixelPerUnitX = MathF.Max(0.1f, _startScale.X * scaleFactor);
            }
            else
            {
                _plotter.PixelPerUnitY = MathF.Max(0.1f, _startScale.Y * scaleFactor);
            }

            return true;
        }

    }

    #endregion

    public class PlotterReference
    {
        public float Value { get; set; }

        public string? Name { get; set; }

        public Color Color { get; set; }
    }

    public class Plotter : UiElement
    {
        protected internal BasePlotterTool? _activeTool;

        protected Rect2 _chartArea;
        protected int _updateView;
        protected Rect2 _legendArea;
        protected Rect2 _xAxisArea;
        protected Rect2 _yAxisArea;
        protected readonly List<BasePlotterTool> _tools;
        protected readonly Rect2Animation _viewAnimation;

        public Plotter()
        {
            Series = [];
            ReferencesX = [];
            ReferencesY = [];

            this.BuildStyle(a => a.Overflow(UiOverflow.Hidden));

            FormatValueX = v => v.ToString();

            _tools = [];
            _tools.Add(new PanPlotterTool(this));
            _tools.Add(new ScalePlotterTool(this));

            _viewAnimation = new Rect2Animation();
            _viewAnimation.ValueChanged += (s, e) =>
            {
                lock (this)
                {
                    ViewRect = e;
                    UpdatePlotValues(ViewRect);
                }
            };
        }

        public T Tool<T>() where T : BasePlotterTool
        {
            return _tools.OfType<T>().Single();
        }

        protected override void OnPropertyChanged(UiProperty prop, object? value, object? oldValue)
        {
            if (prop.Flags == UiPropertyFlags.Render)
            {
                //ComputeMetrics();
                IsDirty = true;
            }

            base.OnPropertyChanged(prop, value, oldValue);
        }

        protected virtual void OnViewRectChanged(Rect2 value, Rect2 oldValue)
        {
            UpdatePlotValues(value);
        }

        protected void UpdatePlotValues(Rect2 viewRect)
        {
            if (_updateView > 0)
                return;

            _updateView++;

            MinX = viewRect.X;
            MinY = viewRect.Y;
            PixelPerUnitX = _chartArea.Width / viewRect.Width;
            PixelPerUnitY = _chartArea.Height / viewRect.Height;

            _updateView--;
        }

        protected virtual void OnSeriesChanged(ObservableCollection<IPlotterSerie> value, ObservableCollection<IPlotterSerie> oldValue)
        {
            if (oldValue != null)
                oldValue.CollectionChanged -= OnSeriesChanged;

            if (value != null)
                value.CollectionChanged += OnSeriesChanged;
        }

        protected virtual void OnCheckPointsChanged(ObservableCollection<PlotterReference> value, ObservableCollection<PlotterReference> oldValue)
        {
            if (oldValue != null)
                oldValue.CollectionChanged -= OnCheckPointsChanged;

            if (value != null)
                value.CollectionChanged += OnCheckPointsChanged;
        }

        private void OnCheckPointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            IsDirty = true;
            ComputeMetrics(true, true);
        }

        private void OnSeriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (IPlotterSerie item in e.NewItems.OfType<IPlotterSerie>())
                    item.Attach(this);
            }
            if (e.OldItems != null)
            {
                foreach (IPlotterSerie item in e.OldItems.OfType<IPlotterSerie>())
                    item.Detach(this);
            }
        }

        public float PixelToValueX(float x)
        {
            return MinX + (x - _chartArea.X) / PixelPerUnitX;
        }

        public float PixelToValueY(float y)
        {
            return MinY + (_chartArea.Bottom - y) / PixelPerUnitY;
        }


        public float ValueToPixelY(float valueY)
        {
            return _chartArea.Bottom - ((valueY - MinY) * PixelPerUnitY);
        }

        public float ValueToPixelX(float valueX)
        {
            return _chartArea.Left + ((valueX - MinX) * PixelPerUnitX);
        }

        protected override void DrawWork(SKCanvas canvas)
        {
            lock (this)
            {
                IEnumerable<IPlotterSerie> curSeries = Series.ToArray().Where(a => a.IsVisible);

                SKFont font = ActualStyle.GetFont();

                if (ShowLegend)
                {
                    float curLabelY = _chartArea.Y + font.Size;

                    foreach (IPlotterSerie? serie in curSeries)
                    {
                        SKPaint legendColor = SKResources.FillColor(serie.Color);

                        string label = GetLegendLabel(serie);
                        canvas.DrawText(label, new SKPoint(_legendArea.X, curLabelY), font, legendColor);

                        curLabelY += font.Size + 4;
                    }
                }

                if (ShowAxisX || ShowGridX)
                {
                    float tickInterval = TickIntervalX;
                    if (tickInterval == 0)
                        tickInterval = PixelToValueX(50) - PixelToValueX(0);

                    float curX = MathF.Round(MinX / tickInterval) * tickInterval;
                    if (curX < ViewRect.X)
                        curX += tickInterval;

                    float lastLabelPx = 0f;

                    SKPaint tickPaint = SKResources.Stroke("#ccc", 1);
                    SKPaint gridPaint = SKResources.Stroke("#777", 1, 2);
                    SKPaint labelPaint = SKResources.FillColor("#ccc");

                    if (ShowAxisX)
                    {
                        canvas.DrawLine(
                            new SKPoint(_xAxisArea.X, _xAxisArea.Bottom),
                            new SKPoint(_xAxisArea.Right, _xAxisArea.Bottom),
                            tickPaint);
                    }

                    while (curX < ViewRect.Right)
                    {
                        float px = ValueToPixelX(curX);

                        if (ShowAxisX)
                        {
                            canvas.DrawLine(
                                new SKPoint(px, _xAxisArea.Y + font.Size + 2),
                                new SKPoint(px, _xAxisArea.Bottom),
                                tickPaint);

                            string label = FormatValueX(curX);
                            float labelSize = font.MeasureText(label);
                            float labelStartPx = px - labelSize / 2f;

                            if (labelStartPx > lastLabelPx)
                            {
                                canvas.DrawText(label,
                                    new SKPoint(px - labelSize / 2f, _xAxisArea.Y + font.Size),
                                    font,
                                    labelPaint);

                                lastLabelPx = px + labelSize / 2f;
                            }
                        }

                        if (ShowGridX)
                        {
                            canvas.DrawLine(
                                new SKPoint(px, _chartArea.Y),
                                new SKPoint(px, _chartArea.Bottom),
                                gridPaint);
                        }

                        curX += tickInterval;
                    }
                }

                if (ShowAxisY || ShowGridY)
                {
                    float curY;

                    float tickInterval = TickIntervalY;

                    if (tickInterval == 0)
                    {
                        tickInterval = PixelToValueY(0) - PixelToValueY(50);
                        curY = MinY;
                    }
                    else
                    {
                        curY = MathF.Round(MinY / tickInterval) * tickInterval;
                        if (curY < ViewRect.Y)
                            curY += tickInterval;
                    }

                    float lastLabelPy = 0f;

                    SKPaint tickPaint = SKResources.Stroke("#ccc", 1);
                    SKPaint gridPaint = SKResources.Stroke("#777", 1, 2);
                    SKPaint labelPaint = SKResources.FillColor("#ccc");

                    if (ShowAxisY)
                    {
                        canvas.DrawLine(
                            new SKPoint(_yAxisArea.Right, _yAxisArea.Y),
                            new SKPoint(_yAxisArea.Right, _yAxisArea.Bottom),
                            tickPaint);
                    }

                    IPlotterSerie? activeSerie = curSeries.FirstOrDefault();

                    while (curY < ViewRect.Bottom && tickInterval > 0)
                    {
                        float py = ValueToPixelY(curY) - 1;

                        if (ShowAxisY)
                        {
                            canvas.DrawLine(
                                new SKPoint(_yAxisArea.Right, py),
                                new SKPoint(_yAxisArea.Right - 4, py),
                                tickPaint);


                            string label = activeSerie?.FormatValue(curY) ?? "";
                            float labelSize = font.MeasureText(label);
                            float labelStartPy = py - font.Size / 2 + font.Size;

                            if (labelStartPy > lastLabelPy || true)
                            {
                                canvas.DrawText(label,
                                    new SKPoint(_yAxisArea.Right - 6 - labelSize, py),
                                    font,
                                    labelPaint);

                                lastLabelPy = labelStartPy + font.Size;
                            }

                        }

                        if (ShowGridY)
                        {
                            canvas.DrawLine(
                                new SKPoint(_chartArea.X, py),
                                new SKPoint(_chartArea.Right, py),
                                gridPaint);
                        }

                        curY += tickInterval;
                    }

                }


                canvas.ClipRect(_chartArea.ToSKRect());

                Bounds1 sampleArea = new Bounds1(ViewRect.X, ViewRect.Right);

                foreach (IPlotterSerie? serie in curSeries)
                {
                    SKPoint prevPoint = new SKPoint(float.NaN, float.NaN);

                    SKPaint paint = SKResources.Stroke(serie.Color, 1);

                    foreach (Vector2 sample in serie.Sample(sampleArea, (int)_chartArea.Width))
                    {
                        float px = ValueToPixelX(sample.X);
                        float py = ValueToPixelY(sample.Y);
                        SKPoint curPoint = new SKPoint(px, py);
                        if (!float.IsNaN(prevPoint.Y))
                            canvas.DrawLine(prevPoint, curPoint, paint);
                        prevPoint = curPoint;
                    }
                }

                float pixelX = ValueToPixelX(CursorX);

                canvas.DrawLine(new SKPoint(pixelX, _chartArea.Y),
                                new SKPoint(pixelX, _chartArea.Bottom),
                                SKResources.Stroke("#ccc", 1));

                SKPaint labelFill = SKResources.FillColor("#00000080");


                foreach (PlotterReference cp in ReferencesX)
                {
                    if (cp.Value < ViewRect.X || cp.Value > ViewRect.Right)
                        continue;

                    pixelX = ValueToPixelX(cp.Value);

                    canvas.DrawLine(new SKPoint(pixelX, _chartArea.Y),
                                  new SKPoint(pixelX, _chartArea.Bottom),
                                  SKResources.Stroke(cp.Color, 2));
                }

                float curOfs = _chartArea.Bottom - 16;

                foreach (PlotterReference cp in ReferencesX)
                {
                    if (cp.Value < ViewRect.X || cp.Value > ViewRect.Right)
                        continue;

                    pixelX = ValueToPixelX(cp.Value);

                    if (cp.Name != null)
                    {
                        float nameSize = font.MeasureText(cp.Name);
                        canvas.DrawRect(pixelX + 4, curOfs - font.Size, nameSize + 8, font.Size + 8, labelFill);

                        canvas.DrawText(cp.Name,
                            new SKPoint(pixelX + 8, curOfs + 2),
                            font,
                            SKResources.FillColor(cp.Color));

                        curOfs -= font.Size + 16;
                    }
                }


                foreach (PlotterReference cp in ReferencesY)
                {
                    if (cp.Value < ViewRect.Y || cp.Value > ViewRect.Bottom)
                        continue;

                    float pixelY = ValueToPixelY(cp.Value);

                    canvas.DrawLine(new SKPoint(_chartArea.X, pixelY),
                                  new SKPoint(_chartArea.Right, pixelY),
                                  SKResources.Stroke(cp.Color, 1));
                }

            }
        }

        protected override void OnSizeChanged()
        {
            lock (this)
            {
                ComputeLayout();
                ComputeMetrics();
            }

            OnViewChanged();

            base.OnSizeChanged();
        }

        public void NotifyChanged(IPlotterSerie? serie)
        {
            lock (this)
            {
                if (Parent == null)
                    return;

                if (_activeTool != null)
                    return;

                Invalidate();
                ComputeLayout();
                ComputeMetrics(true, true);
            }
        }

        protected string GetLegendLabel(IPlotterSerie serie)
        {
            float value = serie.ValueAt(CursorX);
            string valueText = float.IsNaN(value) ? "" : serie.FormatValue(value);
            return $"{serie.Name}: {valueText}";
        }

        protected void ComputeLayout()
        {
            if (_contentRect.Width == 0 || _contentRect.Height == 0)
                return;

            _chartArea = _contentRect;

            SKFont font = ActualStyle.GetFont();

            if (ShowLegend)
            {
                float maxW = float.NegativeInfinity;

                if (LegendWidth == 0)
                {
                    foreach (IPlotterSerie? serie in Series.Where(a => a.IsVisible))
                        maxW = MathF.Max(maxW, font.MeasureText(GetLegendLabel(serie)));

                    if (float.IsInfinity(maxW))
                        maxW = 0;
                }
                else
                    maxW = LegendWidth;

                _chartArea.Left += maxW + 16;
                _legendArea = new Rect2(_contentRect.X, _contentRect.Y, maxW, _contentRect.Height);
            }

            if (ShowAxisX)
            {
                _xAxisArea = new Rect2(_chartArea.X, _contentRect.Y, _chartArea.Width, font.Size + 8);
                _chartArea.Top += _xAxisArea.Height + 4;
            }

            if (ShowAxisY)
            {
                _yAxisArea = new Rect2(_chartArea.X, _chartArea.Y, LabelWidthY, _chartArea.Height);
                _chartArea.Left += _yAxisArea.Width + 4;
                _xAxisArea.Left = _chartArea.Left;
            }
        }

        public Rect2 GetMaxViewRect()
        {
            IEnumerable<IPlotterSerie> series = Series.Where(a => a.IsVisible);

            Bounds1 minMaxX = new();
            Bounds1 minMaxY = new();

            minMaxX.Min = float.PositiveInfinity;
            minMaxX.Max = float.NegativeInfinity;

            minMaxY.Min = float.PositiveInfinity;
            minMaxY.Max = float.NegativeInfinity;

            foreach (IPlotterSerie? serie in series)
            {
                Bounds1 rangeX = serie.GetMinMaxX();
                if (float.IsNaN(rangeX.Min))
                    continue;

                minMaxX.Min = MathF.Min(rangeX.Min, minMaxX.Min);
                minMaxX.Max = MathF.Max(rangeX.Max, minMaxX.Max);

                Bounds1 rangeY = serie.GetMinMaxYAt(rangeX);
                minMaxY.Min = MathF.Min(rangeY.Min, minMaxY.Min);
                minMaxY.Max = MathF.Max(rangeY.Max, minMaxY.Max);
            }

            return new Rect2(minMaxX.Min, minMaxY.Min, minMaxX.Length, minMaxY.Length);
        }


        protected internal void ComputeMetrics(bool advance = false, bool animate = false)
        {
            if (_chartArea.Width == 0 || _chartArea.Height == 0)
                return;

            Rect2 curViewRect = new Rect2(MinX, MinY, _chartArea.Width / PixelPerUnitX, _chartArea.Height / PixelPerUnitY);

            IEnumerable<IPlotterSerie> series = Series.Where(a => a.IsVisible);

            if (!series.Any())
                return;

            Bounds1 minMax = new();

            if (AutoScaleX != AutoScaleXMode.None)
            {
                minMax.Min = float.PositiveInfinity;
                minMax.Max = float.NegativeInfinity;

                foreach (IPlotterSerie? serie in series)
                {
                    Bounds1 range = serie.GetMinMaxX();
                    if (float.IsNaN(range.Min))
                        continue;
                    minMax.Min = MathF.Min(range.Min, minMax.Min);
                    minMax.Max = MathF.Max(range.Max, minMax.Max);
                }

                if (minMax.Min != float.PositiveInfinity)
                {
                    if (AutoScaleX == AutoScaleXMode.Fit)
                    {
                        curViewRect.X = minMax.Min;
                        curViewRect.Width = minMax.Max - minMax.Min;
                    }
                    else if (AutoScaleX == AutoScaleXMode.Advance && advance)
                    {
                        float maxX = minMax.Max;

                        if (ReferencesX.Any())
                            maxX = MathF.Max(maxX, ReferencesX.Max(a => a.Value));

                        curViewRect.X = maxX - curViewRect.Width;
                        CursorX = curViewRect.Right;
                    }
                }
            }

            if (AutoScaleY != AutoScaleYMode.None)
            {
                minMax.Min = float.PositiveInfinity;
                minMax.Max = float.NegativeInfinity;

                Bounds1 window;

                foreach (IPlotterSerie? serie in series)
                {
                    if (AutoScaleY == AutoScaleYMode.Window)
                        window = new Bounds1() { Min = curViewRect.X, Max = curViewRect.Right };
                    else
                        window = serie.GetMinMaxX();

                    Bounds1 range = serie.GetMinMaxYAt(window);

                    if (float.IsNaN(range.Max))
                        continue;

                    minMax.Min = MathF.Min(range.Min, minMax.Min);
                    minMax.Max = MathF.Max(range.Max, minMax.Max);
                }

                float height = minMax.Max - minMax.Min;

                if (!float.IsInfinity(height))
                {
                    if (minMax.Max == minMax.Min)
                    {
                        float epslon = 0.1f;
                        curViewRect.Y = minMax.Min - epslon;
                        curViewRect.Height = epslon * 2;
                    }
                    else
                    {
                        curViewRect.Y = minMax.Min;
                        curViewRect.Height = (minMax.Max - minMax.Min);
                    }
                }
            }

            if (curViewRect.Width == 0 || curViewRect.Height == 0)
                return;

            if (ViewRect.Equals(curViewRect))
                return;

            if (animate)
            {
                _viewAnimation.Start(ViewRect, curViewRect, 200);
            }
            else
            {
                _viewAnimation.Stop();
                ViewRect = curViewRect;
                UpdatePlotValues(curViewRect);
            }
        }

        protected override void OnPointerMove(UiPointerEvent ev)
        {
            Vector2 pos = ev.Position(this);

            CursorX = PixelToValueX(ev.WindowPosition.X);

            base.OnPointerMove(ev);
        }

        protected internal void OnViewChanged()
        {
            ViewChanged?.Invoke(this, EventArgs.Empty);
        }


        public event EventHandler? ViewChanged;


        [UiProperty(0f, UiPropertyFlags.Render)]
        public float MinX
        {
            get => GetValue<float>(nameof(MinX))!;
            set => SetValue(nameof(MinX), value);
        }

        [UiProperty(0f, UiPropertyFlags.Render)]
        public float MinY
        {
            get => GetValue<float>(nameof(MinY))!;
            set => SetValue(nameof(MinY), value);
        }

        [UiProperty(1f, UiPropertyFlags.Render)]
        public float PixelPerUnitY
        {
            get => GetValue<float>(nameof(PixelPerUnitY))!;
            set => SetValue(nameof(PixelPerUnitY), value);
        }

        [UiProperty(1f, UiPropertyFlags.Render)]
        public float PixelPerUnitX
        {
            get => GetValue<float>(nameof(PixelPerUnitX))!;
            set => SetValue(nameof(PixelPerUnitX), value);
        }


        [UiProperty(AutoScaleYMode.None, UiPropertyFlags.Render)]
        public AutoScaleYMode AutoScaleY
        {
            get => GetValue<AutoScaleYMode>(nameof(AutoScaleY))!;
            set => SetValue(nameof(AutoScaleY), value);
        }


        [UiProperty(AutoScaleXMode.None, UiPropertyFlags.Render)]
        public AutoScaleXMode AutoScaleX
        {
            get => GetValue<AutoScaleXMode>(nameof(AutoScaleX))!;
            set => SetValue(nameof(AutoScaleX), value);
        }


        [UiProperty]
        public ObservableCollection<IPlotterSerie> Series
        {
            get => GetValue<ObservableCollection<IPlotterSerie>>(nameof(Series))!;
            set => SetValue(nameof(Series), value);
        }

        [UiProperty]
        public ObservableCollection<PlotterReference> ReferencesX
        {
            get => GetValue<ObservableCollection<PlotterReference>>(nameof(ReferencesX))!;
            set => SetValue(nameof(ReferencesX), value);
        }

        [UiProperty]
        public ObservableCollection<PlotterReference> ReferencesY
        {
            get => GetValue<ObservableCollection<PlotterReference>>(nameof(ReferencesY))!;
            set => SetValue(nameof(ReferencesY), value);
        }


        [UiProperty]
        public Rect2 ViewRect
        {
            get => GetValue<Rect2>(nameof(ViewRect));
            set => SetValue(nameof(ViewRect), value);
        }

        [UiProperty(0f, UiPropertyFlags.Render)]
        public float CursorX
        {
            get => GetValue<float>(nameof(CursorX));
            set => SetValue(nameof(CursorX), value);
        }


        [UiProperty(false, UiPropertyFlags.Render)]
        public bool ShowLegend
        {
            get => GetValue<bool>(nameof(ShowLegend));
            set => SetValue(nameof(ShowLegend), value);
        }

        [UiProperty(false, UiPropertyFlags.Render)]
        public bool ShowGridX
        {
            get => GetValue<bool>(nameof(ShowGridX));
            set => SetValue(nameof(ShowGridX), value);
        }

        [UiProperty(false, UiPropertyFlags.Render)]
        public bool ShowAxisX
        {
            get => GetValue<bool>(nameof(ShowAxisX));
            set => SetValue(nameof(ShowAxisX), value);
        }


        [UiProperty(0f, UiPropertyFlags.Render)]
        public float TickIntervalX
        {
            get => GetValue<float>(nameof(TickIntervalX));
            set => SetValue(nameof(TickIntervalX), value);
        }

        [UiProperty(false, UiPropertyFlags.Render)]
        public bool ShowAxisY
        {
            get => GetValue<bool>(nameof(ShowAxisY));
            set => SetValue(nameof(ShowAxisY), value);
        }

        [UiProperty(false, UiPropertyFlags.Render)]
        public bool ShowGridY
        {
            get => GetValue<bool>(nameof(ShowGridY));
            set => SetValue(nameof(ShowGridY), value);
        }

        [UiProperty(0f, UiPropertyFlags.Render)]
        public float TickIntervalY
        {
            get => GetValue<float>(nameof(TickIntervalY));
            set => SetValue(nameof(TickIntervalY), value);
        }

        [UiProperty(100f, UiPropertyFlags.Render)]
        public float LabelWidthY
        {
            get => GetValue<float>(nameof(LabelWidthY));
            set => SetValue(nameof(LabelWidthY), value);
        }

        [UiProperty(100f, UiPropertyFlags.Render)]
        public float LegendWidth
        {
            get => GetValue<float>(nameof(LegendWidth));
            set => SetValue(nameof(LegendWidth), value);
        }

        public Func<float, string> FormatValueX { get; set; }
    }
}