using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace XrMath
{

    public static class MathExtensions
    {
        public const float EPSILON = 1e-6f;    

        #region Matrix4x4

        public unsafe static Matrix4x4 InvertRigidBody(this Matrix4x4 src)
        {
            var result = stackalloc float[16];
            var srcArray = (float*)&src;

            result[0] = srcArray[0];
            result[1] = srcArray[4];
            result[2] = srcArray[8];
            result[3] = 0.0f;
            result[4] = srcArray[1];
            result[5] = srcArray[5];
            result[6] = srcArray[9];
            result[7] = 0.0f;
            result[8] = srcArray[2];
            result[9] = srcArray[6];
            result[10] = srcArray[10];
            result[11] = 0.0f;
            result[12] = -(srcArray[0] * srcArray[12] + srcArray[1] * srcArray[13] + srcArray[2] * srcArray[14]);
            result[13] = -(srcArray[4] * srcArray[12] + srcArray[5] * srcArray[13] + srcArray[6] * srcArray[14]);
            result[14] = -(srcArray[8] * srcArray[12] + srcArray[9] * srcArray[13] + srcArray[10] * srcArray[14]);
            result[15] = 1.0f;

            return *(Matrix4x4*)result;
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
            point -= self.Size / 2;

            return self.Pose.Transform(new Vector3(point.X, point.Y, 0));
        }

        public static IEnumerable<Vector3> Corners(this Quad3 self)
        {

            yield return self.PointAt(0, 0);
            yield return self.PointAt(self.Size.X, 0);
            yield return self.PointAt(self.Size.X, self.Size.Y);
            yield return self.PointAt(0, self.Size.Y);

        }

        public static Vector2 LocalPointAt(this Quad3 self, Vector3 worldPoint)
        {
            var local = self.Pose.Inverse().Transform(worldPoint);

            return new Vector2(local.X, local.Y) + self.Size / 2;
        }

        #endregion

        #region PLANE

        public static bool IntersectLine(this Plane self, Vector3 point1, Vector3 point2)
        {
            var distance1 = Plane.DotCoordinate(self, point1);
            var distance2 = Plane.DotCoordinate(self, point2);

            return distance1 * distance2 < 0;
        }

        #endregion

        #region BOUNDS

        public static IEnumerable<Quad3> Faces(this Bounds3 self)
        {
            var C1 = new Vector3(self.Min.X, self.Min.Y, self.Min.Z);
            var C2 = new Vector3(self.Max.X, self.Min.Y, self.Min.Z);
            var C3 = new Vector3(self.Max.X, self.Max.Y, self.Min.Z);
            var C4 = new Vector3(self.Min.X, self.Max.Y, self.Min.Z);
            var C5 = new Vector3(self.Min.X, self.Min.Y, self.Max.Z);
            var C6 = new Vector3(self.Max.X, self.Min.Y, self.Max.Z);
            var C7 = new Vector3(self.Max.X, self.Max.Y, self.Max.Z);
            var C8 = new Vector3(self.Min.X, self.Max.Y, self.Max.Z);

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

        public static bool IntersectFrustum(this Bounds3 self, IEnumerable<Plane> planes)
        {
            foreach (var plane in planes)
            {
                if (plane.IntersectLine(self.Min, self.Max))
                    return true;

                var positiveVertex = new Vector3(
                    (plane.Normal.X >= 0) ? self.Max.X : self.Min.X,
                    (plane.Normal.Y >= 0) ? self.Max.Y : self.Min.Y,
                    (plane.Normal.Z >= 0) ? self.Max.Z : self.Min.Z
                );

                if (Plane.DotCoordinate(plane, positiveVertex) < 0)
                    return false;
            }

            return true;
        }


        public static Bounds3 Transform(this Bounds3 self, Matrix4x4 matrix)
        {
            return self.Points.ComputeBounds(matrix);
        }

        public static bool Contains(this Bounds3 self, Vector3 point)
        {
            return point.X >= self.Min.X && point.X <= self.Max.X &&
                   point.Y >= self.Min.Y && point.Y <= self.Max.Y &&
                   point.Z >= self.Min.Z && point.Z <= self.Max.Z;
        }

        public static bool Inside(this Bounds3 self, Bounds3 other)
        {
            if (self.Min.X < other.Min.X || self.Max.X > other.Max.X)
                return false;
            if (self.Min.Y < other.Min.Y || self.Max.Y > other.Max.Y)
                return false;
            if (self.Min.Z < other.Min.Z || self.Max.Z > other.Max.Z)
                return false;

            return true;
        }

        public static bool Intersects(this Bounds3 self, Bounds3 other)
        {
            if (self.Max.X < other.Min.X || self.Min.X > other.Max.X)
                return false;
            if (self.Max.Y < other.Min.Y || self.Min.Y > other.Max.Y)
                return false;
            if (self.Max.Z < other.Min.Z || self.Min.Z > other.Max.Z)
                return false;

            return true;
        }

        public static bool Intersects(this Bounds3 self, Bounds3 other, out Bounds3 result)
        {
            float intersectMinX = Math.Max(self.Min.X, other.Min.X);
            float intersectMaxX = Math.Min(self.Max.X, other.Max.X);

            float intersectMinY = Math.Max(self.Min.Y, other.Min.Y);
            float intersectMaxY = Math.Min(self.Max.Y, other.Max.Y);

            float intersectMinZ = Math.Max(self.Min.Z, other.Min.Z);
            float intersectMaxZ = Math.Min(self.Max.Z, other.Max.Z);

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


        public static bool Intersects(this Bounds3 self, Line3 line, out float distance)
        {
            var dir = line.Direction();
            var tMin = (self.Min - line.From) / dir; 
            var tMax = (self.Max - line.From) / dir;

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

        public static bool IsSimilar(this Pose3 self, Pose3 other, float epsilon = EPSILON)
        {
            return self.Position.IsSimilar(other.Position, epsilon) &&
                   self.Orientation.IsSimilar(other.Orientation, epsilon);
        }

        public static Matrix4x4 ToMatrix(this Pose3 self)
        {
            return Matrix4x4.CreateFromQuaternion(self.Orientation) *
                   Matrix4x4.CreateTranslation(self.Position);
        }

        public static Pose3 Inverse(this Pose3 self)
        {
            var quat = Quaternion.Conjugate(self.Orientation);

            return new Pose3
            {
                Orientation = quat,
                Position = Vector3.Transform(-self.Position, quat)
            };
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Transform(this Pose3 pose, Vector3 vector)
        {
            var result = Vector3.Transform(vector, pose.Orientation);
            return result + pose.Position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pose3 Multiply(this Pose3 self, Pose3 other)
        {
            return new Pose3
            {
                Orientation = self.Orientation * other.Orientation,
                Position = self.Transform(other.Position)
            };
        }

        public static Pose3 ToPose(this Matrix4x4 self)
        {
            Matrix4x4.Decompose(self, out var scale, out var orientation, out var translation);
            return new Pose3
            {
                Orientation = orientation,
                Position = translation
            };
        }

        #endregion

        #region TRIANGLE

        public static bool IsCCW(this Triangle3 self)
        {
            var normal = self.Normal();
            var dot = Vector3.Dot(normal, Vector3.UnitZ);
            return dot > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Normal(this Triangle3 self)
        {
            var edge1 = self.V1 - self.V0;
            var edge2 = self.V2 - self.V0;
            var normal = Vector3.Cross(edge1, edge2);
            return Vector3.Normalize(normal);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Triangle3 Transform(this Triangle3 self, Matrix4x4 matrix)
        {
            return new Triangle3
            {
                V0 = self.V0.Transform(matrix),
                V1 = self.V1.Transform(matrix),
                V2 = self.V2.Transform(matrix),
            };
        }

        #endregion

        #region VECTOR3
        
        public static Bounds3 ComputeBounds(this IEnumerable<Vector3> self)
        {
            var builder = new BoundsBuilder();
            builder.Add(self);
            return builder.Result;
        }

        public static Bounds3 ComputeBounds(this IEnumerable<Vector3> self, Matrix4x4 matrix)
        {
            var builder = new BoundsBuilder();
            builder.Add(self.Select(a => a.Transform(matrix)));
            return builder.Result;
        }

        public static Quaternion ToOrientation(this Vector3 self)
        {
            return Vector3.UnitZ.RotationTowards(self);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSameValue(this Vector3 value, float epsilon)
        {
            return MathF.Abs(value.X - value.Y) < epsilon &&
                   MathF.Abs(value.X - value.Z) < epsilon &&
                   MathF.Abs(value.Y - value.Z) < epsilon;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSimilar(this Vector3 self, Vector3 other, float epsilon)
        {
            return MathF.Abs(self.X - other.X) < epsilon &&
                   MathF.Abs(self.Y - other.Y) < epsilon &&
                   MathF.Abs(self.Z - other.Z) < epsilon;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Transform(this Vector3 self, Matrix4x4 matrix)
        {
            return Vector3.Transform(self, matrix);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Normalize(this Vector3 self)
        {
            return Vector3.Normalize(self);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToDirection(this Vector3 self, Matrix4x4 matrix)
        {
            return (self.Transform(matrix) - Vector3.Zero.Transform(matrix)).Normalize();
        }

        public static Quaternion RotationTowards(this Vector3 from, Vector3 to, float epsilon = EPSILON)
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

        public static float SignedAngleWith(this Vector3 self, Vector3 other, Vector3 planeNormal)
        {
            self = Vector3.Normalize(self);
            other = Vector3.Normalize(other);
            var cross = Vector3.Cross(self, other);
            var dot = Vector3.Dot(self, other);
            var angle = MathF.Atan2(cross.Length(), dot);
            var sign = MathF.Sign(Vector3.Dot(cross, planeNormal));
            return angle * sign;
        }

        #endregion

        #region RAY 

        public static Line3 ToLine(this Ray3 self, float len)
        {
            return new Line3()
            {
                From = self.Origin,
                To = self.PointAt(len)
            };
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 PointAt(this Ray3 self, float distance)
        {
            return self.Origin + self.Direction * distance;
        }


        public static Vector3? Intersects(this Ray3 self, Triangle3 triangle, out float distance, float epsilon = EPSILON)
        {
            distance = float.PositiveInfinity;

            var edge1 = triangle.V1 - triangle.V0;
            var edge2 = triangle.V2 - triangle.V0;
            var pVec = Vector3.Cross(self.Direction, edge2);
            var det = Vector3.Dot(edge1, pVec);

            if (MathF.Abs(det) < epsilon)
                return null;

            var invDet = 1.0f / det;
            var tVec = self.Origin - triangle.V0;
            var u = Vector3.Dot(tVec, pVec) * invDet;

            if (u < 0 || u > 1)
                return null;

            var qVec = Vector3.Cross(tVec, edge1);
            var v = Vector3.Dot(self.Direction, qVec) * invDet;

            if (v < 0 || u + v > 1)
                return null;

            var t = Vector3.Dot(edge2, qVec) * invDet;

            if (t > 0)
            { 
                var intersectionPoint = self.PointAt(t); 
                distance = t;
                return intersectionPoint;
            }
            else
                return null;
        }

        public static bool Intersects(this Ray3 self, Quad3 quad, out Vector3 intersectionPoint, float epsilon = EPSILON)
        {
            if (!self.Intersects(quad.ToPlane(), out intersectionPoint, epsilon))
                return false;

            var local = quad.LocalPointAt(intersectionPoint);

            return local.InRange(Vector2.Zero, quad.Size);  
        }

        public static bool Intersects(this Ray3 self, Plane plane, out Vector3 intersectionPoint, float epsilon = EPSILON)
        {
            intersectionPoint = Vector3.Zero;
            var denominator = Vector3.Dot(self.Direction, plane.Normal);
            if (Math.Abs(denominator) < epsilon)
                return false;

            var numerator = -Vector3.Dot(self.Origin, plane.Normal) - plane.D;
            var t = numerator / denominator;
            if (t < 0)
                return false;

            intersectionPoint = self.PointAt(t);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Ray3 Transform(this Ray3 self, Matrix4x4 matrix)
        {
            var v0 = Vector3.Transform(self.Origin, matrix);
            var v1 = Vector3.Transform(self.Origin + self.Direction, matrix);

            return new Ray3
            {
                Origin = v0,
                Direction = Vector3.Normalize(v1 - v0)
            };
        }

        #endregion

        #region QUATERNION

        public static Quaternion Subtract(this Quaternion self, Quaternion other)
        {
            return self * Quaternion.Inverse(other);
        }

        public static Quaternion AddDelta(this Quaternion self, Quaternion delta)
        {
            return delta * self;
        }

        public static bool IsSimilar(this Quaternion self, Quaternion other, float epsilon)
        {
            return MathF.Abs(self.X - other.X) < epsilon &&
                MathF.Abs(self.Y - other.Y) < epsilon &&
                MathF.Abs(self.Z - other.Z) < epsilon &&
                MathF.Abs(self.W - other.W) < epsilon;
        }

        public static Vector3 ToEuler(this Quaternion self)
        {
            Vector3 res;

            self = Quaternion.Normalize(self);

            float sinp = -2.0f * (self.X * self.Z - self.W * self.Y);
            sinp = Math.Clamp(sinp, -1.0f, 1.0f);

            res.X = MathF.Atan2(2.0f * (self.Y * self.Z + self.W * self.X), self.W * self.W - self.X * self.X - self.Y * self.Y + self.Z * self.Z);
            res.Y = MathF.Asin(sinp);
            res.Z = MathF.Atan2(2.0f * (self.X * self.Y + self.W * self.Z), self.W * self.W + self.X * self.X - self.Y * self.Y - self.Z * self.Z);

            return res;
        }

        public static Matrix3x3 ToMatrix3x3(this Quaternion self)
        {
            // Extract individual components of the quaternion
            float x = self.X;
            float y = self.Y;
            float z = self.Z;
            float w = self.W;

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

        public static string ToHex(this Color self)
        {
            static string ToHex(float value)
            {
                var iVal = (int)Math.Max(0, Math.Min(255, value * 255));
                return iVal.ToString("X").PadLeft(2, '0');
            }

            return $"#{ToHex(self.R)}{ToHex(self.G)}{ToHex(self.B)}{ToHex(self.A)}";
        }

        public static string ToHexArgb(this Color self)
        {
            static string ToHex(float value)
            {
                var iVal = (int)Math.Max(0, Math.Min(255, value * 255));
                return iVal.ToString("X").PadLeft(2, '0');
            }

            return $"#{ToHex(self.A)}{ToHex(self.R)}{ToHex(self.G)}{ToHex(self.B)}";
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

        #region VECTOR2

        public static bool InRange(this Vector2 self, Vector2 min, Vector2 max)
        {
            return self.X >= min.X && self.X <= max.X &&
                   self.Y >= min.Y && self.Y <= max.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSimilar(this Vector2 self, Vector2 other, float epsilon)
        {
            return MathF.Abs(self.X - other.X) < epsilon &&
                   MathF.Abs(self.Y - other.Y) < epsilon;
        }


        #endregion

        #region MISC

        public static bool Contains(this Rect2 self, Vector2 point)
        {
            return point.X >= self.X && point.X <= self.Right &&
                   point.Y >= self.Y && point.Y <= self.Bottom;
        }

        public static bool Intersects(this Sphere self, Sphere other, out float offset)
        {
            var dist = (self.Center - other.Center).Length();

            offset = dist - (self.Radius + other.Radius);

            return offset < 0;
        }



        #endregion
    }
}
