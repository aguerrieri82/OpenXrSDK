namespace XrEngine.OpenXr
{
    public class EquirectSphere : TriangleMesh, IGeneratedContent
    {
        public EquirectSphere(float radius = 3, float section = 1)
        {
            Geometry = new Sphere3D();

            Radius = radius;
            Section = section;
            Subs = 64;

            Build();
        }

        [Action]
        public void Build()
        {
            var geo = (Sphere3D)_geometry!;

            geo.Radius = Radius;
            geo.Section = Section;
            geo.Subs = Subs;
            geo.Build();

            UpdateBounds();
        }

        public float Radius { get; set; }

        [Range(0, 1, 0.01f)]
        public float Section { get; set; }

        public uint Subs { get; set; }
    }
}