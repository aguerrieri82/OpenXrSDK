using System;
using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class SemiSphere3D : Geometry3D, IGeneratedContent
    {
        public SemiSphere3D()
        {
            Flags |= EngineObjectFlags.Readonly;
        }

        public SemiSphere3D(float radius, uint subs)
            : this()
        {
            Radius = radius;
            Subs = subs;
            Build();
        }

        public void Build()
        {
            var vertices = new List<VertexData>();
            var indices = new List<uint>();

            for (int i = 0; i <= Subs; i++)
            {
                for (int j = 0; j <= Subs; j++)
                {
                    float phi = MathF.PI * i / Subs;
                    float theta = 1 * MathF.PI * j / Subs;

                    float x = MathF.Sin(phi) * MathF.Cos(theta);
                    float y = MathF.Cos(phi);
                    float z = MathF.Sin(phi) * MathF.Sin(theta);

                    vertices.Add(new VertexData
                    {
                        Pos = new Vector3(x, y, z) * Radius,
                        Normal = new Vector3(x, y, z),
                        UV = new Vector2(0.25f + (float)j / Subs / 2, (float)i / Subs)

                    });
                }
            }

            for (uint i = 0; i < Subs; i++)
            {
                for (uint j = 0; j < Subs; j++)
                {
                    var topLeft = i * (Subs + 1) + j;
                    var topRight = topLeft + 1;
                    var bottomLeft = (i + 1) * (Subs + 1) + j;
                    var bottomRight = bottomLeft + 1;

                    indices.Add(topLeft);
                    indices.Add(bottomLeft);
                    indices.Add(topRight);

                    indices.Add(topRight);
                    indices.Add(bottomLeft);
                    indices.Add(bottomRight);
                }
            }

            _indices = indices.ToArray();
            _vertices = vertices.ToArray(); 

            ActiveComponents |= VertexComponent.Normal | VertexComponent.UV0;
        }

        public uint Subs { get; set; }

        public float Radius { get; set; }


        public static readonly SemiSphere3D Default = new(1f, 30);
    }
}
