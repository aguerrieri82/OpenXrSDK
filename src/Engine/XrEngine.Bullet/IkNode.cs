using System.Numerics;
using static XrEngine.Bullet.BulletLib;

namespace XrEngine.Bullet
{
    public class IkNode
    {
        public Vector3 Attach;

        public Vector3 Axis;

        public float Size;

        public Purpose Purpose;

        public float MinTheta;

        public float MaxTheta;

        public float RestAngle;

        public float Theta;

        public IkNode? Left;

        public IkNode? Right;

        public IkNode? Parent;

        public string? Name;

        public Vector3 RelPos => Parent == null ? Attach : (Attach - Parent.Attach);

        public Matrix4x4 GetLocalTransform()
        {
            var axis = Axis;

            if (axis.LengthSquared() > 0f)
                axis = Vector3.Normalize(axis);
            else
                axis = Vector3.Zero;

            var theta = Theta;

            var rot = axis == Vector3.Zero
                ? Quaternion.Identity
                : Quaternion.CreateFromAxisAngle(axis, theta);

            return Matrix4x4.CreateFromQuaternion(rot) *
                   Matrix4x4.CreateTranslation(RelPos);
        }
    }
}
