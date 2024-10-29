using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace CanvasUI
{
    public interface IAnimation
    {
        bool IsStarted { get; set; }

        TimeSpan StartTime { get; set; }    

        TimeSpan Duration { get; set; }

        void Step(float t);
    }

    public abstract class Animation<T> : IAnimation where T : struct
    {
        public void Start(T from, T to, float durationMs)
        {
            Start(from, to, TimeSpan.FromMilliseconds(durationMs));
        }

        public void Start(T from, T to, TimeSpan duration)
        {
            if (IsStarted)
                Value = to;

            Duration = duration;
            From = from;
            To = to;
            Value = from;

            AnimationManager.Instance.Start(this);  
        }

        public void Step(float t)
        {
            Value = Interpolate(From, To, t);

            ValueChanged?.Invoke(this, Value);
        }

        protected abstract T Interpolate(T from, T to, float t);

        public bool IsStarted { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan Duration { get; set; }

        public T From { get; set; }

        public T To { get; set; }

        public T Value { get; set; }


        public event EventHandler<T>? ValueChanged;  
    }

    public class Rect2Animation : Animation<Rect2>
    {
        protected override Rect2 Interpolate(Rect2 from, Rect2 to, float t)
        {
            return new Rect2
            {
                X = from.X + (to.X - from.X) * t,
                Y = from.Y + (to.Y - from.Y) * t,
                Width = from.Width + (to.Width - from.Width) * t,
                Height = from.Height + (to.Height - from.Height) * t
            };
        }
    }


    public class AnimationManager
    {
        readonly Thread _thread;
        readonly HashSet<IAnimation> _animations;

        AnimationManager()
        {
            _thread = new Thread(Update);
            _thread.Name = "UI Animation";
            _thread.Start();
            _animations = [];
            FrameRate = 60;
        }

        public void Start(IAnimation animation)
        {
            lock (_animations)
            {
                animation.IsStarted = false;
                _animations.Add(animation);
                Monitor.Pulse(_animations);
            }
        }

        protected void Update()
        {
            var startTime = DateTime.Now;
            
            var toRemove = new HashSet<IAnimation>();   

            while (true)
            {
                lock (_animations)
                {
                    while (_animations.Count == 0)
                        Monitor.Wait(_animations);

                }

                var curTime = DateTime.Now - startTime;

                toRemove.Clear();

                foreach (var animation in _animations)
                {
                    if (!animation.IsStarted)
                    {
                        animation.StartTime = curTime;
                        animation.IsStarted = true;
                    }

                    var t = (float)((curTime - animation.StartTime).TotalMilliseconds / animation.Duration.TotalMilliseconds);

                    if (t > 1)
                        t = 1;
                    
                    animation.Step(t);

                    if (t == 1)
                    {
                        lock (_animations)
                        {
                            animation.IsStarted = false;
                            toRemove.Add(animation);
                        }
         
                    }
           
                }

                lock (_animations)
                {
                    foreach (var item in toRemove)
                        _animations.Remove(item);
                }

                if (_animations.Count > 0)
                    Thread.Sleep(TimeSpan.FromSeconds(1f / FrameRate));
            }
        }
        

        public int FrameRate { get; set; }


        public static readonly AnimationManager Instance = new AnimationManager();      
    }
}
