namespace XrEngine
{
    public class RenderContext
    {
        public TimeSpan StartTime { get; internal set; }

        public long Frame { get; internal set; }

        public double Time { get; internal set; }

        public double DeltaTime { get; internal set; }

        public Scene3D? Scene { get; internal set; }
    }
}
