namespace OpenXr.Engine
{
    public class RenderContext
    {
        public TimeSpan StartTime { get; internal set; }

        public long Frame { get; internal set; }

        public double Time { get; internal set; }
        public int Fps { get; internal set; }
    }
}
