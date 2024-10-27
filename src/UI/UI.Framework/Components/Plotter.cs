using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Numerics;

using XrMath;

namespace CanvasUI.Components
{
    public struct MinMax
    {
        public float Min;
        public float Max;       
    }

    #region ENUMS

    public enum SerieSampleMode
    {
        Nearest,
        Linear
    }

    public enum AutoScaleMode
    {
        None,
        Window,
        Serie
    }


    #endregion

    #region IPlotterSerie

    public interface IPlotterSerie
    {
        float ValueAt(float x);

        MinMax GetMinMaxX();

        MinMax GetMinMaxYAt(MinMax xRange);

        void Attach(Plotter plotter);

        void Detach(Plotter plotter);

        void NotifyChanged();

        string? Name { get; }

        Color Color { get;  }

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
        
        public abstract MinMax GetMinMaxX();
        
        public abstract MinMax GetMinMaxYAt(MinMax xRange);


        public Func<float, string> FormatValue { get; set; }

        public string? Name { get; set; }

        public Color Color { get; set; }

        public bool IsVisible { get; set; }

    }

    #endregion

    #region FunctionPlotterSerie

    public class FunctionPlotterSerie : BasePlotterSerie
    {
        private Func<float, float> _func;
        MinMax _xRange;
        MinMax _yRange; 

        public FunctionPlotterSerie(Func<float, float> func, MinMax xRange, MinMax yRange)
        {
            _func = func;
            _xRange = xRange;
            _yRange = yRange;   
        }

        public override MinMax GetMinMaxX()
        {
            return _xRange;
        }

        public override MinMax GetMinMaxYAt(MinMax xRange)
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
        public DiscretePlotterSerie()
        {
            Points = [];
            SampleMode = SerieSampleMode.Nearest;
        }

        public void AppendValue(float x, float y)
        {
            Points.Add(new Vector2(x, y));
            NotifyChanged();    
        }

        public void Clear()
        {
            Points.Clear();
            NotifyChanged();
        }       

        public override float ValueAt(float x)
        {
            switch (SampleMode)
            {
                case SerieSampleMode.Nearest:
                    var index = IndexOfClosestX(x); 
                    if (index == -1)
                        return float.NaN;   
                    return Points[index].Y; 
                case SerieSampleMode.Linear:
                    return InterpolateY(x);
                default:
                    throw new NotImplementedException();
            }
        }

        public float InterpolateY(float targetX)
        {
            var index = IndexOfClosestX(targetX);
            if (index == -1)
                return float.NaN;
            if (index== Points.Count -1)
                return Points[index].Y; 

            var p1 = Points[index];
            var p2 = Points[index + 1];

            var t = (targetX - p1.X) / (p2.X - p1.X);   
            return p1.Y + (p2.Y - p1.Y) * t;    
        }


        public int IndexOfClosestX(float targetX)
        {
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
            return right;
        }

        public override MinMax GetMinMaxX()
        {
            if (Points.Count == 0)
            {
                return new MinMax
                {
                    Max = float.NaN,
                    Min = float.NaN
                };
            }
            return new MinMax
            {
                Min = Points[0].X,
                Max = Points[Points.Count - 1].X
            };
        }

        public override MinMax GetMinMaxYAt(MinMax xRange)
        {
            var result = new MinMax
            {
                Min = float.MaxValue,
                Max = float.MinValue
            };  

            var index = IndexOfClosestX(xRange.Min);
            if (index == -1)
                return result;

            while (true)
            {
                var point = Points[index];
                if (point.X > xRange.Max)
                    break;
                result.Min = Math.Min(result.Min, point.Y);
                result.Max = Math.Max(result.Max, point.Y);
                index++;
            }

            return result;
        }


        public SerieSampleMode SampleMode { get; set; }


        public IList<Vector2> Points { get; set; }

    }

    #endregion


    public class Plotter : UiElement
    {
        public Plotter()
        {

        }

        protected override void OnPropertyChanged(string propName, object? value, object? oldValue)
        {
            if (propName == nameof(Series))
            {
                if (oldValue is ObservableCollection<DiscretePlotterSerie> oldSeries)
                    oldSeries.CollectionChanged -= OnSeriesChanged;

                if (value is ObservableCollection<DiscretePlotterSerie> newSeries)
                    newSeries.CollectionChanged += OnSeriesChanged;
            }   
            base.OnPropertyChanged(propName, value, oldValue);
        }

        private void OnSeriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<IPlotterSerie>())
                    item.Attach(this);
            }
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<IPlotterSerie>())
                    item.Detach(this);
            }
        }




        protected override void DrawWork(SKCanvas canvas)
        {
            throw new NotImplementedException();
        }

        protected internal void NotifyChanged(IPlotterSerie serie)
        {
            IsDirty = true; 
        }

        protected void ComputeMetrics()
        {

        }

        [UiProperty(0f)]
        public float MinX
        {
            get => GetValue<float>(nameof(MinX))!;
            set => SetValue(nameof(MinX), value);
        }

        [UiProperty(0f)]
        public float MinY
        {
            get => GetValue<float>(nameof(MinY))!;
            set => SetValue(nameof(MinY), value);
        }

        [UiProperty(1f)]
        public float PixelPerUnitY
        {
            get => GetValue<float>(nameof(PixelPerUnitY))!;
            set => SetValue(nameof(PixelPerUnitY), value);
        }

        [UiProperty(1f)]
        public float PixelPerUnitX
        {
            get => GetValue<float>(nameof(PixelPerUnitX))!;
            set => SetValue(nameof(PixelPerUnitX), value);
        }


        [UiProperty(AutoScaleMode.None)]
        public AutoScaleMode AutoScaleY
        {
            get => GetValue<AutoScaleMode>(nameof(AutoScaleY))!;
            set => SetValue(nameof(AutoScaleY), value);
        }

        public ObservableCollection<IPlotterSerie> Series
        {
            get => GetValue<ObservableCollection<IPlotterSerie>>(nameof(Series))!;
            set => SetValue(nameof(Series), value);
        }
    }
}