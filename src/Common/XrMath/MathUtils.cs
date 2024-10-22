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


        public static Quad3 QuadFromEdges(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            var result = new Quad3();

            var edge1 = p2 - p1;
            var edge2 = p4 - p1;

            Vector3 normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

            result.Pose.Position = (p1 + p2 + p3 + p4) / 4;
            result.Pose.Orientation = normal.ToOrientation();
            result.Size = new Vector2(edge1.Length(), edge2.Length());

            return result;
        }


        public static Quaternion AlignVectors(Vector3 normal1, Vector3 tangent1, Vector3 normal2, Vector3 tangent2, float epsilon = MathExtensions.EPSILON)
        {
            // Normalize input vectors
            normal1 = Vector3.Normalize(normal1);
            tangent1 = Vector3.Normalize(tangent1);
            normal2 = Vector3.Normalize(normal2);
            tangent2 = Vector3.Normalize(tangent2);

            // Step 1: Rotate normal1 to normal2
            Vector3 rotationAxis1 = Vector3.Cross(normal1, normal2);
            float angle1 = MathF.Acos(Vector3.Dot(normal1, normal2));

            Quaternion rotationToAlignNormals = Quaternion.Identity;
            if (rotationAxis1.Length() > epsilon) // avoid division by zero
            {
                rotationAxis1 = Vector3.Normalize(rotationAxis1);
                rotationToAlignNormals = Quaternion.CreateFromAxisAngle(rotationAxis1, angle1);
            }

            // Step 2: Rotate tangent1 to tangent2 around normal2 (now aligned with normal1)
            Vector3 rotatedTangent1 = Vector3.Transform(tangent1, rotationToAlignNormals);

            Vector3 rotationAxis2 = normal2; // This is the axis to rotate around to adjust the tangent
            float angle2 = MathF.Acos(Vector3.Dot(rotatedTangent1, tangent2));

            Quaternion rotationToAlignTangents = Quaternion.Identity;
            if (angle2 > epsilon) // avoid zero rotation
            {
                rotationToAlignTangents = Quaternion.CreateFromAxisAngle(rotationAxis2, angle2);
            }

            // Final rotation is the combination of the two rotations
            Quaternion finalRotation = Quaternion.Concatenate(rotationToAlignTangents, rotationToAlignNormals);
            return finalRotation;
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
    }
}
