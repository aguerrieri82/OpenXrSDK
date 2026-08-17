using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace XrEngine
{
    public struct JointDof
    {
        public bool Enabled;
        public float Min;
        public float Max;
        public float Rest;
    }

    public class Joint3D : Group3D, IDrawGizmos, ISelectionHandler
    {
        private bool _isSelected;

        public Joint3D()
        {
            EnableGizmos = Context.Require<IPlatform>().Name == "Editor";
        }

        public void DrawGizmos(Canvas3D canvas, RenderContext ctx)
        {
            if (!IsVisible)
                return;

            if (Parent is not Joint3D parent)
                return;

            var start = parent.WorldPosition;
            var end = WorldPosition;

            var axis = end - start;
            var length = axis.Length();

            if (length < 0.000001f)
                return;

            axis /= length;

            var up = MathF.Abs(Vector3.Dot(axis, Vector3.UnitY)) < 0.99f
                ? Vector3.UnitY
                : Vector3.UnitX;

            var side = Vector3.Normalize(Vector3.Cross(axis, up));
            up = Vector3.Normalize(Vector3.Cross(side, axis));

            var baseCenter = start + axis * length * 0.2f;
            var radius = length * 0.1f;

            var p0 = baseCenter + side * radius + up * radius;
            var p1 = baseCenter - side * radius + up * radius;
            var p2 = baseCenter - side * radius - up * radius;
            var p3 = baseCenter + side * radius - up * radius;

            canvas.Save();

            canvas.State.Color = _isSelected? "#ffff00" : "#ffffff";

            canvas.DrawLine(start, p0);
            canvas.DrawLine(start, p1);
            canvas.DrawLine(start, p2);
            canvas.DrawLine(start, p3);

            canvas.DrawLine(p0, p1);
            canvas.DrawLine(p1, p2);
            canvas.DrawLine(p2, p3);
            canvas.DrawLine(p3, p0);

            canvas.DrawLine(p0, end);
            canvas.DrawLine(p1, end);
            canvas.DrawLine(p2, end);
            canvas.DrawLine(p3, end);

            canvas.Restore();
        }

        void ISelectionHandler.OnSelected(Object3D obj, bool isSelected)
        {
            _isSelected = isSelected;
        }

        public Matrix4x4 InverseBindMatrix { get; set; }

        bool IDrawGizmos.IsEnabled => EnableGizmos;

        public bool EnableGizmos { get; set; }

        public JointDof DofX { get; set; }
        
        public JointDof DofY { get; set; }
        
        public JointDof DofZ { get; set; }

        public bool IsEffector { get; set; }
    }
}
