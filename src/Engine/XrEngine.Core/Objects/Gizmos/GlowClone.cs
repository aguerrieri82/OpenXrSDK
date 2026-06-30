
using System.Numerics;
using System.Text;


namespace XrEngine
{
    public class GlowClone : TriangleMesh
    {
        public GlowClone()
        {
            Geometry = Quad3D.Default;
            Materials.Add(new GlowConeMaterial() { });
        }

        public override void Update(RenderContext ctx)
        {
            var axis = Vector3.Normalize(Direction);

            var normal = -ctx.Camera!.Forward;
            normal -= axis * Vector3.Dot(normal, axis);

            if (normal.LengthSquared() < 1e-6f)
            {
                normal = ctx.Camera.Right;
                normal -= axis * Vector3.Dot(normal, axis);
            }

            normal = Vector3.Normalize(normal);

            var right = Vector3.Normalize(Vector3.Cross(axis, normal));
            var up = Vector3.Normalize(Vector3.Cross(normal, right));

            var rotMatrix = new Matrix4x4(
                right.X, right.Y, right.Z, 0f,
                up.X, up.Y, up.Z, 0f,
                normal.X, normal.Y, normal.Z, 0f,
                0f, 0f, 0f, 1f
            );

            WorldOrientation = Quaternion.CreateFromRotationMatrix(rotMatrix);

            base.Update(ctx);
        }

        public Vector3 Direction { get; set; } 
    }
}
