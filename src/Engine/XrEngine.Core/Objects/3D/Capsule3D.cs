using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class Capsule3D : Geometry3D, IGeneratedContent
    {
        public Capsule3D()
            : this(0.5f, 1)
        {

        }

        public Capsule3D(float radius, float height, int horizontal = 7, int vertical = 7)
        {
            Radius = radius;
            Height = height;
            SubsH = horizontal;
            SubsV = vertical;
            Flags |= EngineObjectFlags.Readonly;
            Build();
        }


        public void Build()
        {
            int vertexCount = (SubsH + 1) * (SubsV + 1);
            int indexCount = SubsH * SubsV * 6;
            float latRads = MathF.PI * 0.5f;
            float h = Height * 0.5f;

            var verts = new VertexData[vertexCount * 3];
            var indices = new int[indexCount * 3];

            int vertexIndexOffset = 0;
            int triangleIndexOffset = 0;

            int index = 0;

            for (int y = 0; y <= SubsV; ++y)
            {
                float yf = y / (float)SubsV;
                for (int x = 0; x <= SubsH; ++x)
                {
                    float xf = x / (float)SubsH;
                    index = y * (SubsH + 1) + x + vertexIndexOffset;
                    verts[index].Pos.X = MathF.Cos(MathF.PI * 2 * xf) * Radius;
                    verts[index].Pos.Y = MathF.Sin(MathF.PI * 2 * xf) * Radius;
                    verts[index].Pos.Z = -h + yf * 2 * h;
                    verts[index].Normal =
                        new Vector3(verts[index].Pos.X, verts[index].Pos.Y, 0).Normalize();
                }
            }


            index = triangleIndexOffset;
            for (int y = 0; y < SubsV; y++)
            {
                for (int x = 0; x < SubsH; x++)
                {
                    indices[index + 0] = y * (SubsH + 1) + x;
                    indices[index + 1] = y * (SubsH + 1) + x + 1;
                    indices[index + 2] = (y + 1) * (SubsH + 1) + x;
                    indices[index + 3] = (y + 1) * (SubsH + 1) + x;
                    indices[index + 4] = y * (SubsH + 1) + x + 1;
                    indices[index + 5] = (y + 1) * (SubsH + 1) + x + 1;
                    index += 6;
                }
            }

            vertexIndexOffset += vertexCount;
            triangleIndexOffset += indexCount;

            for (int y = 0; y <= SubsV; y++)
            {
                float yf = y / (float)SubsV;
                float lat = MathF.PI - yf * latRads - 0.5f * MathF.PI;
                float cosLat = MathF.Cos(lat);
                for (int x = 0; x <= SubsH; x++)
                {
                    float xf = x / (float)SubsH;
                    float lon = (xf) * MathF.PI * 2;
                    index = y * (SubsH + 1) + x + vertexIndexOffset;
                    verts[index].Pos.X = Radius * MathF.Cos(lon) * cosLat;
                    verts[index].Pos.Y = Radius * MathF.Sin(lon) * cosLat;
                    verts[index].Pos.Z = h + (Radius * MathF.Sin(lat));
                    verts[index].Normal = new Vector3(
                                                verts[index].Pos.X,
                                                verts[index].Pos.Y,
                                                verts[index].Pos.Z - h)
                                                .Normalize();
                }
            }

            index = triangleIndexOffset;
            for (int x = 0; x < SubsH; x++)
            {
                for (int y = 0; y < SubsV; y++)
                {
                    indices[index + 0] = vertexIndexOffset + y * (SubsH + 1) + x;
                    indices[index + 2] = vertexIndexOffset + y * (SubsH + 1) + x + 1;
                    indices[index + 1] = vertexIndexOffset + (y + 1) * (SubsH + 1) + x;
                    indices[index + 3] = vertexIndexOffset + (y + 1) * (SubsH + 1) + x;
                    indices[index + 5] = vertexIndexOffset + y * (SubsH + 1) + x + 1;
                    indices[index + 4] = vertexIndexOffset + (y + 1) * (SubsH + 1) + x + 1;
                    index += 6;
                }
            }

            vertexIndexOffset += vertexCount;
            triangleIndexOffset += indexCount;


            for (int y = 0; y <= SubsV; y++)
            {
                float yf = y / (float)SubsV;
                float lat = MathF.PI - yf * latRads - 0.5f * MathF.PI;
                float cosLat = MathF.Cos(lat);
                for (int x = 0; x <= SubsH; x++)
                {
                    float xf = x / (float)SubsH;
                    float lon = xf * MathF.PI * 2;
                    index = y * (SubsH + 1) + x + vertexIndexOffset;
                    verts[index].Pos.X = Radius * MathF.Cos(lon) * cosLat;
                    verts[index].Pos.Y = Radius * MathF.Sin(lon) * cosLat;
                    verts[index].Pos.Z = -h + -(Radius * MathF.Sin(lat));
                    verts[index].Normal = new Vector3(
                                                verts[index].Pos.X,
                                                verts[index].Pos.Y,
                                                verts[index].Pos.Z + h)
                                                .Normalize();
                }
            }

            index = triangleIndexOffset;
            for (int x = 0; x < SubsH; x++)
            {
                for (int y = 0; y < SubsV; y++)
                {
                    indices[index + 0] = vertexIndexOffset + y * (SubsH + 1) + x;
                    indices[index + 1] = vertexIndexOffset + y * (SubsH + 1) + x + 1;
                    indices[index + 2] = vertexIndexOffset + (y + 1) * (SubsH + 1) + x;
                    indices[index + 3] = vertexIndexOffset + (y + 1) * (SubsH + 1) + x;
                    indices[index + 4] = vertexIndexOffset + y * (SubsH + 1) + x + 1;
                    indices[index + 5] = vertexIndexOffset + (y + 1) * (SubsH + 1) + x + 1;
                    index += 6;
                }
            }

            Vertices = verts;
            Indices = indices.Select(a => (uint)a).ToArray();
            ActiveComponents |= VertexComponent.Normal;
        }

        public int SubsH { get; set; }

        public int SubsV { get; set; }

        public float Radius { get; set; }

        public float Height { get; set; }
    }
}
