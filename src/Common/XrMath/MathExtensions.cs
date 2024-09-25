using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace XrMath
{

    public static class MathExtensions
    {
        #region PLANE
        
        public static bool IntersectLine(this Plane plane, Vector3 point1, Vector3 point2)
        {
            var distance1 = Plane.DotCoordinate(plane, point1);
            var distance2 = Plane.DotCoordinate(plane, point2);

            return distance1 * distance2 < 0;
        }

        #endregion

        #region BOUNDS

        public static bool IntersectFrustum(this Bounds3 bounds, IEnumerable<Plane> planes)
        {
            foreach (var plane in planes)
            {
                if (plane.IntersectLine(bounds.Min, bounds.Max))
                    return true;

                Vector3 positiveVertex = new Vector3(
                    (plane.Normal.X >= 0) ? bounds.Max.X : bounds.Min.X,
                    (plane.Normal.Y >= 0) ? bounds.Max.Y : bounds.Min.Y,
                    (plane.Normal.Z >= 0) ? bounds.Max.Z : bounds.Min.Z
                );

                if (Plane.DotCoordinate(plane, positiveVertex) < 0)
                    return false;
            }

            return true;
        }

        public static Bounds3 ComputeBounds(this IEnumerable<Vector3> points, Matrix4x4 matrix)
        {
            var result = new Bounds3();

            result.Min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            result.Max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            foreach (var point in points)
            {
                var tPoint = point.Transform(matrix);
                result.Min = Vector3.Min(result.Min, tPoint);
                result.Max = Vector3.Max(result.Max, tPoint);
            }

            return result;
        }

        public static Bounds3 Transform(this Bounds3 bounds, Matrix4x4 matrix)
        {
            return bounds.Points.ComputeBounds(matrix);
        }

        public static bool Contains(this Bounds3 bounds, Vector3 point)
        {
            return point.X >= bounds.Min.X && point.X <= bounds.Max.X &&
                   point.Y >= bounds.Min.Y && point.Y <= bounds.Max.Y &&
                   point.Z >= bounds.Min.Z && point.Z <= bounds.Max.Z;
        }

        public static bool Inside(this Bounds3 bounds, Bounds3 other)
        {
            if (bounds.Min.X < other.Min.X || bounds.Max.X > other.Max.X)
                return false;
            if (bounds.Min.Y < other.Min.Y || bounds.Max.Y > other.Max.Y)
                return false;
            if (bounds.Min.Z < other.Min.Z || bounds.Max.Z > other.Max.Z)
                return false;

            return true;
        }

        public static bool Intersects(this Bounds3 bounds, Bounds3 other)
        {
            if (bounds.Max.X < other.Min.X || bounds.Min.X > other.Max.X)
                return false;
            if (bounds.Max.Y < other.Min.Y || bounds.Min.Y > other.Max.Y)
                return false;
            if (bounds.Max.Z < other.Min.Z || bounds.Min.Z > other.Max.Z)
                return false;

            return true;
        }

        public static bool Intersects(this Bounds3 bounds, Line3 line, out float distance)
        {
            Vector3 dir = (line.To - line.From).Normalize(); // direction of the line
            Vector3 tMin = (bounds.Min - line.From) / dir; // minimum t to hit the box
            Vector3 tMax = (bounds.Max - line.From) / dir; // maximum t to hit the box

            // Ensure tMin <= tMax
            Vector3 t1 = Vector3.Min(tMin, tMax);
            Vector3 t2 = Vector3.Max(tMin, tMax);

            float tNear = MathF.Max(MathF.Max(t1.X, t1.Y), t1.Z);
            float tFar = MathF.Min(MathF.Min(t2.X, t2.Y), t2.Z);

            distance = tNear;

            // Return whether intersection exists
            return tNear <= tFar && tFar >= 0;
        }

        #endregion

        #region POSE

        public static bool IsSimilar(this Pose3 value, Pose3 other, float epsilon)
        {
            return value.Position.IsSimilar(other.Position, epsilon) &&
                   value.Orientation.IsSimilar(other.Orientation, epsilon);
        }

        public static Matrix4x4 ToMatrix(this Pose3 pose)
        {
            return Matrix4x4.CreateFromQuaternion(pose.Orientation) *
                   Matrix4x4.CreateTranslation(pose.Position);
        }

        public static Pose3 Inverse(this Pose3 pose)
        {
            return new Pose3
            {
                Orientation = Quaternion.Inverse(pose.Orientation),
                Position = Vector3.Transform(pose.Position * -1, pose.Orientation)
            };
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Transform(this Pose3 pose, Vector3 vector)
        {
            var result = Vector3.Transform(vector, pose.Orientation);
            return result + pose.Position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pose3 Multiply(this Pose3 a, Pose3 b)
        {
            return new Pose3
            {
                Orientation = b.Orientation * a.Orientation,
                Position = a.Transform(b.Position)
            };
        }

        public static Pose3 ToPose(this Matrix4x4 matrix)
        {
            Matrix4x4.Decompose(matrix, out var scale, out var orientation, out var translation);
            return new Pose3
            {
                Orientation = orientation,
                Position = translation
            };
        }

        #endregion

        #region TRIANGLE

        public static bool IsCCW(this Triangle3 triangle)
        {
            var normal = triangle.Normal();
            var dot = Vector3.Dot(normal, Vector3.UnitZ);
            return dot > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Normal(this Triangle3 triangle)
        {
            var edge1 = triangle.V1 - triangle.V0;
            var edge2 = triangle.V2 - triangle.V0;
            var normal = Vector3.Cross(edge1, edge2);
            return Vector3.Normalize(normal);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Triangle3 Transform(this Triangle3 triangle, Matrix4x4 matrix)
        {
            return new Triangle3
            {
                V0 = triangle.V0.Transform(matrix),
                V1 = triangle.V1.Transform(matrix),
                V2 = triangle.V2.Transform(matrix),
            };
        }

        #endregion

        #region VECTOR3

        public static bool IsSameValue(this Vector3 value, float epsilon)
        {
            return MathF.Abs(value.X - value.Y) < epsilon &&
                   MathF.Abs(value.X - value.Z) < epsilon &&
                   MathF.Abs(value.Y - value.Z) < epsilon;
        }

        public static bool IsSimilar(this Vector2 value, Vector2 other, float epsilon)
        {
            return MathF.Abs(value.X - other.X) < epsilon &&
                MathF.Abs(value.Y - other.Y) < epsilon;
        }

        public static bool IsSimilar(this Vector3 value, Vector3 other, float epsilon)
        {
            return MathF.Abs(value.X - other.X) < epsilon &&
                MathF.Abs(value.Y - other.Y) < epsilon &&
                MathF.Abs(value.Z - other.Z) < epsilon;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Transform(this Vector3 vector, Matrix4x4 matrix)
        {
            return Vector3.Transform(vector, matrix);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Normalize(this Vector3 vector)
        {
            return Vector3.Normalize(vector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToDirection(this Vector3 vector, Matrix4x4 matrix)
        {
            return (vector.Transform(matrix) - Vector3.Zero.Transform(matrix)).Normalize();
        }

        public static Quaternion RotationTowards(this Vector3 from, Vector3 to)
        {
            Quaternion result;

            var axis = Vector3.Cross(from, to);
            result.X = axis.X;
            result.Y = axis.Y;
            result.Z = axis.Z;
            result.W = MathF.Sqrt((from.LengthSquared() * to.LengthSquared())) + Vector3.Dot(from, to);

            return Quaternion.Normalize(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Project(this Vector3 self, Matrix4x4 matrix)
        {
            var worldPoint = Vector4.Transform(new Vector4(self, 1), matrix);
            return new Vector3(worldPoint.X, worldPoint.Y, worldPoint.Z) / worldPoint.W;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Project(this Vector3 pos, Plane plane)
        {
            var distance = Plane.DotCoordinate(plane, pos);
            return pos - distance * plane.Normal;
        }

        public static float SignedAngleWith(this Vector3 A, Vector3 B, Vector3 planeNormal)
        {
            A = Vector3.Normalize(A);
            B = Vector3.Normalize(B);
            var cross = Vector3.Cross(A, B);
            var dot = Vector3.Dot(A, B);
            var angle = MathF.Atan2(cross.Length(), dot);
            var sign = MathF.Sign(Vector3.Dot(cross, planeNormal));
            return angle * sign;
        }

        #endregion

        #region RAY 

        public static Line3 ToLine(this Ray3 ray, float len)
        {
            return new Line3()
            {
                From = ray.Origin,
                To = ray.Origin + ray.Direction * len,
            };
        }

        public static Vector3? Intersects(this Ray3 ray, Triangle3 triangle, out float distance, float epsilon = 1e-6f)
        {
            distance = float.PositiveInfinity;

            var edge1 = triangle.V1 - triangle.V0;
            var edge2 = triangle.V2 - triangle.V0;
            var pVec = Vector3.Cross(ray.Direction, edge2);
            var det = Vector3.Dot(edge1, pVec);

            if (MathF.Abs(det) < epsilon)
                return null;

            var invDet = 1.0f / det;
            var tVec = ray.Origin - triangle.V0;
            var u = Vector3.Dot(tVec, pVec) * invDet;

            if (u < 0 || u > 1)
                return null;

            var qVec = Vector3.Cross(tVec, edge1);
            var v = Vector3.Dot(ray.Direction, qVec) * invDet;

            if (v < 0 || u + v > 1)
                return null;

            var t = Vector3.Dot(edge2, qVec) * invDet;

            if (t > 0)
            {
                var intersectionPoint = ray.Origin + t * ray.Direction;
                distance = t;
                return intersectionPoint;
            }
            else
                return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Ray3 Transform(this Ray3 ray, Matrix4x4 matrix)
        {
            var v0 = Vector3.Transform(ray.Origin, matrix);
            var v1 = Vector3.Transform(ray.Origin + ray.Direction, matrix);

            return new Ray3
            {
                Origin = v0,
                Direction = Vector3.Normalize(v1 - v0)
            };
        }

        #endregion

        #region QUATERNION

        public static bool IsSimilar(this Quaternion value, Quaternion other, float epsilon)
        {
            return MathF.Abs(value.X - other.X) < epsilon &&
                MathF.Abs(value.Y - other.Y) < epsilon &&
                MathF.Abs(value.Z - other.Z) < epsilon &&
                MathF.Abs(value.W - other.W) < epsilon;
        }

        public static Vector3 ToEuler(this Quaternion q)
        {
            Vector3 res;
            q = Quaternion.Normalize(q);
            res.X = MathF.Atan2(2.0f * (q.Y * q.Z + q.W * q.X), q.W * q.W - q.X * q.X - q.Y * q.Y + q.Z * q.Z);
            res.Y = MathF.Asin(-2.0f * (q.X * q.Z - q.W * q.Y));
            res.Z = MathF.Atan2(2.0f * (q.X * q.Y + q.W * q.Z), q.W * q.W + q.X * q.X - q.Y * q.Y - q.Z * q.Z);
            return res;
        }

        public static Matrix3x3 ToMatrix(this Quaternion quaternion)
        {
            // Extract individual components of the quaternion
            float x = quaternion.X;
            float y = quaternion.Y;
            float z = quaternion.Z;
            float w = quaternion.W;

            // Calculate matrix elements
            float xx = x * x;
            float xy = x * y;
            float xz = x * z;
            float xw = x * w;

            float yy = y * y;
            float yz = y * z;
            float yw = y * w;

            float zz = z * z;
            float zw = z * w;

            // Construct the rotation matrix
            return new Matrix3x3(
                1 - 2 * (yy + zz), 2 * (xy - zw), 2 * (xz + yw),
                2 * (xy + zw), 1 - 2 * (xx + zz), 2 * (yz - xw),
                2 * (xz - yw), 2 * (yz + xw), 1 - 2 * (xx + yy)
            );
        }

        #endregion

        #region COLOR

        public static string ToHex(this Color color)
        {
            static string ToHex(float value)
            {
                var iVal = (int)Math.Max(0, Math.Min(255, value * 255));
                return iVal.ToString("X").PadLeft(2, '0');
            }

            return $"#{ToHex(color.R)}{ToHex(color.G)}{ToHex(color.B)}{ToHex(color.A)}";
        }

        public static string ToHexARGB(this Color color)
        {
            static string ToHex(float value)
            {
                var iVal = (int)Math.Max(0, Math.Min(255, value * 255));
                return iVal.ToString("X").PadLeft(2, '0');
            }

            return $"#{ToHex(color.A)}{ToHex(color.R)}{ToHex(color.G)}{ToHex(color.B)}";
        }



        #endregion

        #region MISC

        public static bool Contains(this Rect2 rect, Vector2 point)
        {
            return point.X >= rect.X && point.X <= rect.Right &&
                   point.Y >= rect.Y && point.Y <= rect.Bottom;
        }

        public static bool Intersects(this Sphere sphere, Sphere other, out float offset)
        {
            var dist = (sphere.Center - other.Center).Length();

            offset = dist - (sphere.Radius + other.Radius);

            return offset < 0;
        }



        #endregion
    }
}
