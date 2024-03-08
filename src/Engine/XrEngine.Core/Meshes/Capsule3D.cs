using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class Capsule3D : Geometry3D
    {
        public Capsule3D()
        {
        }

        public Capsule3D(float radius, float height, int horizontal = 7, int vertical = 7)
        {
            Radius = radius;
            Height = height;
            Build(horizontal, vertical);
        }

        public void Build(int horizontal, int vertical)
        {
            int vertexCount = (horizontal + 1) * (vertical + 1);
            int indexCount = horizontal * vertical * 6;
            float latRads = MathF.PI * 0.5f;
            float h = Height * 0.5f;

            var verts = new VertexData[vertexCount * 3];
            var indices = new int[indexCount * 3];

            int vertexIndexOffset = 0;
            int triangleIndexOffset = 0;

            int index = 0;

            for (int y = 0; y <= vertical; ++y)
            {
                float yf = (float)y / (float)vertical;
                for (int x = 0; x <= horizontal; ++x)
                {
                    float xf = (float)x / (float)horizontal;
                    index = y * (horizontal + 1) + x + vertexIndexOffset;
                    verts[index].Pos.X = MathF.Cos(MathF.PI * 2 * xf) * Radius;
                    verts[index].Pos.Y = MathF.Sin(MathF.PI * 2 * xf) * Radius;
                    verts[index].Pos.Z = -h + yf * 2 * h;
                    verts[index].Normal =
                        new Vector3(verts[index].Pos.X, verts[index].Pos.Y, 0).Normalize();
                }
            }


            index = triangleIndexOffset;
            for (int y = 0; y < vertical; y++)
            {
                for (int x = 0; x < horizontal; x++)
                {
                    indices[index + 0] = y * (horizontal + 1) + x;
                    indices[index + 1] = y * (horizontal + 1) + x + 1;
                    indices[index + 2] = (y + 1) * (horizontal + 1) + x;
                    indices[index + 3] = (y + 1) * (horizontal + 1) + x;
                    indices[index + 4] = y * (horizontal + 1) + x + 1;
                    indices[index + 5] = (y + 1) * (horizontal + 1) + x + 1;
                    index += 6;
                }
            }

            vertexIndexOffset += vertexCount;
            triangleIndexOffset += indexCount;

            for (int y = 0; y <= vertical; y++)
            {
                float yf = (float)y / (float)vertical;
                float lat = MathF.PI - yf * latRads - 0.5f * MathF.PI;
                float cosLat = MathF.Cos(lat);
                for (int x = 0; x <= horizontal; x++)
                {
                    float xf = (float)x / (float)horizontal;
                    float lon = (xf) * MathF.PI * 2;
                    index = y * (horizontal + 1) + x + vertexIndexOffset;
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
            for (int x = 0; x < horizontal; x++)
            {
                for (int y = 0; y < vertical; y++)
                {
                    indices[index + 0] = vertexIndexOffset + y * (horizontal + 1) + x;
                    indices[index + 2] = vertexIndexOffset + y * (horizontal + 1) + x + 1;
                    indices[index + 1] = vertexIndexOffset + (y + 1) * (horizontal + 1) + x;
                    indices[index + 3] = vertexIndexOffset + (y + 1) * (horizontal + 1) + x;
                    indices[index + 5] = vertexIndexOffset + y * (horizontal + 1) + x + 1;
                    indices[index + 4] = vertexIndexOffset + (y + 1) * (horizontal + 1) + x + 1;
                    index += 6;
                }
            }

            vertexIndexOffset += vertexCount;
            triangleIndexOffset += indexCount;


            for (int y = 0; y <= vertical; y++)
            {
                float yf = (float)y / (float)vertical;
                float lat = MathF.PI - yf * latRads - 0.5f * MathF.PI;
                float cosLat = MathF.Cos(lat);
                for (int x = 0; x <= horizontal; x++)
                {
                    float xf = (float)x / (float)horizontal;
                    float lon = xf * MathF.PI * 2;
                    index = y * (horizontal + 1) + x + vertexIndexOffset;
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
            for (int x = 0; x < horizontal; x++)
            {
                for (int y = 0; y < vertical; y++)
                {
                    indices[index + 0] = vertexIndexOffset + y * (horizontal + 1) + x;
                    indices[index + 1] = vertexIndexOffset + y * (horizontal + 1) + x + 1;
                    indices[index + 2] = vertexIndexOffset + (y + 1) * (horizontal + 1) + x;
                    indices[index + 3] = vertexIndexOffset + (y + 1) * (horizontal + 1) + x;
                    indices[index + 4] = vertexIndexOffset + y * (horizontal + 1) + x + 1;
                    indices[index + 5] = vertexIndexOffset + (y + 1) * (horizontal + 1) + x + 1;
                    index += 6;
                }
            }

            Vertices = verts;
            Indices = indices.Select(a => (uint)a).ToArray();
            ActiveComponents |= VertexComponent.Normal;
        }

        public float Radius;

        public float Height;
    }
}
