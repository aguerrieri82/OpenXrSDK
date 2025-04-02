using System.Numerics;

namespace XrMath
{
    public static class MathUtils
    {
        public unsafe static Vector3[] Vector3FromArray(float[] array)
        {
            fixed (float* pData = array)
            {
                var span = new Span<Vector3>(pData, array.Length / 3);
                return span.ToArray();
            }
        }

        public static float MapRange(float value, float min, float max)
        {
            return MathF.Max(0, MathF.Min((max - min), value - min)) / (max - min);
        }

        public static Vector3 ToVector3(float[] array)
        {
            return new Vector3(array[0], array[1], array[2]);
        }

        public static unsafe Matrix4x4 CreateMatrix(float[] values)
        {
            if (values.Length != 16)
                throw new ArgumentException();
            fixed (float* data = values)
                return *(Matrix4x4*)data;
        }

        public static Quaternion QuatFromForwardUp(Vector3 forward, Vector3 up)
        {
            var lookAt = Matrix4x4.CreateLookAt(Vector3.Zero, forward, up);
            Matrix4x4.Invert(lookAt, out var rotMatrix);
            return Quaternion.CreateFromRotationMatrix(rotMatrix);
        }

        public static Quaternion QuatAlignTangent(Vector3 tangent, Vector3 up)
        {
            var right = Vector3.Normalize(Vector3.Cross(up, tangent));
            var correctedUp = Vector3.Cross(tangent, right);

            var rotationMatrix = new Matrix4x4(
                right.X, right.Y, right.Z, 0,
                correctedUp.X, correctedUp.Y, correctedUp.Z, 0,
                tangent.X, tangent.Y, tangent.Z, 0,
                0, 0, 0, 1
            );

            return Quaternion.CreateFromRotationMatrix(rotationMatrix);
        }

        public static Quad3 QuadFromEdges(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            var result = new Quad3();

            var edge1 = p2 - p1;
            var edge2 = p4 - p1;

            Vector3 normal = -Vector3.Normalize(Vector3.Cross(edge1, edge2));

            result.Pose.Position = (p1 + p2 + p3 + p4) / 4;
            result.Pose.Orientation = normal.ToOrientation();
            result.Size = new Vector2(edge1.Length(), edge2.Length());

            return result;
        }


        public static void OrthoNormalize(ref Vector3 normal, ref Vector3 tangent, float epsilon = MathExtensions.EPSILON)
        {
            // Normalize the normal vector
            normal = Vector3.Normalize(normal);

            // Project the tangent onto the normal
            var proj = normal * Vector3.Dot(tangent, normal);

            // Subtract the projection from the tangent to make it orthogonal to the normal
            tangent -= proj;

            // Normalize the tangent vector
            float tangentLength = tangent.Length();
            if (tangentLength > epsilon) // Avoid division by zero
            {
                tangent /= tangentLength;
            }
            else
            {
                // If the tangent length is zero, set it to an arbitrary orthogonal vector
                tangent = Vector3.Cross(normal, Vector3.UnitX);
                if (tangent.LengthSquared() < epsilon)
                    tangent = Vector3.Cross(normal, Vector3.UnitY);
                tangent = Vector3.Normalize(tangent);
            }
        }

        public static Matrix4x4 CreateReflectionMatrix(Plane plane)
        {
            float x = plane.Normal.X;
            float y = plane.Normal.Y;
            float z = plane.Normal.Z;
            float d = plane.D;

            return new Matrix4x4(
                1 - 2 * x * x, -2 * x * y, -2 * x * z, 0,
                -2 * y * x, 1 - 2 * y * y, -2 * y * z, 0,
                -2 * z * x, -2 * z * y, 1 - 2 * z * z, 0,
                -2 * d * x, -2 * d * y, -2 * d * z, 1
            );
        }

        public static Plane PlaneFromNormalPoint(Vector3 normal, Vector3 point)
        {
            return new Plane(normal, -Vector3.Dot(normal, point));
        }

        public static float Smooth(float prev, float current, float alpha)
        {
            return alpha * current + (1 - alpha) * prev;
        }
    }
}
