using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine
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
            Horizontal = horizontal;
            Vertical = vertical;
            Build();
        }
        
        public void Build()
        { 
            int vertexCount = (Horizontal + 1) * (Vertical + 1);
            int indexCount = Horizontal * Vertical * 6;
            float latRads = MathF.PI * 0.5f;
            float h = Height * 0.5f;

            var verts = new VertexData[vertexCount * 3];
            var indices = new int[indexCount * 3];

            int vertexIndexOffset = 0;
            int triangleIndexOffset = 0;

            int index = 0;

            for (int y = 0; y <= Vertical; ++y)
            {
                float yf = (float)y / (float)Vertical;
                for (int x = 0; x <= Horizontal; ++x)
                {
                    float xf = (float)x / (float)Horizontal;
                    index = y * (Horizontal + 1) + x + vertexIndexOffset;
                    verts[index].Pos.X = MathF.Cos(MathF.PI * 2 * xf) * Radius;
                    verts[index].Pos.Y = MathF.Sin(MathF.PI * 2 * xf) * Radius;
                    verts[index].Pos.Z = -h + yf * 2 * h;
                    verts[index].Normal =
                        new Vector3(verts[index].Pos.X, verts[index].Pos.Y, 0).Normalize();
                }
            }


            index = triangleIndexOffset;
            for (int y = 0; y < Vertical; y++)
            {
                for (int x = 0; x < Horizontal; x++)
                {
                    indices[index + 0] = y * (Horizontal + 1) + x;
                    indices[index + 1] = y * (Horizontal + 1) + x + 1;
                    indices[index + 2] = (y + 1) * (Horizontal + 1) + x;
                    indices[index + 3] = (y + 1) * (Horizontal + 1) + x;
                    indices[index + 4] = y * (Horizontal + 1) + x + 1;
                    indices[index + 5] = (y + 1) * (Horizontal + 1) + x + 1;
                    index += 6;
                }
            }

            vertexIndexOffset += vertexCount;
            triangleIndexOffset += indexCount;

            for (int y = 0; y <= Vertical; y++)
            {
                float yf = (float)y / (float)Vertical;
                float lat = MathF.PI - yf * latRads - 0.5f * MathF.PI;
                float cosLat = MathF.Cos(lat);
                for (int x = 0; x <= Horizontal; x++)
                {
                    float xf = (float)x / (float)Horizontal;
                    float lon = (xf) * MathF.PI * 2;
                    index = y * (Horizontal + 1) + x + vertexIndexOffset;
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
            for (int x = 0; x < Horizontal; x++)
            {
                for (int y = 0; y < Vertical; y++)
                {
                    indices[index + 0] = vertexIndexOffset + y * (Horizontal + 1) + x;
                    indices[index + 2] = vertexIndexOffset + y * (Horizontal + 1) + x + 1;
                    indices[index + 1] = vertexIndexOffset + (y + 1) * (Horizontal + 1) + x;
                    indices[index + 3] = vertexIndexOffset + (y + 1) * (Horizontal + 1) + x;
                    indices[index + 5] = vertexIndexOffset + y * (Horizontal + 1) + x + 1;
                    indices[index + 4] = vertexIndexOffset + (y + 1) * (Horizontal + 1) + x + 1;
                    index += 6;
                }
            }

            vertexIndexOffset += vertexCount;
            triangleIndexOffset += indexCount;


            for (int y = 0; y <= Vertical; y++)
            {
                float yf = (float)y / (float)Vertical;
                float lat = MathF.PI - yf * latRads - 0.5f * MathF.PI;
                float cosLat = MathF.Cos(lat);
                for (int x = 0; x <= Horizontal; x++)
                {
                    float xf = (float)x / (float)Horizontal;
                    float lon = xf  * MathF.PI * 2;
                    index = y * (Horizontal + 1) + x + vertexIndexOffset;
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
            for (int x = 0; x < Horizontal; x++)
            {
                for (int y = 0; y < Vertical; y++)
                {
                    indices[index + 0] = vertexIndexOffset + y * (Horizontal + 1) + x;
                    indices[index + 1] = vertexIndexOffset + y * (Horizontal + 1) + x + 1;
                    indices[index + 2] = vertexIndexOffset + (y + 1) * (Horizontal + 1) + x;
                    indices[index + 3] = vertexIndexOffset + (y + 1) * (Horizontal + 1) + x;
                    indices[index + 4] = vertexIndexOffset + y * (Horizontal + 1) + x + 1;
                    indices[index + 5] = vertexIndexOffset + (y + 1) * (Horizontal + 1) + x + 1;
                    index += 6;
                }
            }

            Vertices = verts;
            Indices = indices.Select(a => (uint)a).ToArray();
            ActiveComponents |= VertexComponent.Normal;
        }

        public float Radius;
        
        public float Height;
        
        public int Horizontal;

        public int Vertical;
    }
}
