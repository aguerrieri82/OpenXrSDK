namespace XrEngine
{
    public class RenderContext : IReferenceTime
    {
        public TimeSpan StartTime { get; set; }

        public long Frame { get; set; }

        public double Time { get; set; }

        public double DeltaTime { get; set; }

        public Scene3D? Scene { get; set; }

        public Camera? Camera { get; set; }

        public bool UpdateOnlySelf { get; set; }
    }
}
