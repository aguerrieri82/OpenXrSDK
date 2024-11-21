using CanvasUI.Components;
using System.Diagnostics.CodeAnalysis;
using XrEngine;
using XrMath;

namespace XrSamples
{
    public class PlotterTimeLogger : ITimeLogger
    {
        static readonly string[] PALETTE = [
          "#FF5722", // Deep Orange
              "#FF9800", // Orange
              "#FFC107", // Amber
              "#FFEB3B", // Yellow
              "#CDDC39", // Lime
              "#8BC34A", // Light Green
              "#4CAF50", // Green
              "#009688", // Teal
              "#00BCD4", // Cyan
              "#03A9F4", // Light Blue
              "#2196F3", // Blue
              "#3F51B5", // Indigo
              "#673AB7", // Deep Purple
              "#9C27B0", // Purple
              "#E91E63", // Pink
              "#F44336"  // Red
        ];



        readonly Plotter _plotter;

        private DateTime _lastValueTime;
        private DateTime _lastNotifyTime;
        private readonly Timer _notifyTimer;
        private IDispatcher? _dispatcher;

        public PlotterTimeLogger(Plotter plotter)
        {
            IsEnabled = true;

            _plotter = plotter;

            _notifyTimer = new Timer(state => OnNotify(), null, Timeout.Infinite, Timeout.Infinite);

            _plotter.AutoScaleX = AutoScaleXMode.Advance;
            _plotter.AutoScaleY = AutoScaleYMode.Window;
            _plotter.PixelPerUnitX = 1;
            _plotter.ShowLegend = true;
            _plotter.ShowAxisX = true;
            _plotter.ShowGridX = true;
            _plotter.TickIntervalX = 60;
            _plotter.ShowAxisY = true;
            _plotter.TickIntervalY = 0;

            RetainTimeMs = 500;
        }

        protected void OnNotify()
        {
            if ((_lastValueTime - _lastNotifyTime).TotalMilliseconds > RetainTimeMs)
            {
                _lastNotifyTime = DateTime.UtcNow;
                if (EnsureDispatcher())
                    _dispatcher.ExecuteAsync(() => _plotter.NotifyChanged(null));
            }
        }

        static uint HashString(string str)
        {
            uint hash = 5381;
            foreach (var c in str)
                hash = ((hash << 5) + hash) + c;
            return hash;
        }


        public void Checkpoint(string name, Color color)
        {
            if (!IsEnabled)
                return;

            if (!EnsureDispatcher())
                return;

            _dispatcher.ExecuteAsync(() =>
            {
                _plotter.ReferencesX.Add(new PlotterReference()
                {
                    Value = EngineApp.Current!.Stats.Frame,
                    Name = name,
                    Color = color
                });
            });
        }

        public void LogValue<T>(string name, T value)
        {
            if (!IsEnabled)
                return;

            if (value is float fValue)
                LogValue(name, fValue);
        }


        public void LogValue(string name, float value)
        {
            if (!IsEnabled)
                return;

            var serie = (DiscretePlotterSerie?)_plotter.Series.FirstOrDefault(a => a.Name == name);

            if (serie == null)
            {
                var index = HashString(name) % PALETTE.Length;

                serie = new DiscretePlotterSerie()
                {
                    Name = name,
                    Color = PALETTE[index],
                    MaxGapX = 10,
                    SampleMode = SerieSampleMode.Nearest
                };

                if (EnsureDispatcher())
                    _dispatcher.ExecuteAsync(() => _plotter.Series.Add(serie));
            }

            _lastValueTime = DateTime.UtcNow;

            var isNotify = (_lastValueTime - _lastNotifyTime).TotalMilliseconds > RetainTimeMs;

            serie.AppendValue(EngineApp.Current!.Stats.Frame, value, isNotify);

            if (!isNotify)
                _notifyTimer.Change(RetainTimeMs, Timeout.Infinite);
            else
            {
                _lastNotifyTime = _lastValueTime;
                _notifyTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void Clear()
        {
            _plotter.Series.Clear();
            _plotter.ReferencesX.Clear();
        }

        [MemberNotNullWhen(true, nameof(_dispatcher))]
        protected bool EnsureDispatcher()
        {
            _dispatcher = EngineApp.Current?.Renderer?.Dispatcher;
            return _dispatcher != null;
        }


        public int RetainTimeMs { get; set; }

        public bool IsEnabled { get; set; }
    }
}
