using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine
{
    public class Sphere3D : Geometry3D
    {
        public Sphere3D()
            : this(1f, 10, 10)
        {

        }

        public Sphere3D(float radius, int latSegments, int lonSegments)
        {
            Radius = radius;
            Build(latSegments, lonSegments);
        }
        
        public void Build(int latSegments, int lonSegments)
        {
            var vertices = new List<VertexData>();

            for (int lat = 0; lat <= latSegments; lat++)
            {
                var theta = lat * Math.PI / latSegments;
                var sinTheta = Math.Sin(theta);
                var cosTheta = Math.Cos(theta);

                for (int lon = 0; lon <= lonSegments; lon++)
                {
                    var phi = lon * 2 * Math.PI / lonSegments;
                    var sinPhi = Math.Sin(phi);
                    var cosPhi = Math.Cos(phi);

                    var x = cosPhi * sinTheta;
                    var y = cosTheta;
                    var z = sinPhi * sinTheta;
                    var u = 1 - (lon / (float)lonSegments);
                    var v = 1 - (lat / (float)latSegments);

                    var position = new Vector3((float)(Radius * x), (float)(Radius * y), (float)(Radius * z));
                    var normal = Vector3.Normalize(position);
                    var uv = new Vector2(u, v);

                    vertices.Add(new VertexData { Pos = position, Normal = normal, UV = uv });
                }
            }

            var indices = new List<uint>();

            for (uint lat = 0; lat < latSegments; lat++)
            {
                for (uint lon = 0; lon < lonSegments; lon++)
                {
                    uint first = (uint)(lat * (lonSegments + 1)) + lon;
                    uint second = (uint)(first + lonSegments + 1);

                    indices.Add(first);
                    indices.Add(second);
                    indices.Add(first + 1);

                    indices.Add(second);
                    indices.Add(second + 1);
                    indices.Add(first + 1);
                }
            }
            
            Indices = indices.ToArray();
            Vertices = vertices.ToArray();
            this.ComputeNormals();
            this.SmoothNormals();

            ActiveComponents |= VertexComponent.Normal;
        }

        public float Radius;


        public static readonly Sphere3D Instance = new Sphere3D();
    }
}
