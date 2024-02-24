namespace Xr.Engine
{
    public class RenderContext
    {
        public TimeSpan StartTime { get; internal set; }

        public long Frame { get; internal set; }

        public double Time { get; internal set; }
    }
}
