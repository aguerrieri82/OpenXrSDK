using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace XrMath
{

    public static class MathExtensions
    {
        public const float EPSILON = 1e-6f;

        #region MATRIX4x4


        public static bool DecomposeDouble(this Matrix4x4 matrix, out Vector3 scale, out Quaternion rotation, out Vector3 translation)
        {
            return Matrix4x4.Decompose(matrix, out scale, out rotation, out translation);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pose3 ToPose(this Matrix4x4 self)
        {
            Matrix4x4.Decompose(self, out var scale, out var orientation, out var translation);
            return new Pose3
            {
                Orientation = orientation,
                Position = translation
            };
        }

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

        public static Matrix4x4 InterpolateWorldMatrix(this Matrix4x4 matrix1, Matrix4x4 matrix2, float t)
        {
            // Extract position vectors
            var position1 = new Vector3(matrix1.M41, matrix1.M42, matrix1.M43);
            var position2 = new Vector3(matrix2.M41, matrix2.M42, matrix2.M43);

            // Interpolate position
            var interpolatedPosition = Vector3.Lerp(position1, position2, t);

            // Extract rotation quaternions
            var rotation1 = Quaternion.CreateFromRotationMatrix(matrix1);
            var rotation2 = Quaternion.CreateFromRotationMatrix(matrix2);

            // Interpolate rotation
            var interpolatedRotation = Quaternion.Slerp(rotation1, rotation2, t);

            // Recompose the interpolated matrix
            var result = Matrix4x4.CreateFromQuaternion(interpolatedRotation);
            result.M41 = interpolatedPosition.X;
            result.M42 = interpolatedPosition.Y;
            result.M43 = interpolatedPosition.Z;

            return result;
        }

        #endregion

        #region QUOD3

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Normal(this Quad3 self)
        {
            return Vector3.Transform(Vector3.UnitZ, self.Pose.Orientation).Normalize();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Tangent(this Quad3 self)
        {
            return Vector3.Transform(Vector3.UnitX, self.Pose.Orientation).Normalize();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Plane ToPlane(this Quad3 self)
        {
            var normal = self.Normal();
            return new Plane(normal, -Vector3.Dot(normal, self.Pose.Position));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 PointAt(this Quad3 self, float x, float y)
        {
            return self.PointAt(new Vector2(x, y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 PointAt(this Quad3 self, Vector2 point)
        {
            point -= self.Size / 2;

            return self.Pose.Transform(new Vector3(point.X, point.Y, 0));
        }

        public static Vector3 Center(this Quad3 self)
        {
            var sum = Vector3.Zero;
            foreach (var item in self.Corners())
                sum += item;
            return sum / 4;
        }

        public static IEnumerable<Vector3> Corners(this Quad3 self)
        {
            yield return self.PointAt(0, 0);
            yield return self.PointAt(self.Size.X, 0);
            yield return self.PointAt(self.Size.X, self.Size.Y);
            yield return self.PointAt(0, self.Size.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 LocalPointAt(this Quad3 self, Vector3 worldPoint)
        {
            var local = self.Pose.Inverse().Transform(worldPoint);
            return new Vector2(local.X, local.Y) + self.Size / 2;
        }

        #endregion

        #region PLANE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 ToVector4(this Plane self)
        {
            return new Vector4(self.Normal, self.D);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Project(this Plane self, Vector3 point)
        {
            return point - self.Distance(point) * self.Normal;
        }

        public static void OrthogonalAxis(this Plane self, out Vector3 uAxis, out Vector3 vAxis)
        {
            var arbitrary = Math.Abs(self.Normal.X) > Math.Abs(self.Normal.Z)
                     ? new Vector3(-self.Normal.Y, self.Normal.X, 0)
                     : new Vector3(0, -self.Normal.Z, self.Normal.Y);

            uAxis = Vector3.Normalize(Vector3.Cross(arbitrary, self.Normal));
            vAxis = Vector3.Normalize(Vector3.Cross(self.Normal, uAxis));
        }


        public static Vector2 ProjectUV(this Plane self, Vector3 point)
        {
            self.OrthogonalAxis(out var uAxis, out var vAxis);
            return self.ProjectUV(point, uAxis, vAxis);
        }

        public static Vector2 ProjectUV(this Plane self, Vector3 point, Vector3 uAxis, Vector3 vAxis)
        {
            var projectedPoint = self.Project(point);

            float x = Vector3.Dot(projectedPoint, uAxis);
            float y = Vector3.Dot(projectedPoint, vAxis);

            return new Vector2(x, y);
        }

        public static Vector3 UnprojectUV(this Plane self, Vector2 point)
        {
            self.OrthogonalAxis(out var uAxis, out var vAxis);
            return UnprojectUV(self, point, uAxis, vAxis);
        }

        public static Vector3 UnprojectUV(this Plane self, Vector2 point, Vector3 uAxis, Vector3 vAxis)
        {
            var planePoint = self.Project(Vector3.Zero);

            var pointInPlane = planePoint + point.X * uAxis + point.Y * vAxis;

            return pointInPlane;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Distance(this Plane self, Vector3 point)
        {
            return self.Normal.Dot(point) + self.D;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DotCoordinate(this Plane self, Vector3 point)
        {
            return Plane.Dot(self, Vector4.Create(point, 1f));
        }

        public static bool Intersects(this Plane self, Line3 line, out Vector3 point)
        {
            point = Vector3.Zero;

            Vector3 direction = line.To - line.From;
            var denominator = Vector3.Dot(direction, self.Normal);
            if (Math.Abs(denominator) < EPSILON)
                return false;

            var numerator = -(Vector3.Dot(line.From, self.Normal) + self.D);
            var t = numerator / denominator;
            if (t < -EPSILON || t > 1 + EPSILON)
                return false;

            point = line.From + t * direction;
            return true;
        }



        #endregion

        #region BOUNDS

        public static CubeFaces Faces(this Bounds3 self)
        {
            var C1 = new Vector3(self.Min.X, self.Min.Y, self.Min.Z);
            var C2 = new Vector3(self.Max.X, self.Min.Y, self.Min.Z);
            var C3 = new Vector3(self.Max.X, self.Max.Y, self.Min.Z);
            var C4 = new Vector3(self.Min.X, self.Max.Y, self.Min.Z);
            var C5 = new Vector3(self.Min.X, self.Min.Y, self.Max.Z);
            var C6 = new Vector3(self.Max.X, self.Min.Y, self.Max.Z);
            var C7 = new Vector3(self.Max.X, self.Max.Y, self.Max.Z);
            var C8 = new Vector3(self.Min.X, self.Max.Y, self.Max.Z);

            var result = new CubeFaces();

            // Bottom face (XY plane at Min.Z)
            result.Back = MathUtils.QuadFromEdges(C4, C3, C2, C1);

            // Top face (XY plane at Max.Z)
            result.Front = MathUtils.QuadFromEdges(C5, C6, C7, C8);

            // Front face (XZ plane at Min.Y)
            result.Bottom = MathUtils.QuadFromEdges(C1, C2, C6, C5);

            // Back face (XZ plane at Max.Y)
            result.Top = MathUtils.QuadFromEdges(C8, C7, C3, C4);

            // Left face (YZ plane at Min.X)
            result.Left = MathUtils.QuadFromEdges(C8, C4, C1, C5);

            // Right face (YZ plane at Max.X)
            result.Right = MathUtils.QuadFromEdges(C3, C7, C6, C2);

            return result;
        }

        public static bool IntersectFrustum(this Bounds3 self, Plane[] planes)
        {
            for (var i = 0; i < planes.Length; i++)
            {
                var plane = planes[i];
                /*
                if (plane.IntersectLine(self.Min, self.Max))
                    return true;
                */
                var positiveVertex = new Vector3(
                    (plane.Normal.X >= 0) ? self.Max.X : self.Min.X,
                    (plane.Normal.Y >= 0) ? self.Max.Y : self.Min.Y,
                    (plane.Normal.Z >= 0) ? self.Max.Z : self.Min.Z
                );

                if (plane.DotCoordinate(positiveVertex) < 0)
                    return false;
            }

            return true;
        }


        public static Bounds3 Transform(this Bounds3 self, Matrix4x4 matrix)
        {
            return self.Points.ComputeBounds(matrix);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(this Bounds3 self, Vector3 point)
        {
            return point.X >= self.Min.X && point.X <= self.Max.X &&
                   point.Y >= self.Min.Y && point.Y <= self.Max.Y &&
                   point.Z >= self.Min.Z && point.Z <= self.Max.Z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceTo(this Bounds3 self, Vector3 point)
        {
            var vec = new Vector3(
                Math.Max(Math.Max(self.Min.X - point.X, 0), point.X - self.Max.X),
                Math.Max(Math.Max(self.Min.Y - point.Y, 0), point.Y - self.Max.Y),
                Math.Max(Math.Max(self.Min.Z - point.Z, 0), point.Z - self.Max.Z)
            );
            return vec.Length();
        }

        public static float DistanceSquaredTo(this Bounds3 self, Vector3 point)
        {
            var vec = new Vector3(
                Math.Max(Math.Max(self.Min.X - point.X, 0), point.X - self.Max.X),
                Math.Max(Math.Max(self.Min.Y - point.Y, 0), point.Y - self.Max.Y),
                Math.Max(Math.Max(self.Min.Z - point.Z, 0), point.Z - self.Max.Z)
            );
            return vec.LengthSquared();
        }

        public static Bounds3 Merge(this Bounds3 self, Bounds3 other)
        {
            return new Bounds3
            {
                Min = Vector3.Min(self.Min, other.Min),
                Max = Vector3.Max(self.Max, other.Max)
            };
        }

        public static float Volume(this Bounds3 self)
        {
            var size = self.Size;
            return size.X * size.Y * size.Z;
        }

        #endregion

        #region POSE

        public static Pose3 Lerp(this Pose3 self, Pose3 other, float otherWeight)
        {
            return new Pose3
            {
                Orientation = Quaternion.Slerp(self.Orientation, other.Orientation, otherWeight),
                Position = Vector3.Lerp(self.Position, other.Position, otherWeight)
            };
        }

        public static bool IsFinite(this Pose3 self)
        {
            return self.Position.IsFinite() && self.Orientation.IsFinite();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pose3 Inverse(this Pose3 self)
        {
            var quat = Quaternion.Inverse(self.Orientation);

            return new Pose3
            {
                Orientation = quat,
                Position = Vector3.Transform(-self.Position, quat)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Transform(this Pose3 self, Vector3 other)
        {
            return self.Position + Vector3.Transform(other, self.Orientation);
        }

        /*
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pose3 Transform(this Pose3 pose, Pose3 other)
        {
            return other.Multiply(pose);
        }
        */

        public static bool IsIdentity(this Pose3 self)
        {
            return self.Position == Vector3.Zero && self.Orientation == Quaternion.Identity;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pose3 Difference(this Pose3 self, Pose3 other)
        {
            return new Pose3
            {
                Orientation = Quaternion.Inverse(self.Orientation) * other.Orientation,
                Position = other.Position - self.Position
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Ray3 ToRay(this Pose3 self)
        {
            var direction = (-Vector3.UnitZ).Transform(self.Orientation);

            var transformedUp = Vector3.UnitY.Transform(self.Orientation);

            // Project the transformed up vector onto the plane perpendicular to the direction
            var projectedUp = transformedUp - transformedUp.Dot(direction) * direction;

            // Calculate the roll angle in radians, using atan2 for signed angle
            var angle = (float)Math.Atan2(Vector3.UnitY.Cross(projectedUp).Dot(direction),
                                          Vector3.UnitY.Dot(projectedUp));

            return new Ray3
            {
                Origin = self.Position,
                Direction = direction,
                Roll = angle
            };
        }

        #endregion

        #region TRIANGLE

        public static bool IsCCW(this Triangle3 self)
        {
            var normal = self.Normal();
            var dot = normal.Dot(Vector3.UnitZ);
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

        public static bool IsFinite(this Vector3 self)
        {
            return float.IsFinite(self.X) && float.IsFinite(self.Y) && float.IsFinite(self.Z);
        }

        public static Vector3 Round(this Vector3 vector, int decimals)
        {
            return new Vector3(
                MathF.Round(vector.X, decimals),
                MathF.Round(vector.Y, decimals),
                MathF.Round(vector.Z, decimals)
            );
        }

        public static Bounds3 ComputeBounds(this IEnumerable<Vector3> self)
        {
            var builder = new Bounds3Builder();
            builder.Add(self);
            return builder.Result;
        }

        public static Bounds3 ComputeBounds(this IEnumerable<Vector3> self, Matrix4x4 matrix)
        {
            var builder = new Bounds3Builder();
            builder.Add(self.Select(a => a.Transform(matrix)));
            return builder.Result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToVector2(this Vector3 self)
        {
            return new Vector2(self.X, self.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ToOrientation(this Vector3 self)
        {
            return (-Vector3.UnitZ).RotationTowards(self);
        }

        public static Quaternion ToOrientation(this Vector3 self, float roll)
        {
            var mainQuat = self.ToOrientation();

            var rollQuaternion = Quaternion.CreateFromAxisAngle(self, roll);

            return rollQuaternion * mainQuat;
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
        public static bool IsSameValue(this Vector3 value, float epsilon = 1e-5f)
        {
            return MathF.Abs(value.X - value.Y) < epsilon &&
                   MathF.Abs(value.X - value.Z) < epsilon &&
                   MathF.Abs(value.Y - value.Z) < epsilon;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSimilar(this Vector3 self, Vector3 other, float epsilon = 1e-5f)
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
        public static Vector3 Transform(this Vector3 self, Quaternion quat)
        {
            return Vector3.Transform(self, quat);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DotNormal(this Vector3 self, Vector3 other)
        {
            return Math.Clamp(Vector3.Dot(self, other), -1f, 1f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(this Vector3 self, Vector3 other)
        {
            return Vector3.Dot(self, other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Cross(this Vector3 self, Vector3 other)
        {
            return Vector3.Cross(self, other);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Normalize(this Vector3 self)
        {
            return Vector3.Normalize(self);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToDirection(this Vector3 self, Matrix4x4 matrix)
        {
            return new Vector3(
                  self.X * matrix.M11 + self.Y * matrix.M21 + self.Z * matrix.M31,
                  self.X * matrix.M12 + self.Y * matrix.M22 + self.Z * matrix.M32,
                  self.X * matrix.M13 + self.Y * matrix.M23 + self.Z * matrix.M33
              ).Normalize();
        }

        public static Quaternion RotationTowards(this Vector3 from, Vector3 to, float epsilon = EPSILON)
        {
            return from.RotationTowards(to, Vector3.UnitY, epsilon);
        }

        public static Quaternion RotationTowards(this Vector3 from, Vector3 to, Vector3 referenceAxis, float epsilon = EPSILON)
        {
            from = Vector3.Normalize(from);
            to = Vector3.Normalize(to);

            float angle;
            Vector3 rotationAxis;

            // Compute the dot product to find the cosine of the angle between the vectors
            var dot = from.DotNormal(to);

            // Handle the case where the vectors are already aligned
            if (MathF.Abs(dot - 1.0f) < epsilon)
                return Quaternion.Identity;

            // Handle the case where the vectors are opposite (180-degree rotation)
            if (MathF.Abs(dot + 1.0f) < epsilon)
            {
                // Find an orthogonal vector to use as the rotation axis
                rotationAxis = Vector3.Cross(from, referenceAxis);
                if (rotationAxis.LengthSquared() < epsilon)
                {
                    referenceAxis = Vector3.UnitX;
                    rotationAxis = Vector3.Cross(from, referenceAxis); // Try a different axis if the first fails
                }
                angle = MathF.PI;
            }
            else
            {
                rotationAxis = Vector3.Cross(from, to);
                angle = MathF.Acos(dot);
            }


            return Quaternion.CreateFromAxisAngle(rotationAxis.Normalize(), angle);
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
            var distance = plane.DotCoordinate(pos);
            return pos - distance * plane.Normal;
        }

        public static float SignedAngleWith(this Vector3 self, Vector3 other, Vector3 planeNormal)
        {
            self = Vector3.Normalize(self);
            other = Vector3.Normalize(other);
            var cross = Vector3.Cross(self, other);
            var dot = self.DotNormal(other);
            var angle = MathF.Atan2(cross.Length(), dot);
            var sign = MathF.Sign(cross.Dot(planeNormal));
            return angle * sign;
        }

        public static float AngleWith(this Vector3 self, Vector3 other)
        {
            var dot = self.Normalize().DotNormal(other.Normalize());
            return MathF.Acos(dot);
        }


        #endregion

        #region RAY 

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        public static Vector3? Intersects(this Ray3 self, Sphere sphere, out float distance, float epsilon = EPSILON)
        {
            var oc = self.Origin - sphere.Center;

            float a = Vector3.Dot(self.Direction, self.Direction);
            float b = 2.0f * Vector3.Dot(oc, self.Direction);
            float c = Vector3.Dot(oc, oc) - sphere.Radius * sphere.Radius;

            float discriminant = b * b - 4 * a * c;

            if (discriminant < 0)
            {
                distance = 0;
                return null;
            }

            // Calculate the two possible solutions for t
            float sqrtDiscriminant = (float)Math.Sqrt(discriminant);
            float t1 = (-b - sqrtDiscriminant) / (2 * a);
            float t2 = (-b + sqrtDiscriminant) / (2 * a);

            // Choose the smallest positive t as the intersection point

            distance = (t1 >= 0) ? t1 : t2;

            return distance >= 0 ? self.PointAt(distance) : null;
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

        public static Pose3 ToPose(this Ray3 self)
        {
            return new Pose3
            {
                Position = self.Origin,
                Orientation = self.Roll == 0 ? self.Direction.ToOrientation() : self.Direction.ToOrientation(self.Roll)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Plane ToPlane(this Ray3 self)
        {
            return new Plane(self.Direction, -(self.Direction.Dot(self.Origin)));
        }

        #endregion

        #region QUATERNION

        public static bool IsFinite(this Quaternion self)
        {
            return float.IsFinite(self.X) && float.IsFinite(self.Y) && float.IsFinite(self.Z) && float.IsFinite(self.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Opposite(this Quaternion self)
        {
            return new Quaternion(-self.X, -self.Y, -self.Z, self.W);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Subtract(this Quaternion self, Quaternion other)
        {
            return self * Quaternion.Inverse(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion AddDelta(this Quaternion self, Quaternion delta)
        {
            return delta * self;
        }

        public static bool IsSimilar(this Quaternion self, Quaternion other, float epsilon = 1e-5f)
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

        public static void AxisAndAngle(this Quaternion self, out Vector3 axis, out float angle)
        {
            angle = 2.0f * (float)Math.Acos(self.W);
            axis = new Vector3(self.X, self.Y, self.Z).Normalize();
        }

        public static float AngleAmongAxis(this Quaternion self, Vector3 axis, Vector3 normal)
        {
            self.AxisAndAngle(out var quatAxis, out _);

            var projection = quatAxis.Dot(axis);

            var angle = MathF.Acos(projection);

            var crossProduct = Vector3.Cross(quatAxis, axis);

            var sign = MathF.Sign(crossProduct.Dot(normal));

            return angle * sign;
        }

        public static Vector3 Right(this Quaternion q)
        {
            return new Vector3(
                1 - 2 * (q.Y * q.Y + q.Z * q.Z),
                2 * (q.X * q.Y + q.W * q.Z),
                2 * (q.X * q.Z - q.W * q.Y)
            );
        }

        public static Vector3 Up(this Quaternion q)
        {
            return new Vector3(
                2 * (q.X * q.Y - q.W * q.Z),
                1 - 2 * (q.X * q.X + q.Z * q.Z),
                2 * (q.Y * q.Z + q.W * q.X)
            );
        }

        public static Vector3 Forward(this Quaternion q)
        {
            return - new Vector3(
                2 * (q.X * q.Z + q.W * q.Y),
                2 * (q.Y * q.Z - q.W * q.X),
                1 - 2 * (q.X * q.X + q.Y * q.Y)
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
        public static Vector3 Center(this Line3 self)
        {
            return (self.From + self.To) / 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Line3 Reverse(this Line3 self)
        {
            return new Line3(self.To, self.From);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Line3 Expand(this Line3 self, float fromDelta, float toDelta)
        {
            return new Line3(self.PointAt(-fromDelta), self.PointAt(self.Length() + toDelta));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Line3 Transform(this Line3 self, Matrix4x4 matrix)
        {
            return new Line3(self.From.Transform(matrix), self.To.Transform(matrix));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Line3 Transform(this Line3 self, Quaternion quat)
        {
            return new Line3(Vector3.Transform(self.From, quat), Vector3.Transform(self.To, quat));
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToVector3(this Vector2 sel, float z = 0)
        {
            return new Vector3(sel.X, sel.Y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cross(this Vector2 a, Vector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        #endregion

        #region POLY2

        public static Vector2[] Transform(this Vector2[] self, Matrix3x2 matrix)
        {
            var res = new Vector2[self.Length];
            for (var i = 0; i < self.Length; i++)
                res[i] = Vector2.Transform(self[i], matrix);
            return res;
        }

        public static Bounds2 Bounds(this IList<Vector2> self)
        {
            if (self.Count == 0)
                return new Bounds2();

            var result = new Bounds2
            {
                Min = self[0],
                Max = self[0]
            };

            foreach (var point in self.Skip(1))
            {
                result.Min = Vector2.Min(result.Min, point);
                result.Max = Vector2.Max(result.Max, point);
            }

            return result;
        }

        public static Bounds2 Bounds(this Poly2 self)
        {
            return self.Points.Bounds();
        }

        public static void EnsureCCW(this Poly2 self)
        {
            if (self.SignedArea() < 0)
                Array.Reverse(self.Points);
        }

        public static float Length(this Poly2 self)
        {
            var length = 0f;

            for (int i = 0; i < self.Points.Length - 1; i++)
                length += Vector2.Distance(self.Points[i], self.Points[i + 1]);

            if (self.IsClosed)
                length += Vector2.Distance(self.Points[^1], self.Points[0]);

            return length;
        }

        public static float SignedArea(this Poly2 self)
        {
            float area = 0;
            for (var i = 0; i < self.Points.Length; i++)
            {
                var current = self.Points[i];
                var next = self.Points[(i + 1) % self.Points.Length];
                area += (current.X * next.Y - next.X * current.Y);
            }
            return area * 0.5f;
        }

        #endregion

        #region MISC

        public static Vector2 ToVector2(this Size2I self)
        {
            return new Vector2(self.Width, self.Height);
        }

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
