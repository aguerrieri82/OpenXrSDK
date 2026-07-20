using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class Grid3D : Geometry3D, IGeneratedContent
    {
        public Grid3D(Size2I size)
        {
            Size = size;
            Build();
        }

        protected override void CloneWork(Geometry3D result)
        {
            var geo = (Grid3D)result;
            geo.Size = Size;
        }

        public void Build()
        {
            var w = Size.Width;
            var h = Size.Height;

            var vertices = new VertexData[w * h];

            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var uv = new Vector2(
                        (float)x / (w - 1),
                        (float)y / (h - 1)
                    );

                    vertices[y * w + x] = new VertexData
                    {
                        Pos = new Vector3(uv.X, uv.Y, 0.0f),
                        Normal = Vector3.UnitZ,
                        UV = uv
                    };
                }
            }

            var indices = new uint[(w - 1) * (h - 1) * 6];
            var k = 0;

            for (var y = 0; y < h - 1; y++)
            {
                for (var x = 0; x < w - 1; x++)
                {
                    var i0 = (uint)(y * w + x);
                    var i1 = i0 + 1;
                    var i2 = i0 + (uint)w;
                    var i3 = i2 + 1;

                    indices[k++] = i0;
                    indices[k++] = i1;
                    indices[k++] = i2;

                    indices[k++] = i1;
                    indices[k++] = i3;
                    indices[k++] = i2;
                }
            }

            Vertices = vertices;
            Indices = indices;

            ActiveComponents = VertexComponent.Position | VertexComponent.UV0 | VertexComponent.Normal;

            NotifyChanged(ChangeType.Geometry);
        }

        public Size2I Size { get; set; }
    }
}
