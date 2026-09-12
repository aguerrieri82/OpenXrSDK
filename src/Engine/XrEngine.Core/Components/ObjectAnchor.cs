using System.Numerics;
using XrMath;

namespace XrEngine
{
    public enum BoxEdge
    {
        Top,
        Bottom,
        Left,
        Right,
    }

    public class ObjectAnchor : Behavior<Object3D>
    {
        protected override void Update(RenderContext ctx)
        {
            if (Target == null || Target == _host)
                return;

            if (_host is not ILocalBounds sourceBounds ||
                Target is not ILocalBounds targetBounds)
                return;

            sourceBounds.UpdateBounds();
            targetBounds.UpdateBounds();

            var sourcePivot = EdgePoint(sourceBounds.LocalBounds, SourceEdge);
            var targetPoint = EdgePoint(targetBounds.LocalBounds, TargetEdge);

            var targetWorldPoint = targetPoint.Transform(Target.WorldMatrix);

            var targetOffsetDir = Vector3.Transform(EdgeNormal(TargetEdge), Target.WorldOrientation);
            targetWorldPoint += targetOffsetDir * Offset;

            _host.Transform.SetLocalPivot(sourcePivot, true);

            _host.WorldOrientation = Target.WorldOrientation * Orientation;
            _host.WorldPosition = targetWorldPoint;
        }

        private static Vector3 EdgePoint(Bounds3 bounds, BoxEdge edge)
        {
            var center = bounds.Center;

            return edge switch
            {
                BoxEdge.Top => new Vector3(center.X, bounds.Max.Y, center.Z),
                BoxEdge.Bottom => new Vector3(center.X, bounds.Min.Y, center.Z),
                BoxEdge.Left => new Vector3(bounds.Min.X, center.Y, center.Z),
                BoxEdge.Right => new Vector3(bounds.Max.X, center.Y, center.Z),
                _ => throw new ArgumentOutOfRangeException(nameof(edge))
            };
        }

        private static Vector3 EdgeNormal(BoxEdge edge)
        {
            return edge switch
            {
                BoxEdge.Top => Vector3.UnitY,
                BoxEdge.Bottom => -Vector3.UnitY,
                BoxEdge.Left => -Vector3.UnitX,
                BoxEdge.Right => Vector3.UnitX,
                _ => throw new ArgumentOutOfRangeException(nameof(edge))
            };
        }

        public Object3D? Target { get; set; }

        public BoxEdge SourceEdge { get; set; }

        public BoxEdge TargetEdge { get; set; }

        public float Offset { get; set; }

        public Quaternion Orientation { get; set; } = Quaternion.Identity;
    }
}