using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class PointLight : Light
    {
        Vector3 _lastWorldPos;

        public PointLight()
        {
            Specular = Color.White;
            Range = 10;
        }

        protected internal override void InvalidateWorld()
        {
            base.InvalidateWorld();

            if (!_lastWorldPos.IsSimilar(WorldPosition, 0.001f))
            {
                _lastWorldPos = WorldPosition;
                _version++;
            }
        }

        public override void DrawGizmos(Canvas3D canvas, RenderContext ctx)
        {
            canvas.State.Color = "#500000";
            canvas.DrawSphere(WorldPosition, Range, ctx.Camera!.WorldPosition, 60);
            canvas.State.Color = "#ff4000";
            canvas.DrawSphere(WorldPosition, Range * 0.7f, ctx.Camera!.WorldPosition, 60);
            canvas.State.Color = "#404000";

            canvas.DrawLine(WorldPosition - Vector3.UnitX * Range, WorldPosition + Vector3.UnitX * Range);
            canvas.DrawLine(WorldPosition - Vector3.UnitZ * Range, WorldPosition + Vector3.UnitZ * Range);
        }

        [Range(0, 100, 0.05f)]
        public float Range { get; set; }
    }
}
