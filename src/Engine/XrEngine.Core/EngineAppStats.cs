﻿using System.Diagnostics;

namespace XrEngine
{
    public class EngineAppStats
    {
        protected int _fpsFrameCount;
        protected int _frameCount;
        protected DateTime _fpsLastTime;
        protected Dictionary<string, double> _updateTimes = [];

        public void BeginFrame()
        {

        }

        public void EndFrame()
        {
            _fpsFrameCount++;
            _frameCount++;

            var deltaSecs = (DateTime.UtcNow - _fpsLastTime).TotalSeconds;

            if (deltaSecs >= 2)
            {
                Fps = (int)(_fpsFrameCount / deltaSecs);
                _fpsFrameCount = 0;
                _fpsLastTime = DateTime.UtcNow;
            }
        }

        public void Update(IRenderUpdate renderUpdate, Action action)
        {
            var startTime = Stopwatch.GetTimestamp();

            action();

            var total = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;

            var type = renderUpdate.GetType();

            if (!_updateTimes.TryGetValue(type.FullName!, out _))
                _updateTimes[type.FullName!] = total;
            else
                _updateTimes[type.FullName!] += total;
        }

        public long LayerChanges;

        public long Frame => _frameCount;

        public int Fps { get; internal set; }
    }
}
