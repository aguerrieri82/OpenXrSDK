using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace XrMath
{

    public static class MathExtensions
    {
        #region Matrix4x4

        public static bool DecomposeV2(this Matrix4x4 matrix, out Vector3 translation, out Quaternion rotation, out Vector3 scale)
        {
            // Extract the translation
            translation = new Vector3(matrix.M41, matrix.M42, matrix.M43);

            // Extract the scale
            scale = new Vector3(
                new Vector3(matrix.M11, matrix.M12, matrix.M13).Length(),
                new Vector3(matrix.M21, matrix.M22, matrix.M23).Length(),
                new Vector3(matrix.M31, matrix.M32, matrix.M33).Length()
            );

            // If any scale component is zero, return false
            if (scale.X == 0.0f || scale.Y == 0.0f || scale.Z == 0.0f)
            {
                rotation = Quaternion.Identity;
                return false;
            }

            // Normalize the rows of the matrix to remove the scale from the rotation
            Matrix4x4 rotationMatrix = new Matrix4x4(
                matrix.M11 / scale.X, matrix.M12 / scale.X, matrix.M13 / scale.X, 0.0f,
                matrix.M21 / scale.Y, matrix.M22 / scale.Y, matrix.M23 / scale.Y, 0.0f,
                matrix.M31 / scale.Z, matrix.M32 / scale.Z, matrix.M33 / scale.Z, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f
            );

            // Convert the 3x3 rotation matrix into a quaternion
            rotation = Quaternion.CreateFromRotationMatrix(rotationMatrix);

            return true;
        }

        #endregion

        #region QUOD3

        public static Vector3 Normal(this Quad3 self)
        {
            return Vector3.Transform(Vector3.UnitZ, self.Pose.Orientation).Normalize();
        }

        public static Vector3 Tangent(this Quad3 self)
        {
            return Vector3.Transform(Vector3.UnitX, self.Pose.Orientation).Normalize();
        }

        public static Plane ToPlane(this Quad3 self)
        {
            var normal = self.Normal();
            return new Plane(normal, -Vector3.Dot(normal, self.Pose.Position));
        }

        public static Vector3 PointAt(this Quad3 self, float x, float y)
        {
            return self.PointAt(new Vector2(x, y));
        }

        public static Vector3 PointAt(this Quad3 self, Vector2 point)
        {
            return self.Pose.Transform(new Vector3(point.X, point.Y, 0));
        }

        public static IEnumerable<Vector3> Corners(this Quad3 self)
        {
            var halfSize = self.Size / 2;

            yield return self.PointAt(-halfSize.X, -halfSize.Y);
            yield return self.PointAt(halfSize.X, -halfSize.Y);
            yield return self.PointAt(halfSize.X, halfSize.Y);
            yield return self.PointAt(-halfSize.X, halfSize.Y);

        }

        #endregion

        #region PLANE

        public static bool IntersectLine(this Plane plane, Vector3 point1, Vector3 point2)
        {
            var distance1 = Plane.DotCoordinate(plane, point1);
            var distance2 = Plane.DotCoordinate(plane, point2);

            return distance1 * distance2 < 0;
        }

        #endregion

        #region BOUNDS

        public static IEnumerable<Quad3> Faces(this Bounds3 bounds)
        {
            var C1 = new Vector3(bounds.Min.X, bounds.Min.Y, bounds.Min.Z);
            var C2 = new Vector3(bounds.Max.X, bounds.Min.Y, bounds.Min.Z);
            var C3 = new Vector3(bounds.Max.X, bounds.Max.Y, bounds.Min.Z);
            var C4 = new Vector3(bounds.Min.X, bounds.Max.Y, bounds.Min.Z);
            var C5 = new Vector3(bounds.Min.X, bounds.Min.Y, bounds.Max.Z);
            var C6 = new Vector3(bounds.Max.X, bounds.Min.Y, bounds.Max.Z);
            var C7 = new Vector3(bounds.Max.X, bounds.Max.Y, bounds.Max.Z);
            var C8 = new Vector3(bounds.Min.X, bounds.Max.Y, bounds.Max.Z);

            var quads = new Quad3[6];

            // Bottom face (XY plane at Min.Z)
            quads[0] = MathUtils.QuadFromEdges(C4, C3, C2, C1);

            // Top face (XY plane at Max.Z)
            quads[1] = MathUtils.QuadFromEdges(C5, C6, C7, C8);

            // Front face (XZ plane at Min.Y)
            quads[2] = MathUtils.QuadFromEdges(C1, C2, C6, C5);

            // Back face (XZ plane at Max.Y)
            quads[3] = MathUtils.QuadFromEdges(C8, C7, C3, C4);

            // Left face (YZ plane at Min.X)
            quads[4] = MathUtils.QuadFromEdges(C8, C4, C1, C5);

            // Right face (YZ plane at Max.X)
            quads[5] = MathUtils.QuadFromEdges(C3, C7, C6, C2);

            return quads;
        }

        public static bool IntersectFrustum(this Bounds3 bounds, IEnumerable<Plane> planes)
        {
            foreach (var plane in planes)
            {
                if (plane.IntersectLine(bounds.Min, bounds.Max))
                    return true;

                var positiveVertex = new Vector3(
                    (plane.Normal.X >= 0) ? bounds.Max.X : bounds.Min.X,
                    (plane.Normal.Y >= 0) ? bounds.Max.Y : bounds.Min.Y,
                    (plane.Normal.Z >= 0) ? bounds.Max.Z : bounds.Min.Z
                );

                if (Plane.DotCoordinate(plane, positiveVertex) < 0)
                    return false;
            }

            return true;
        }

        public static Bounds3 ComputeBounds(this IEnumerable<Vector3> points)
        {
            var builder = new BoundsBuilder();
            builder.Add(points);
            return builder.Result;
        }

        public static Bounds3 ComputeBounds(this IEnumerable<Vector3> points, Matrix4x4 matrix)
        {
            var builder = new BoundsBuilder();
            builder.Add(points.Select(a => a.Transform(matrix)));
            return builder.Result;
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

        public static bool Intersects(this Bounds3 a, Bounds3 b, out Bounds3 result)
        {
            float intersectMinX = Math.Max(a.Min.X, b.Min.X);
            float intersectMaxX = Math.Min(a.Max.X, b.Max.X);

            float intersectMinY = Math.Max(a.Min.Y, b.Min.Y);
            float intersectMaxY = Math.Min(a.Max.Y, b.Max.Y);

            float intersectMinZ = Math.Max(a.Min.Z, b.Min.Z);
            float intersectMaxZ = Math.Min(a.Max.Z, b.Max.Z);

            if (intersectMinX > intersectMaxX || intersectMinY > intersectMaxY || intersectMinZ > intersectMaxZ)
            {
                result = new Bounds3();
                return false;
            }

            result = new Bounds3()
            {
                Min = new Vector3(intersectMinX, intersectMinY, intersectMinZ),
                Max = new Vector3(intersectMaxX, intersectMaxY, intersectMaxZ)
            };

            return true;
        }


        public static bool Intersects(this Bounds3 bounds, Line3 line, out float distance)
        {
            var dir = (line.To - line.From).Normalize(); // direction of the line
            var tMin = (bounds.Min - line.From) / dir; // minimum t to hit the box
            var tMax = (bounds.Max - line.From) / dir; // maximum t to hit the box

            // Ensure tMin <= tMax
            var t1 = Vector3.Min(tMin, tMax);
            var t2 = Vector3.Max(tMin, tMax);

            var tNear = MathF.Max(MathF.Max(t1.X, t1.Y), t1.Z);
            var tFar = MathF.Min(MathF.Min(t2.X, t2.Y), t2.Z);

            distance = tNear;

            // Return whether intersection exists
            return tNear <= tFar && tFar >= 0;
        }

        #endregion

        #region POSE

        public static bool IsSimilar(this Pose3 value, Pose3 other, float epsilon = 0.001f)
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
                Orientation = a.Orientation * b.Orientation,
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

        public static Quaternion ToOrientation(this Vector3 direction)
        {
            return Vector3.UnitZ.RotationTowards(direction);
        }

        public static float MinDistanceTo(this Vector3[] self, Vector3 point)
        {
            var result = float.PositiveInfinity;

            for (var i = 0; i < self.Length; i++)
            {
                var d = Vector3.Distance(point, self[i]);
                result = MathF.Min(result, d);
            }

            return result;
        }

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

        public static Quaternion RotationTowards(this Vector3 from, Vector3 to, float epsilon = 1e-6f)
        {
            // Normalize the input vectors
            from = Vector3.Normalize(from);
            to = Vector3.Normalize(to);

            // Compute the dot product to find the cosine of the angle between the vectors
            var dot = Vector3.Dot(from, to);

            // Handle the case where the vectors are already aligned
            if (MathF.Abs(dot - 1.0f) < epsilon)
                return Quaternion.Identity; // No rotation needed

            // Handle the case where the vectors are opposite (180-degree rotation)
            if (MathF.Abs(dot + 1.0f) < epsilon)
            {
                // Find an orthogonal vector to use as the rotation axis
                var orthogonalAxis = Vector3.Cross(from, Vector3.UnitX);
                if (orthogonalAxis.LengthSquared() < epsilon)
                    orthogonalAxis = Vector3.Cross(from, Vector3.UnitY); // Try a different axis if the first fails

                orthogonalAxis = Vector3.Normalize(orthogonalAxis);
                return Quaternion.CreateFromAxisAngle(orthogonalAxis, MathF.PI); // 180-degree rotation
            }

            // Compute the axis of rotation (cross product of from and to)
            Vector3 rotationAxis = Vector3.Cross(from, to);
            rotationAxis = Vector3.Normalize(rotationAxis);

            // Compute the angle between the vectors (acos of the dot product)
            float angle = MathF.Acos(dot);

            // Create the quaternion representing the rotation
            return Quaternion.CreateFromAxisAngle(rotationAxis, angle);
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
                To = ray.PointAt(len)
            };
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 PointAt(this Ray3 ray, float distance)
        {
            return ray.Origin + ray.Direction * distance;
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

        public static bool Intersects(this Ray3 ray, Plane plane, out Vector3 intersectionPoint)
        {
            intersectionPoint = Vector3.Zero;
            var denominator = Vector3.Dot(ray.Direction, plane.Normal);
            if (Math.Abs(denominator) < 1e-6)
                return false;

            var numerator = -Vector3.Dot(ray.Origin, plane.Normal) - plane.D;
            var t = numerator / denominator;
            if (t < 0)
                return false;

            intersectionPoint = ray.PointAt(t);
            return true;
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

            float sinp = -2.0f * (q.X * q.Z - q.W * q.Y);
            sinp = Math.Clamp(sinp, -1.0f, 1.0f);

            res.X = MathF.Atan2(2.0f * (q.Y * q.Z + q.W * q.X), q.W * q.W - q.X * q.X - q.Y * q.Y + q.Z * q.Z);
            res.Y = MathF.Asin(sinp);
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

        #region LINE2


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Direction(this Line3 self)
        {
            return (self.To - self.From).Normalize();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Length(this Line3 self)
        {
            return Vector3.Distance(self.From, self.To);    
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 PointAt(this Line3 self, float distance)
        {
            return self.From + self.Direction() * distance; 
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 PointAtOffset(this Line3 self, float t)
        {
            return self.From + self.Direction() * (t * self.Length());
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
