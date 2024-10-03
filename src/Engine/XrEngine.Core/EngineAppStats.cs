namespace XrEngine
{
    public class EngineAppStats
    {
        protected int _fpsFrameCount;
        protected int _frameCount;
        protected DateTime _fpsLastTime;
        protected Dictionary<Type, double> _updateTimes = [];

        public void BeginFrame()
        {

        }

        public void EndFrame()
        {
            _fpsFrameCount++;
            _frameCount++;

            var deltaSecs = (DateTime.Now - _fpsLastTime).TotalSeconds;

            if (deltaSecs >= 2)
            {
                Fps = (int)(_fpsFrameCount / deltaSecs);
                _fpsFrameCount = 0;
                _fpsLastTime = DateTime.Now;
            }
        }

        public void Update(IRenderUpdate renderUpdate, Action action)
        {
            var start = DateTime.Now;

            action();

            var total = (DateTime.Now - start).TotalMilliseconds;

            var type = renderUpdate.GetType();

            if (!_updateTimes.TryGetValue(type, out var time))
                _updateTimes[type] = total;
            else
                _updateTimes[type] += total;
        }


        public int Fps { get; protected set; }
    }
}
