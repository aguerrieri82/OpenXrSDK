using CanvasUI.Components;
using System.ComponentModel;
using XrEngine;
using XrMath;

namespace XrEditor.Plot
{
    [Panel("cf183da2-a88f-499c-bea4-b286644d4e78")]
    [DisplayName("Plot")]
    [StateManager(StateManagerMode.Explicit)]
    public class PlotPanel : BasePanel, ITimeLogger
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


        private DateTime _lastValueTime;
        private DateTime _lastNotifyTime;
        private SingleSelector _autoScaleX;
        private SingleSelector _autoScaleY;
        private readonly Timer _notifyTimer;

        public PlotPanel()
        {
            Context.Implement<ITimeLogger>(this);

            Plotter = new Plotter();

            _notifyTimer = new Timer(state => OnNotify(), null, Timeout.Infinite, Timeout.Infinite);

            Plotter.AutoScaleX = AutoScaleXMode.Advance;
            Plotter.AutoScaleY = AutoScaleYMode.Window;
            Plotter.PixelPerUnitX = 1;
            Plotter.ShowLegend = true;
            Plotter.ShowAxisX = true;
            Plotter.ShowGridX = true;
            Plotter.TickIntervalX = 60;
            Plotter.ShowAxisY = true;
            Plotter.TickIntervalY = 0;

            RetainTimeMs = 200;

            ToolBar = new ToolbarView();
            ToolBar.AddText("X:");
            _autoScaleX = ToolBar.AddEnumSelect(Plotter.AutoScaleX, a => Plotter.AutoScaleX = a);
            ToolBar.AddText("Y:");
            _autoScaleY= ToolBar.AddEnumSelect(Plotter.AutoScaleY, a => Plotter.AutoScaleY = a);
            ToolBar.AddToggle("icon_vertical_split", Plotter.ShowAxisY, a => Plotter.ShowAxisY = a);
            ToolBar.AddDivider();
            ToolBar.AddButton("icon_close", Clear);
        }

        public void Clear()
        {
            Plotter.Series.Clear();
            Plotter.ReferencesX.Clear();
        }


        static uint HashString(string str)
        {
            uint hash = 5381;
            foreach (var c in str)
                hash = ((hash << 5) + hash) + c;
            return hash;
        }

        protected void OnNotify()
        {
            if ((_lastValueTime - _lastNotifyTime).TotalMilliseconds > RetainTimeMs)
            {
                _lastNotifyTime = DateTime.UtcNow;
                _mainDispatcher.Execute(() => Plotter.NotifyChanged(null));
            }
        }

        public void LogValue<T>(string name, T value)
        {
            if (value is float fValue)
                LogValue(name, fValue);
        }

        public void LogValue(string name, float value)
        {
            var serie = (DiscretePlotterSerie?)Plotter.Series.FirstOrDefault(a => a.Name == name);

            if (serie == null)
            {
                var index = HashString(name) % PALETTE.Length;

                serie = new DiscretePlotterSerie()
                {
                    Name = name,
                    Color = PALETTE[index],
                    MaxGapX = 1,
                    SampleMode = SerieSampleMode.Nearest
                };

                _mainDispatcher.Execute(() => Plotter.Series.Add(serie));
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

        public override void SetState(IStateContainer container)
        {
            _autoScaleX.SelectedValue = container.Read<AutoScaleXMode>("AutoScaleX");
            _autoScaleY.SelectedValue = container.Read<AutoScaleYMode>("AutoScaleY");

            base.SetState(container);
        }

        public override void GetState(IStateContainer container)
        {
            container.Write("AutoScaleX", Plotter.AutoScaleX);
            container.Write("AutoScaleY", Plotter.AutoScaleY);

            base.GetState(container);
        }

        public void Checkpoint(string name, Color color)
        {
            _mainDispatcher.Execute(() =>
            {
                Plotter.ReferencesX.Add(new PlotterReference()
                {
                    Value = EngineApp.Current!.Stats.Frame,
                    Name = name,
                    Color = color
                });
            });

        }

        public Plotter Plotter { get; }

        public int RetainTimeMs { get; set; }

        public override string? Title => "Plot";
    }
}
