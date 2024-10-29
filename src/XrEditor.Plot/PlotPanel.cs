using CanvasUI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine;

namespace XrEditor.Plot
{
    [Panel("Plotter")]
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
        private Timer _notifyTimer;

        public PlotPanel()
        {
            Context.Implement<ITimeLogger>(this);

            Plotter = new Plotter();

            _notifyTimer = new Timer(state => OnNotify(), null, Timeout.Infinite, Timeout.Infinite);


            /*
            var serie = new FunctionPlotterSerie(a => MathF.Sin(a), new MinMax
            {
                Min = 0,
                Max = MathF.PI * 2
            },
            new MinMax
            {
                Min = -1,
                Max = 1
            });

            serie.Color = "#ff0000";

            Plotter.Series.Add(serie);
            
            Plotter.PixelPerUnitY = 50;
            Plotter.PixelPerUnitX = 1;
            Plotter.MinY = -1;
            */

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
        }


        public void LogValue<T>(string name, T value)
        {
            if (value is float fValue)
                LogValue(name, fValue);
        }

        static uint HashString(string str)
        {
            uint hash = 5381;
            foreach (var c in str)
            {
                hash = ((hash << 5) + hash) + c;
            }
            return hash;
        }

        protected void OnNotify()
        {
            if ((_lastValueTime - _lastNotifyTime).TotalMilliseconds > RetainTimeMs)
            {
                _lastNotifyTime = DateTime.Now;
                _main.Execute(() => Plotter.NotifyChanged(null));
            }
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
                    MinGapX = 1,
                    SampleMode = SerieSampleMode.Nearest
                };

                _main.Execute(() => Plotter.Series.Add(serie));
            }

            _lastValueTime = DateTime.Now;

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

        public void Checkpoint(string name)
        {
            throw new NotImplementedException();
        }

        public Plotter Plotter { get;}


        public int RetainTimeMs { get; set; }

    }
}
