using System.Numerics;
using System.Security.Cryptography.X509Certificates;

namespace Xr.Engine
{
    public struct Canvas3DState
    {
        public Matrix4x4 Transform;

        public Color Color;

        public float LineWidth;
    }

    public class Canvas3D
    {
        LineMesh _lineMesh = new();
        List<LineData> _data = [];
        Stack<Canvas3DState> _states = [];
        Canvas3DState _curState;

        public Canvas3D()
        {
            _curState.LineWidth = 1;
            _curState.Transform = Matrix4x4.Identity;
            _curState.Color = Color.White;
        }

        public void DrawLine(Vector3 from, Vector3 to)
        {
            _data.Add(new LineData
            {
                Pos = from.Transform(_curState.Transform),
                Color = (Vector3)_curState.Color,
                Size = _curState.LineWidth
            });
            _data.Add(new LineData
            {
                Pos = to.Transform(_curState.Transform),
                Color = (Vector3)_curState.Color,
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
            _lineMesh.Vertices = _data.ToArray();
            _lineMesh.Version++;
        }

        public ref Canvas3DState State => ref _curState;

        public Object3D Content => _lineMesh;
    }
}
