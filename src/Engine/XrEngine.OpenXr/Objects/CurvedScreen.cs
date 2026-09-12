using System.Numerics;

namespace XrEngine.OpenXr
{
    public class CurvedScreen : TriangleMesh, IGeneratedContent
    {
        public CurvedScreen(float width = 2, float height = 1, float curvature = MathF.PI / 4)
        {
            Geometry = new CurvedScreen3D();

            Width = width;
            Height = height;
            Curvature = curvature;
            Subs = 64;

            Build();
        }

        [Action]
        public void Build()
        {
            var geo = (CurvedScreen3D)_geometry!;

            geo.Width = Width;
            geo.Height = Height;
            geo.Curvature = Curvature;
            geo.Subs = Subs;
            geo.Build();

            UpdateBounds();
        }

        public float Width { get; set; }

        public float Height { get; set; }

        [ValueType(ValueType.Radiant)]
        public float Curvature { get; set; }

        public uint Subs { get; set; }
    }
}