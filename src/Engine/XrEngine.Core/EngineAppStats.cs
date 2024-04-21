namespace XrEngine
{
    public class EngineAppStats
    {
        protected int _fpsFrameCount;
        protected DateTime _fpsLastTime;

        public void BeginFrame()
        {
        }

        public void EndFrame()
        {
            _fpsFrameCount++;

            var deltaSecs = (DateTime.Now - _fpsLastTime).TotalSeconds;

            if (deltaSecs >= 2)
            {
                Fps = (int)(_fpsFrameCount / deltaSecs);
                _fpsFrameCount = 0;
                _fpsLastTime = DateTime.Now;
            }
        }

        public int Fps { get; protected set; }
    }
}
