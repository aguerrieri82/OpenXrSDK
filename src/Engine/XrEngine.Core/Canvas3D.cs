using System.Numerics;
using XrMath;

namespace XrEngine
{
    public struct Canvas3DState
    {
        public Matrix4x4 Transform;

        public Color Color;

        public float LineWidth;
    }

    public class Canvas3D
    {
        readonly LineMesh _lineMesh = new();
        readonly List<PointData> _data = [];
        readonly Stack<Canvas3DState> _states = [];
        Canvas3DState _curState;

        public Canvas3D()
        {
            _curState.LineWidth = 1;
            _curState.Transform = Matrix4x4.Identity;
            _curState.Color = Color.White;
        }

        public void DrawLine(Vector3 from, Vector3 to)
        {
            _data.Add(new PointData
            {
                Pos = from.Transform(_curState.Transform),
                Color = _curState.Color,
                Size = _curState.LineWidth
            });
            _data.Add(new PointData
            {
                Pos = to.Transform(_curState.Transform),
                Color = _curState.Color,
                Size = _curState.LineWidth
            });
        }

        public void DrawBounds(Bounds3 bounds)
        {

            DrawLine(new Vector3(bounds.Min.X, bounds.Min.Y, bounds.Min.Z),
                    new Vector3(bounds.Min.X, bounds.Max.Y, bounds.Min.Z));
            DrawLine(new Vector3(bounds.Min.X, bounds.Max.Y, bounds.Min.Z),
                    new Vector3(bounds.Max.X, bounds.Max.Y, bounds.Min.Z));
            DrawLine(new Vector3(bounds.Max.X, bounds.Max.Y, bounds.Min.Z),
                    new Vector3(bounds.Max.X, bounds.Min.Y, bounds.Min.Z));
            DrawLine(new Vector3(bounds.Max.X, bounds.Min.Y, bounds.Min.Z),
                    new Vector3(bounds.Min.X, bounds.Min.Y, bounds.Min.Z));

            DrawLine(new Vector3(bounds.Min.X, bounds.Min.Y, bounds.Max.Z),
                     new Vector3(bounds.Min.X, bounds.Max.Y, bounds.Max.Z));
            DrawLine(new Vector3(bounds.Min.X, bounds.Max.Y, bounds.Max.Z),
                    new Vector3(bounds.Max.X, bounds.Max.Y, bounds.Max.Z));
            DrawLine(new Vector3(bounds.Max.X, bounds.Max.Y, bounds.Max.Z),
                    new Vector3(bounds.Max.X, bounds.Min.Y, bounds.Max.Z));
            DrawLine(new Vector3(bounds.Max.X, bounds.Min.Y, bounds.Max.Z),
                    new Vector3(bounds.Min.X, bounds.Min.Y, bounds.Max.Z));

            DrawLine(new Vector3(bounds.Min.X, bounds.Min.Y, bounds.Min.Z),
                    new Vector3(bounds.Min.X, bounds.Min.Y, bounds.Max.Z));
            DrawLine(new Vector3(bounds.Min.X, bounds.Max.Y, bounds.Min.Z),
                     new Vector3(bounds.Min.X, bounds.Max.Y, bounds.Max.Z));
            DrawLine(new Vector3(bounds.Max.X, bounds.Max.Y, bounds.Min.Z),
                     new Vector3(bounds.Max.X, bounds.Max.Y, bounds.Max.Z));
            DrawLine(new Vector3(bounds.Max.X, bounds.Min.Y, bounds.Min.Z),
                     new Vector3(bounds.Max.X, bounds.Min.Y, bounds.Max.Z));
        }

        public void DrawTriangle(Triangle3 triangle)
        {
            DrawLine(triangle.V0, triangle.V1);
            DrawLine(triangle.V1, triangle.V2);
            DrawLine(triangle.V2, triangle.V0);
        }

        public void DrawMesh(Geometry3D geometry)
        {
            foreach (var triangle in geometry.Triangles())
                DrawTriangle(triangle);
        }

        public void DrawQuad(Quad3 quad, bool drawNormal = true)
        {
            var corners = quad.Corners().ToArray();

            DrawLine(corners[0], corners[1]);
            DrawLine(corners[1], corners[2]);
            DrawLine(corners[2], corners[3]);
            DrawLine(corners[3], corners[0]);

            if (drawNormal)
            {
                var center = quad.Size / 2;
                DrawLine(quad.PointAt(center), quad.PointAt(center) + quad.Normal() * 0.5f);
                DrawLine(quad.PointAt(center), quad.PointAt(center) + quad.Tangent() * 0.5f);
            }

        }

        public void DrawPlane(Plane p, float width = 10, float height = 10, float span = 1)
        {
            DrawPlane(p, -p.Normal * p.D, width, height, span);
        }

        public void DrawPlane(Plane p, Vector3 planeOrigin, float width = 10, float height = 10, float span = 1)
        {
            Vector3 u = Vector3.Normalize(Vector3.Cross(p.Normal, new Vector3(1, 0, 0)));
            Vector3 v = Vector3.Normalize(Vector3.Cross(p.Normal, u));

            for (var x = -width / 2; x <= width / 2; x += span)
            {
                var p1 = planeOrigin + x * u + -height / 2 * v;
                var p2 = planeOrigin + x * u + height / 2 * v;
                DrawLine(p1, p2);
            }

            for (var y = -height / 2; y <= height / 2; y += span)
            {
                var p1 = planeOrigin + -width / 2 * u + y * v;
                var p2 = planeOrigin + width / 2 * u + y * v;
                DrawLine(p1, p2);
            }
        }

        public void Clear()
        {
            _data.Clear();
        }

        public void Save()
        {
            _states.Push(_curState);
        }

        public void Restore()
        {
            _curState = _states.Pop();
        }

        public void Flush()
        {
            _lineMesh.Vertices = _data.ToArray(); ;
            _lineMesh.NotifyChanged(ObjectChangeType.Geometry);
        }

        public void DrawCircle(Pose3 pose, float radius, int segments = 30)
        {
            var circlePoints = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)(i * 2 * Math.PI / segments);
                circlePoints[i] = new Vector3((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius, 0);
                circlePoints[i] = Vector3.Transform(circlePoints[i], pose.Orientation) + pose.Position;

            }

            for (int i = 0; i < segments; i++)
                DrawLine(circlePoints[i], circlePoints[(i + 1) % segments]);
        }

        public ref Canvas3DState State => ref _curState;

        public Object3D Content => _lineMesh;
    }
}
