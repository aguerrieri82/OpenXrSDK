using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace XrMath
{

    public static class MathExtensions
    {
        public const float EPSILON = 1e-6f;

        #region MATRIX4X4

        extension(in Matrix4x4 self)
        {
            public float[] ToFloatArray()
            {
                return
                [
                    self.M11, self.M12, self.M13, self.M14,
                    self.M21, self.M22, self.M23, self.M24,
                    self.M31, self.M32, self.M33, self.M34,
                    self.M41, self.M42, self.M43, self.M44
                ];
            }

            public bool DecomposeDouble(out Vector3 scale, out Quaternion rotation, out Vector3 translation)
            {
                return Matrix4x4.Decompose(self, out scale, out rotation, out translation);
            }

            public bool IsValid()
            {
                for (var i = 0; i < 16; i++)
                {
                    var value = self[i / 4, i % 4];
                    if (float.IsNaN(value) || float.IsInfinity(value))
                        return false;
                }
                return true;
            }

            public Matrix4x4 Invert()
            {
                Matrix4x4.Invert(self, out var result);
                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Pose3 ToPose()
            {
                if (!Matrix4x4.Decompose(self, out var scale, out var orientation, out var translation))
                    throw new InvalidOperationException("Matrix cannot be decomposed into scale, rotation, and translation.");

                return new Pose3
                {
                    Orientation = orientation,
                    Position = translation
                };
            }

            public unsafe Matrix4x4 InvertRigidBody()
            {
                var result = stackalloc float[16];
                var src = self;
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

            public Matrix4x4 InterpolateWorldMatrix(Matrix4x4 matrix2, float t)
            {
                // Extract position vectors
                var position1 = new Vector3(self.M41, self.M42, self.M43);
                var position2 = new Vector3(matrix2.M41, matrix2.M42, matrix2.M43);

                // Interpolate position
                var interpolatedPosition = Vector3.Lerp(position1, position2, t);

                // Extract rotation quaternions
                var rotation1 = Quaternion.CreateFromRotationMatrix(self);
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
        }

        #endregion

        #region QUAD3

        extension(Quad3 self)
        {
            public IEnumerable<Vector3> Corners()
            {
                yield return self.PointAt(0, 0);
                yield return self.PointAt(self.Size.X, 0);
                yield return self.PointAt(self.Size.X, self.Size.Y);
                yield return self.PointAt(0, self.Size.Y);
            }
        }

        extension(in Quad3 self)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 Normal()
            {
                return Vector3.Transform(Vector3.UnitZ, self.Pose.Orientation).Normalize();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 Tangent()
            {
                return Vector3.Transform(Vector3.UnitX, self.Pose.Orientation).Normalize();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Plane ToPlane()
            {
                var normal = self.Normal();
                return new Plane(normal, -Vector3.Dot(normal, self.Pose.Position));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 PointAt(float x, float y)
            {
                return self.PointAt(new Vector2(x, y));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 PointAt(Vector2 point)
            {
                point -= self.Size / 2;

                return self.Pose.Transform(new Vector3(point.X, point.Y, 0));
            }

            public Vector3 Center()
            {
                var sum = Vector3.Zero;
                foreach (var item in self.Corners())
                    sum += item;
                return sum / 4;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector2 LocalPointAt(Vector3 worldPoint)
            {
                var local = self.Pose.Inverse().Transform(worldPoint);
                return new Vector2(local.X, local.Y) + self.Size / 2;
            }
        }

        #endregion

        #region PLANE

        extension(in Plane self)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Plane Normalize()
            {
                return Plane.Normalize(self);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector4 ToVector4()
            {
                return new Vector4(self.Normal, self.D);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 Project(Vector3 point)
            {
                return point - self.Distance(point) * self.Normal;
            }

            public void OrthogonalAxis(out Vector3 uAxis, out Vector3 vAxis)
            {
                var arbitrary = Math.Abs(self.Normal.X) > Math.Abs(self.Normal.Z)
                         ? new Vector3(-self.Normal.Y, self.Normal.X, 0)
                         : new Vector3(0, -self.Normal.Z, self.Normal.Y);

                uAxis = Vector3.Normalize(Vector3.Cross(arbitrary, self.Normal));
                vAxis = Vector3.Normalize(Vector3.Cross(self.Normal, uAxis));
            }

            public Vector2 ProjectUV(in Vector3 point)
            {
                self.OrthogonalAxis(out var uAxis, out var vAxis);
                return self.ProjectUV(point, uAxis, vAxis);
            }

            public Vector2 ProjectUV(in Vector3 point, in Vector3 uAxis, in Vector3 vAxis)
            {
                var projectedPoint = self.Project(point);

                var x = Vector3.Dot(projectedPoint, uAxis);
                var y = Vector3.Dot(projectedPoint, vAxis);

                return new Vector2(x, y);
            }

            public Vector3 UnprojectUV(in Vector2 point)
            {
                self.OrthogonalAxis(out var uAxis, out var vAxis);
                return UnprojectUV(self, point, uAxis, vAxis);
            }

            public Vector3 UnprojectUV(in Vector2 point, in Vector3 uAxis, in Vector3 vAxis)
            {
                var planePoint = self.Project(Vector3.Zero);

                var pointInPlane = planePoint + point.X * uAxis + point.Y * vAxis;

                return pointInPlane;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Distance(in Vector3 point)
            {
                return self.Normal.Dot(point) + self.D;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float DotCoordinate(in Vector3 point)
            {
                return Plane.Dot(self, Vector4.Create(point, 1f));
            }

            public bool Intersects(in Line3 line, out Vector3 point)
            {
                point = Vector3.Zero;

                var direction = line.To - line.From;
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
        }

        #endregion

        #region BOUNDS3

        extension(in Bounds3 self)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsSimilar(in Bounds3 other, float epsilon = 1e-5f)
            {
                return self.Min.IsSimilar(other.Min) &&
                       self.Max.IsSimilar(other.Max);
            }

            public CubeFaces Faces()
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

            public bool IntersectFrustum(in ReadOnlySpan<Plane> planes)
            {
                if (planes.Length == 12)
                {
                    return self.IntersectFrustum(planes.Slice(0, 6)) ||
                           self.IntersectFrustum(planes.Slice(6, 6));
                }

                for (var i = 0; i < planes.Length; i++)
                {
                    var plane = planes[i];

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

            public Bounds3 Transform(in Matrix4x4 matrix)
            {
                return self.Points.ComputeBounds(matrix);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Contains(in Vector3 point)
            {
                return point.X >= self.Min.X && point.X <= self.Max.X &&
                       point.Y >= self.Min.Y && point.Y <= self.Max.Y &&
                       point.Z >= self.Min.Z && point.Z <= self.Max.Z;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Inside(in Bounds3 other)
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
            public bool Intersects(in Bounds3 other)
            {
                if (self.Max.X < other.Min.X || self.Min.X > other.Max.X)
                    return false;
                if (self.Max.Y < other.Min.Y || self.Min.Y > other.Max.Y)
                    return false;
                if (self.Max.Z < other.Min.Z || self.Min.Z > other.Max.Z)
                    return false;

                return true;
            }

            public bool Intersects(in Bounds3 other, out Bounds3 result)
            {
                var intersectMinX = Math.Max(self.Min.X, other.Min.X);
                var intersectMaxX = Math.Min(self.Max.X, other.Max.X);

                var intersectMinY = Math.Max(self.Min.Y, other.Min.Y);
                var intersectMaxY = Math.Min(self.Max.Y, other.Max.Y);

                var intersectMinZ = Math.Max(self.Min.Z, other.Min.Z);
                var intersectMaxZ = Math.Min(self.Max.Z, other.Max.Z);

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

            public bool Intersects(in Line3 line, out float distance)
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
            public float DistanceTo(in Vector3 point)
            {
                var vec = new Vector3(
                    Math.Max(Math.Max(self.Min.X - point.X, 0), point.X - self.Max.X),
                    Math.Max(Math.Max(self.Min.Y - point.Y, 0), point.Y - self.Max.Y),
                    Math.Max(Math.Max(self.Min.Z - point.Z, 0), point.Z - self.Max.Z)
                );
                return vec.Length();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float DistanceSquaredTo(in Vector3 point)
            {
                var vec = new Vector3(
                    Math.Max(Math.Max(self.Min.X - point.X, 0), point.X - self.Max.X),
                    Math.Max(Math.Max(self.Min.Y - point.Y, 0), point.Y - self.Max.Y),
                    Math.Max(Math.Max(self.Min.Z - point.Z, 0), point.Z - self.Max.Z)
                );
                return vec.LengthSquared();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Bounds3 Merge(in Bounds3 other)
            {
                return new Bounds3
                {
                    Min = Vector3.Min(self.Min, other.Min),
                    Max = Vector3.Max(self.Max, other.Max)
                };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Volume()
            {
                var size = self.Size;
                return size.X * size.Y * size.Z;
            }
        }

        #endregion

        #region POSE3

        extension(in Pose3 self)
        {
            public Pose3 Lerp(in Pose3 other, float otherWeight)
            {
                return new Pose3
                {
                    Orientation = Quaternion.Slerp(self.Orientation, other.Orientation, otherWeight),
                    Position = Vector3.Lerp(self.Position, other.Position, otherWeight)
                };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsFinite()
            {
                return self.Position.IsFinite() && self.Orientation.IsFinite();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsSimilar(in Pose3 other, float epsilon = EPSILON)
            {
                return self.Position.IsSimilar(other.Position, epsilon) &&
                       self.Orientation.IsSimilar(other.Orientation, epsilon);
            }

            public Matrix4x4 ToMatrix()
            {
                return Matrix4x4.CreateFromQuaternion(self.Orientation) *
                       Matrix4x4.CreateTranslation(self.Position);
            }

            public Matrix4x4 ToMatrix(in Vector3 scale)
            {
                return Matrix4x4.CreateScale(scale) *
                       Matrix4x4.CreateFromQuaternion(self.Orientation) *
                       Matrix4x4.CreateTranslation(self.Position);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Pose3 Inverse()
            {
                var quat = Quaternion.Inverse(self.Orientation);

                return new Pose3
                {
                    Orientation = quat,
                    Position = Vector3.Transform(-self.Position, quat)
                };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 Transform(in Vector3 other)
            {
                return self.Position + Vector3.Transform(other, self.Orientation);
            }

            public bool IsIdentity()
            {
                return self.Position == Vector3.Zero && self.Orientation == Quaternion.Identity;
            }

            /// <summary>
            /// Composes this pose with another pose.
            /// </summary>
            /// <remarks>
            /// The composition order is:
            /// <code>
            /// result = self * other
            /// </code>
            ///
            /// This means <paramref name="other"/> is applied first, then <paramref name="self"/>.
            ///
            /// For hierarchical transforms:
            /// <code>
            /// childWorldPose = parentWorldPose.Multiply(childLocalPose);
            /// </code>
            ///
            /// So the left operand is the outer/parent transform, and the right operand is
            /// the inner/local transform.
            /// </remarks>
            /// <param name="self">The outer/parent pose.</param>
            /// <param name="other">The inner/local pose.</param>
            /// <returns>The composed pose.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Pose3 Multiply(in Pose3 other)
            {
                return new Pose3
                {
                    Orientation = self.Orientation * other.Orientation,
                    Position = self.Transform(other.Position)
                };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Pose3 Add(in Pose3 delta)
            {
                return new Pose3
                {
                    Orientation = self.Orientation * delta.Orientation,
                    Position = self.Position + delta.Position
                };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Pose3 Difference(in Pose3 other)
            {
                return self.Inverse().Multiply(other);
            }

            public string ToCodeString()
            {
                var p = self.Position;
                var q = self.Orientation;
                var c = CultureInfo.InvariantCulture;

                // Formats to 9 significant digits (G9) to maintain precision
                var posX = p.X.ToString("G9", c);
                var posY = p.Y.ToString("G9", c);
                var posZ = p.Z.ToString("G9", c);

                var rotX = q.X.ToString("G9", c);
                var rotY = q.Y.ToString("G9", c);
                var rotZ = q.Z.ToString("G9", c);
                var rotW = q.W.ToString("G9", c);

                return $@"new Pose3
                {{
                    Position = new Vector3({posX}f, {posY}f, {posZ}f),
                    Orientation = new Quaternion({rotX}f, {rotY}f, {rotZ}f, {rotW}f)
                }}";
            }

            public Ray3 ToRay()
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
        }

        #endregion

        #region TRIANGLE3

        extension(in Triangle3 self)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsCCW()
            {
                var normal = self.Normal();
                var dot = normal.Dot(Vector3.UnitZ);
                return dot > 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 Normal()
            {
                var edge1 = self.V1 - self.V0;
                var edge2 = self.V2 - self.V0;
                var normal = Vector3.Cross(edge1, edge2);
                return Vector3.Normalize(normal);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 Center()
            {
                return (self.V0 + self.V1 + self.V2) / 3.0f;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Triangle3 Transform(Matrix4x4 matrix)
            {
                return new Triangle3
                {
                    V0 = self.V0.Transform(matrix),
                    V1 = self.V1.Transform(matrix),
                    V2 = self.V2.Transform(matrix),
                };
            }
        }

        #endregion

        #region VECTOR3

        extension(in Vector3 self)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3I ToRoundI()
            {
                return new Vector3I(
                    (int)MathF.Round(self.X),
                    (int)MathF.Round(self.Y),
                    (int)MathF.Round(self.Z)
                );
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsFinite()
            {
                return float.IsFinite(self.X) && float.IsFinite(self.Y) && float.IsFinite(self.Z);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 Round(int decimals)
            {
                return new Vector3(
                    MathF.Round(self.X, decimals),
                    MathF.Round(self.Y, decimals),
                    MathF.Round(self.Z, decimals)
                );
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector2 ToVector2()
            {
                return new Vector2(self.X, self.Y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Quaternion ToOrientation()
            {
                return (-Vector3.UnitZ).RotationTowards(self);
            }

            public Quaternion ToOrientation(float roll)
            {
                var mainQuat = self.ToOrientation();

                var rollQuaternion = Quaternion.CreateFromAxisAngle(self, roll);

                return rollQuaternion * mainQuat;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsSameValue(float epsilon = 1e-5f)
            {
                return MathF.Abs(self.X - self.Y) < epsilon &&
                       MathF.Abs(self.X - self.Z) < epsilon &&
                       MathF.Abs(self.Y - self.Z) < epsilon;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsSimilar(in Vector3 other, float epsilon = 1e-5f)
            {
                return MathF.Abs(self.X - other.X) < epsilon &&
                       MathF.Abs(self.Y - other.Y) < epsilon &&
                       MathF.Abs(self.Z - other.Z) < epsilon;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 Transform(in Matrix4x4 matrix)
            {
                return Vector3.Transform(self, matrix);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 Transform(in Quaternion quat)
            {
                return Vector3.Transform(self, quat);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float DotNormal(in Vector3 other)
            {
                return Math.Clamp(Vector3.Dot(self, other), -1f, 1f);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Dot(in Vector3 other)
            {
                return Vector3.Dot(self, other);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 Cross(in Vector3 other)
            {
                return Vector3.Cross(self, other);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 Normalize()
            {
                return Vector3.Normalize(self);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 ToDirection(in Matrix4x4 matrix)
            {
                return new Vector3(
                      self.X * matrix.M11 + self.Y * matrix.M21 + self.Z * matrix.M31,
                      self.X * matrix.M12 + self.Y * matrix.M22 + self.Z * matrix.M32,
                      self.X * matrix.M13 + self.Y * matrix.M23 + self.Z * matrix.M33
                  ).Normalize();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Quaternion RotationTowards(in Vector3 to, float epsilon = EPSILON)
            {
                return self.RotationTowards(to, Vector3.UnitY, epsilon);
            }

            public Quaternion RotationTowards(Vector3 to, Vector3 referenceAxis, float epsilon = EPSILON)
            {
                var from = Vector3.Normalize(self);
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
            public Vector3 Project(in Matrix4x4 matrix)
            {
                var worldPoint = Vector4.Transform(new Vector4(self, 1), matrix);
                return new Vector3(worldPoint.X, worldPoint.Y, worldPoint.Z) / worldPoint.W;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 Project(in Plane plane)
            {
                var distance = plane.DotCoordinate(self);
                return self - distance * plane.Normal;
            }

            public float SignedAngleWith(Vector3 other, in Vector3 planeNormal)
            {
                var from = Vector3.Normalize(self);
                other = Vector3.Normalize(other);
                var cross = Vector3.Cross(from, other);
                var dot = from.DotNormal(other);
                var angle = MathF.Atan2(cross.Length(), dot);
                var sign = MathF.Sign(cross.Dot(planeNormal));
                return angle * sign;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float AngleWith(in Vector3 other)
            {
                var dot = self.Normalize().DotNormal(other.Normalize());
                return MathF.Acos(dot);
            }
        }

        #endregion

        #region IENUMERABLE<VECTOR3>

        extension(IEnumerable<Vector3> self)
        {
            public Bounds3 ComputeBounds()
            {
                var builder = new Bounds3Builder();
                builder.Add(self);
                return builder.Result;
            }

            public Bounds3 ComputeBounds(Matrix4x4 matrix)
            {
                var builder = new Bounds3Builder();
                builder.Add(self.Select(a => a.Transform(matrix)));
                return builder.Result;
            }
        }

        #endregion

        #region VECTOR3[]

        extension(Vector3[] self)
        {
            public float MinDistanceTo(in Vector3 point)
            {
                var result = float.PositiveInfinity;

                for (var i = 0; i < self.Length; i++)
                {
                    var d = Vector3.Distance(point, self[i]);
                    result = MathF.Min(result, d);
                }

                return result;
            }
        }

        #endregion

        #region RAY3

        extension(in Ray3 self)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Line3 ToLine(float len)
            {
                return new Line3()
                {
                    From = self.Origin,
                    To = self.PointAt(len)
                };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 PointAt(float distance)
            {
                return self.Origin + self.Direction * distance;
            }

            public Vector3? Intersects(Sphere sphere, out float distance, float epsilon = EPSILON)
            {
                var oc = self.Origin - sphere.Center;

                var a = Vector3.Dot(self.Direction, self.Direction);
                var b = 2.0f * Vector3.Dot(oc, self.Direction);
                var c = Vector3.Dot(oc, oc) - sphere.Radius * sphere.Radius;

                var discriminant = b * b - 4 * a * c;

                if (discriminant < 0)
                {
                    distance = 0;
                    return null;
                }

                // Calculate the two possible solutions for t
                var sqrtDiscriminant = (float)Math.Sqrt(discriminant);
                var t1 = (-b - sqrtDiscriminant) / (2 * a);
                var t2 = (-b + sqrtDiscriminant) / (2 * a);

                // Choose the smallest positive t as the intersection point

                distance = (t1 >= 0) ? t1 : t2;

                return distance >= 0 ? self.PointAt(distance) : null;
            }

            public Vector3? Intersects(in Triangle3 triangle, out float distance, float epsilon = EPSILON)
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

            public bool Intersects(in Quad3 quad, out Vector3 intersectionPoint, float epsilon = EPSILON)
            {
                if (!self.Intersects(quad.ToPlane(), out intersectionPoint, epsilon))
                    return false;

                var local = quad.LocalPointAt(intersectionPoint);

                return local.InRange(Vector2.Zero, quad.Size);
            }

            public bool Intersects(in Plane plane, out Vector3 intersectionPoint, float epsilon = EPSILON)
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
            public Ray3 Transform(in Matrix4x4 matrix)
            {
                var v0 = Vector3.Transform(self.Origin, matrix);
                var v1 = Vector3.Transform(self.Origin + self.Direction, matrix);

                return new Ray3
                {
                    Origin = v0,
                    Direction = Vector3.Normalize(v1 - v0)
                };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Ray3 Transform(in Pose3 pose)
            {
                return new Ray3
                {
                    Origin = pose.Transform(self.Origin),
                    Direction = self.Direction.Transform(pose.Orientation).Normalize()
                };
            }

            public Pose3 ToPose()
            {
                return new Pose3
                {
                    Position = self.Origin,
                    Orientation = self.Roll == 0 ? self.Direction.ToOrientation() : self.Direction.ToOrientation(self.Roll)
                };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Plane ToPlane()
            {
                return new Plane(self.Direction, -(self.Direction.Dot(self.Origin)));
            }
        }

        #endregion

        #region QUATERNION

        extension(in Quaternion self)
        {
            public bool IsFinite()
            {
                return float.IsFinite(self.X) && float.IsFinite(self.Y) && float.IsFinite(self.Z) && float.IsFinite(self.W);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Quaternion Opposite()
            {
                return new Quaternion(-self.X, -self.Y, -self.Z, self.W);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Quaternion Subtract(Quaternion other)
            {
                return self * Quaternion.Inverse(other);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Quaternion AddDelta(Quaternion delta)
            {
                return delta * self;
            }

            public bool IsSimilar(Quaternion other, float epsilon = 1e-5f)
            {
                return MathF.Abs(self.X - other.X) < epsilon &&
                    MathF.Abs(self.Y - other.Y) < epsilon &&
                    MathF.Abs(self.Z - other.Z) < epsilon &&
                    MathF.Abs(self.W - other.W) < epsilon;
            }

            public Vector3 ToEuler()
            {
                Vector3 res;

                var q = Quaternion.Normalize(self);

                var sinp = -2.0f * (q.X * q.Z - q.W * q.Y);
                sinp = Math.Clamp(sinp, -1.0f, 1.0f);

                res.X = MathF.Atan2(2.0f * (q.Y * q.Z + q.W * q.X), q.W * q.W - q.X * q.X - q.Y * q.Y + q.Z * q.Z);
                res.Y = MathF.Asin(sinp);
                res.Z = MathF.Atan2(2.0f * (q.X * q.Y + q.W * q.Z), q.W * q.W + q.X * q.X - q.Y * q.Y - q.Z * q.Z);

                return res;
            }

            public Matrix3x3 ToMatrix3x3()
            {
                // Extract individual components of the quaternion
                var x = self.X;
                var y = self.Y;
                var z = self.Z;
                var w = self.W;

                // Calculate matrix elements
                var xx = x * x;
                var xy = x * y;
                var xz = x * z;
                var xw = x * w;

                var yy = y * y;
                var yz = y * z;
                var yw = y * w;

                var zz = z * z;
                var zw = z * w;

                // Construct the rotation matrix
                return new Matrix3x3(
                    1 - 2 * (yy + zz), 2 * (xy - zw), 2 * (xz + yw),
                    2 * (xy + zw), 1 - 2 * (xx + zz), 2 * (yz - xw),
                    2 * (xz - yw), 2 * (yz + xw), 1 - 2 * (xx + yy)
                );
            }

            public void AxisAndAngle(out Vector3 axis, out float angle)
            {
                angle = 2.0f * (float)Math.Acos(self.W);
                axis = new Vector3(self.X, self.Y, self.Z).Normalize();
            }

            public float AngleAmongAxis(Vector3 axis, Vector3 normal)
            {
                self.AxisAndAngle(out var quatAxis, out _);

                var projection = quatAxis.Dot(axis);

                var angle = MathF.Acos(projection);

                var crossProduct = Vector3.Cross(quatAxis, axis);

                var sign = MathF.Sign(crossProduct.Dot(normal));

                return angle * sign;
            }

            public Vector3 Right()
            {
                return new Vector3(
                    1 - 2 * (self.Y * self.Y + self.Z * self.Z),
                    2 * (self.X * self.Y + self.W * self.Z),
                    2 * (self.X * self.Z - self.W * self.Y)
                );
            }

            public Vector3 Up()
            {
                return new Vector3(
                    2 * (self.X * self.Y - self.W * self.Z),
                    1 - 2 * (self.X * self.X + self.Z * self.Z),
                    2 * (self.Y * self.Z + self.W * self.X)
                );
            }

            public Vector3 Forward()
            {
                return -new Vector3(
                    2 * (self.X * self.Z + self.W * self.Y),
                    2 * (self.Y * self.Z - self.W * self.X),
                    1 - 2 * (self.X * self.X + self.Y * self.Y)
                );
            }

            public Quaternion KeepYawOnly()
            {
                var q = Quaternion.Normalize(self);

                var siny_cosp = 2f * (q.W * q.Y + q.X * q.Z);
                var cosy_cosp = 1f - 2f * (q.Y * q.Y + q.Z * q.Z);
                var yaw = MathF.Atan2(siny_cosp, cosy_cosp);

                return Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw);
            }
        }

        #endregion

        #region COLOR

        extension(in Color self)
        {
            public Color Multiply(float rgbFactor, float aFactor = 1)
            {
                return new Color(self.R * rgbFactor, self.G * rgbFactor, self.B * rgbFactor, self.A * aFactor);
            }

            public Vector3 ToVector3()
            {
                return new Vector3(self.R, self.G, self.B);
            }

            public Vector4 ToVector4()
            {
                return new Vector4(self.R, self.G, self.B, self.A);
            }

            public string ToHex()
            {
                static string ToHex(float value)
                {
                    var iVal = (int)Math.Max(0, Math.Min(255, value * 255));
                    return iVal.ToString("X").PadLeft(2, '0');
                }

                return $"#{ToHex(self.R)}{ToHex(self.G)}{ToHex(self.B)}{ToHex(self.A)}";
            }

            public string ToHexArgb()
            {
                static string ToHex(float value)
                {
                    var iVal = (int)Math.Max(0, Math.Min(255, value * 255));
                    return iVal.ToString("X").PadLeft(2, '0');
                }

                return $"#{ToHex(self.A)}{ToHex(self.R)}{ToHex(self.G)}{ToHex(self.B)}";
            }
        }

        #endregion

        #region LINE3

        extension(in Line3 self)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 Direction()
            {
                return (self.To - self.From).Normalize();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Length()
            {
                return Vector3.Distance(self.From, self.To);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 Center()
            {
                return (self.From + self.To) / 2;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Line3 Reverse()
            {
                return new Line3(self.To, self.From);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Line3 Expand(float fromDelta, float toDelta)
            {
                return new Line3(self.PointAt(-fromDelta), self.PointAt(self.Length() + toDelta));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Line3 Transform(Matrix4x4 matrix)
            {
                return new Line3(self.From.Transform(matrix), self.To.Transform(matrix));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Line3 Transform(Quaternion quat)
            {
                return new Line3(Vector3.Transform(self.From, quat), Vector3.Transform(self.To, quat));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 PointAt(float distance)
            {
                return self.From + self.Direction() * distance;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 PointAtOffset(float t)
            {
                return self.From + self.Direction() * (t * self.Length());
            }
        }

        #endregion

        #region VECTOR2

        extension(in Vector2 self)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool InRange(in Vector2 min, in Vector2 max)
            {
                return self.X >= min.X && self.X <= max.X &&
                       self.Y >= min.Y && self.Y <= max.Y;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsSimilar(in Vector2 other, float epsilon)
            {
                return MathF.Abs(self.X - other.X) < epsilon &&
                       MathF.Abs(self.Y - other.Y) < epsilon;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 ToVector3(float z = 0)
            {
                return new Vector3(self.X, self.Y, z);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Cross(in Vector2 b)
            {
                return self.X * b.Y - self.Y * b.X;
            }
        }

        #endregion

        #region VECTOR2[]

        extension(Vector2[] self)
        {
            public Vector2[] Transform(in Matrix3x2 matrix)
            {
                var res = new Vector2[self.Length];
                for (var i = 0; i < self.Length; i++)
                    res[i] = Vector2.Transform(self[i], matrix);
                return res;
            }
        }

        #endregion

        #region ILIST<VECTOR2>

        extension(IList<Vector2> self)
        {
            public Bounds2 Bounds()
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
        }

        #endregion

        #region POLY2

        extension(in Poly2 self)
        {
            public Bounds2 Bounds()
            {
                return self.Points.Bounds();
            }

            public void EnsureCCW()
            {
                if (self.SignedArea() < 0)
                    Array.Reverse(self.Points);
            }

            public void EnsureCW()
            {
                if (self.SignedArea() > 0)
                    Array.Reverse(self.Points);
            }

            public float Length()
            {
                var length = 0f;

                for (var i = 0; i < self.Points.Length - 1; i++)
                    length += Vector2.Distance(self.Points[i], self.Points[i + 1]);

                if (self.IsClosed)
                    length += Vector2.Distance(self.Points[^1], self.Points[0]);

                return length;
            }

            public float SignedArea()
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
        }

        #endregion

        #region BOUNDS2

        extension(in Bounds2 self)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Contains(in Vector2 point)
            {
                return point.X >= self.Min.X && point.X <= self.Max.X &&
                       point.Y >= self.Min.Y && point.Y <= self.Max.Y;
            }

            public Bounds2 Scale(float factor)
            {
                var halfSize = (self.Size * factor) / 2f;

                return new Bounds2
                {
                    Min = self.Center - halfSize,
                    Max = self.Center + halfSize
                };
            }

            public Rect2I ToRect2I()
            {
                return new Rect2I((int)self.Min.X, (int)self.Min.Y, (uint)self.Size.X, (uint)self.Size.Y);
            }

            public Rect2I ToRect2I(int padding)
            {
                return new Rect2I((int)self.Min.X - padding, (int)self.Min.Y - padding,
                    (uint)(self.Size.X + (padding * 2)), (uint)(self.Size.Y + (padding * 2)));
            }
        }

        #endregion

        #region RECT2

        extension(in Rect2 self)
        {
            public Rect2 Scale(float value)
            {
                return Scale(self, value, value);
            }

            public Rect2 Scale(float x, float y)
            {
                return new Rect2(self.X * x, self.Y * y, self.Width * x, self.Height * y);
            }

            public Rect2 Translate(float x, float y)
            {
                return new Rect2(self.X + x, self.Y + y, self.Width, self.Height);
            }

            public Poly2 ToPoly2()
            {
                return new Poly2
                {
                    IsClosed = false,
                    Points = self.Corners.ToArray()
                };
            }

            public bool Contains(in Vector2 point)
            {
                return point.X >= self.X && point.X <= self.Right &&
                       point.Y >= self.Y && point.Y <= self.Bottom;
            }
        }

        #endregion

        #region VECTOR4

        extension(in Vector4 self)
        {
            public Quaternion ToQuaternion()
            {
                return new Quaternion(self.X, self.Y, self.Z, self.W);
            }
        }

        #endregion

        #region FLOAT

        extension(in float self)
        {
            public float ToRadians()
            {
                return (float)(self * (Math.PI / 180.0));
            }

            public float ToDegrees()
            {
                return (float)(self * (180.0 / Math.PI));
            }
        }

        #endregion

        #region MATRIX3X3

        extension(in Matrix3x3 self)
        {
            public bool IsSimilar(in Matrix3x3 other)
            {
                return new Vector3(self.M11, self.M12, self.M13).IsSimilar(new Vector3(other.M11, other.M12, other.M13)) &&
                       new Vector3(self.M21, self.M22, self.M23).IsSimilar(new Vector3(other.M21, other.M22, other.M23)) &&
                       new Vector3(self.M31, self.M32, self.M33).IsSimilar(new Vector3(other.M31, other.M32, other.M33));

            }
        }

        #endregion

        #region SIZE2I

        extension(in Size2I self)
        {
            public Vector2 ToVector2()
            {
                return new Vector2(self.Width, self.Height);
            }
        }

        #endregion

        #region SPHERE

        extension(in Sphere self)
        {
            public bool Intersects(in Sphere other, out float offset)
            {
                var dist = (self.Center - other.Center).Length();

                offset = dist - (self.Radius + other.Radius);

                return offset < 0;
            }
        }

        #endregion

        #region VECTOR3I

        extension(in Vector3I self)
        {
            public int Area()
            {
                return self.Z * self.Y * self.X;
            }
        }

        #endregion

    }
}
