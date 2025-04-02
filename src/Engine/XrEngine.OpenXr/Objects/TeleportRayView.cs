using System.Numerics;

namespace XrEngine.OpenXr
{
    public class TeleportRayView : TriangleMesh
    {
         readonly PathMaterial _material;

        public TeleportRayView()
        {
            Segments = 20;
            Radius = 0.01f;
            _material = new PathMaterial("#0000ff")
            {
                DoubleSided = false,
                Alpha = AlphaMode.Blend,
                UseVertexColor = true
            };

            Materials.Add(_material);
            //Materials.Add(new WireframeMaterial());
            Build();
        }



        public void Update(IEnumerable<Vector3> points, bool isActive)
        {
            var oldCount = _material.Points.Length;

            _material.Points = points.ToArray();

            if (oldCount != _material.Points.Length)
                _material.NotifyChanged(ObjectChangeType.Material);

            /*
            for (var i = 0; i < _vertices.Length; i++)
            {   
                var v = _vertices![i];
                var pi = (int)v.Z;
                var p = _points[pi];
                
                Vector3 tan;

                if (pi == _points.Length - 1)
                    tan = (p - _points[pi - 1]);
                else
                    tan = (_points[pi + 1] - p);

                var quat = AlignTangent(tan.Normalize(), Vector3.UnitY);

                Geometry!.Vertices[i].Pos = (new Vector3(v.X, v.Y, 0) ).Transform(quat) + p;
            }

            Geometry.NotifyChanged(ObjectChangeType.Geometry);
            */
        }

        protected void Build()
        {
            var builder = new MeshBuilder();

            var path = new Vector2[Segments];
            for (var i = 0; i < Segments; i++)
                path[i] = new Vector2(0, i);

            var pathPoly = new Poly2D() { Points = path };

            var circle = new Circle2D()
            {
                Radius = Radius
            }.ToPoly2(10, true);

            builder.LoftPoly(circle, pathPoly, UVMode.Normalized);

            Geometry = builder.ToGeometry();

            Geometry.ActiveComponents |= VertexComponent.Tangent;

            for (var i = 0; i < Geometry.Vertices.Length; i++)
            {
                var uv = Geometry.Vertices[i].UV;
                Geometry.Vertices[i].Tangent = new Vector4(1, 1, 1, uv.Y);
            }
        }

        public float Radius { get; set; }

        public int Segments { get; set; }
    }
}
