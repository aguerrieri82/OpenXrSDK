using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public class Arrow3D : Geometry3D, IGeneratedContent
    {
        public Arrow3D()
        {
            Subs = 15;
            BaseDiameter = 0.03f;
            ArrowDiameter = 0.075f;
            ArrowLength = 0.1f;
            BaseLength = 0.2f;
            Flags |= EngineObjectFlags.Readonly;
            Build();
        }

        public void Build()
        {
            var vertices = new List<Vector3>();
            
            void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
            {
                vertices.Add(c);
                vertices.Add(b);
                vertices.Add(a);
            }

            void AddCircle(Vector3 center, float radius, float subs)
            {
                for (var i = 0; i < Subs; i++)
                {
                    var a1 = MathF.PI * 2 * i / Subs;
                    var a2 = MathF.PI * 2 * (i + 1) / Subs;
                    var v1 = center + new Vector3(MathF.Cos(a1) * radius, MathF.Sin(a1) * radius, 0);
                    var v2 = center + new Vector3(MathF.Cos(a2) * radius, MathF.Sin(a2) * radius, 0);
                    AddTriangle(v1, v2, center);
                }
            }

            void AddCylinder(Vector3 center, float radius, float height, float subs)
            {
                for (var i = 0; i < Subs; i++)
                {
                    var a1 = MathF.PI * 2 * i / Subs;
                    var a2 = MathF.PI * 2 * (i + 1) / Subs;

                    var v1 = center + new Vector3(MathF.Cos(a1) * radius, MathF.Sin(a1) * radius, 0);
                    var v2 = center + new Vector3(MathF.Cos(a2) * radius, MathF.Sin(a2) * radius, 0);

                    var v3 = new Vector3(v1.X, v1.Y, v1.Z + height);
                    var v4 = new Vector3(v2.X, v2.Y, v2.Z + height);

                    AddTriangle(v1, v3, v2);
                    AddTriangle(v3, v4, v2);
                }
            }

            void AddCone(Vector3 center, float radius, float height, float subs)
            {
                var top = center + new Vector3(0, 0, height);       
                for (var i = 0; i < Subs; i++)
                {
                    var a1 = MathF.PI * 2 * i / Subs;
                    var a2 = MathF.PI * 2 * (i + 1) / Subs;

                    var v1 = center + new Vector3(MathF.Cos(a1) * radius, MathF.Sin(a1) * radius, 0);
                    var v2 = center + new Vector3(MathF.Cos(a2) * radius, MathF.Sin(a2) * radius, 0);
                    AddTriangle(v1, top, v2);
                }
            }

            AddCircle(Vector3.Zero, BaseDiameter / 2, Subs);
            AddCircle(new Vector3(0,0, BaseLength), ArrowDiameter / 2, Subs);
            AddCylinder(Vector3.Zero, BaseDiameter / 2, BaseLength, Subs);
            AddCone(new Vector3(0, 0, BaseLength), ArrowDiameter / 2, ArrowLength, Subs);

            Vertices = vertices.Select(a=> new VertexData
            {
                Pos = a,
            }).ToArray();

            ActiveComponents |= VertexComponent.Position;

            this.ComputeNormals();
            this.SmoothNormals(Subs * 2 * 3, (uint)Vertices.Length - 1);
        }

        public uint Subs { get; set; }

        public float BaseDiameter { get; set; }

        public float ArrowDiameter { get; set; }

        public float ArrowLength { get; set; }  

        public float BaseLength { get; set; }


    }
}
