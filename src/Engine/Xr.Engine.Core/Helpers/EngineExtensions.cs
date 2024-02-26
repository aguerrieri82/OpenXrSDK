using System.Numerics;
using System.Runtime.CompilerServices;

namespace Xr.Engine
{
    public static class EngineExtensions
    {
        #region EngineObject

        public static Behavior<T> AddBehavior<T>(this T obj, Action<T, RenderContext> action) where T : EngineObject
        {
            var result = new LambdaBehavior<T>(action);
            obj.AddComponent(result);
            return result;
        }

        public static T AddComponent<T>(this EngineObject obj) where T : IComponent, new()
        {
            var result = new T();
            obj.AddComponent(new T());
            return result;
        }


        #endregion

        #region OBJECT3D

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToLocal(this Object3D obj, Vector3 vector)
        {
            return vector.Transform(obj.WorldMatrixInverse);
        }

        public static IEnumerable<Group> Ancestors(this Object3D obj)
        {
            var curItem = obj.Parent;

            while (curItem != null)
            {
                yield return curItem;
                curItem = curItem.Parent;
            }
        }

        public static T? FindAncestor<T>(this Object3D obj) where T : Group
        {
            return obj.Ancestors().OfType<T>().FirstOrDefault();
        }

        #endregion

        #region SCENE

        public static T AddLayer<T>(this Scene scene) where T : ILayer, new()
        {
            return scene.AddLayer(new T());
        }

        public static T AddLayer<T>(this Scene scene, T layer) where T : ILayer
        {
            scene.Layers.Add(layer);
            return layer;
        }

        public static IEnumerable<Object3D> ObjectsWithComponent<TComp>(this Scene scene) where TComp : IComponent
        {
            var layer = scene.Layers.OfType<ComponentLayer<TComp>>().FirstOrDefault();
            if (layer == null)
            {
                layer = new ComponentLayer<TComp>();
                scene.Layers.Add(layer);
            }

            return layer.Content.Cast<Object3D>();
        }

        public static IEnumerable<T> TypeLayerContent<T>(this Scene scene) where T : Object3D
        {
            var layer = scene.Layers.OfType<TypeLayer<T>>().FirstOrDefault();
            if (layer == null)
                return [];
            return layer.Content.Cast<T>();
        }

        public static IEnumerable<Collision> RayCollisions(this Scene scene, Ray3 ray)
        {

            foreach (var obj in scene.ObjectsWithComponent<ICollider>())
            {
                foreach (var collider in obj.Components<ICollider>())
                {
                    if (!collider.IsEnabled)
                        continue;
                    var collision = collider.CollideWith(ray);
                    if (collision != null)
                        yield return collision;
                }
            }
        }

        #endregion

        #region GROUP

        public static T AddChild<T>(this Group group) where T : Object3D, new()
        {
            return group.AddChild(new T());
        }

        public static T? FindByName<T>(this Group group, string name) where T : Object3D
        {
            return group.Descendants<T>().Where(a => a.Name == name).FirstOrDefault();
        }

        public static IEnumerable<T> VisibleDescendants<T>(this Group target) where T : Object3D
        {
            return target.Descendants<T>().Where(a => a.IsVisible);
        }

        public static IEnumerable<Object3D> Descendants(this Group target)
        {
            return target.Descendants<Object3D>();
        }

        public static IEnumerable<T> Descendants<T>(this Group target) where T : Object3D
        {
            foreach (var child in target.Children)
            {
                if (child is T validChild)
                    yield return validChild;

                if (child is Group group)
                {
                    foreach (var desc in group.Descendants<T>())
                        yield return desc;
                }
            }
        }

        #endregion

        #region ENGINE APP

        public static void OpenScene(this EngineApp app, string name)
        {
            app.OpenScene(app.Scenes.Single(s => s.Name == name));
        }

        #endregion

        #region GEOMETRY

        public delegate void VertexAssignDelegate<T>(ref VertexData vertexData, T value);

        public static void ComputeNormals(this Geometry3D geo)
        {
            if (geo.Indices != null && geo.Indices.Length > 0)
            {
                int i = 0;
                while (i < geo.Indices.Length)
                {
                    var i0 = geo.Indices[i++];
                    var i1 = geo.Indices[i++];
                    var i2 = geo.Indices[i++];

                    var triangle = new Triangle3
                    {
                        V0 = geo.Vertices![i0].Pos,
                        V1 = geo.Vertices![i1].Pos,
                        V2 = geo.Vertices![i2].Pos,
                    };

                    var normal = triangle.Normal();
                    geo.Vertices![i0].Normal = normal;
                    geo.Vertices![i1].Normal = normal;
                    geo.Vertices![i2].Normal = normal;
                }
            }
            else
            {
                int i = 0;
                while (i < geo.Vertices!.Length)
                {
                    var i0 = i++;
                    var i1 = i++;
                    var i2 = i++;

                    var triangle = new Triangle3
                    {
                        V0 = geo.Vertices![i0].Pos,
                        V1 = geo.Vertices![i1].Pos,
                        V2 = geo.Vertices![i2].Pos,
                    };

                    var normal = triangle.Normal();
                    geo.Vertices![i0].Normal = normal;
                    geo.Vertices![i1].Normal = normal;
                    geo.Vertices![i2].Normal = normal;
                }
            }
            geo.ActiveComponents |= VertexComponent.Normal;
            geo.Version++;
        }

        public static void SmoothNormals(this Geometry3D geo)
        {
            Dictionary<Vector3, List<int>> groups = [];

            for (var i = 0; i < geo.Vertices!.Length; i++)
            {
                var v = geo.Vertices[i].Pos;
                if (!groups.TryGetValue(v, out var list))
                {
                    list = [i];
                    groups[v] = list;
                }
                else
                    list.Add(i);
            }
            foreach (var group in groups.Values)
            {
                if (group.Count > 1)
                {
                    var sum = Vector3.Zero;
                    foreach (var index in group)
                        sum += geo.Vertices[index].Normal;

                    sum /= group.Count;
                    foreach (var index in group)
                        geo.Vertices[index].Normal = sum;
                }
            }
            geo.Version++;
        }

        public static IEnumerable<Triangle3> Triangles(this Geometry3D geo)
        {
            if (geo.Indices != null && geo.Indices.Length > 0)
            {
                int i = 0;
                while (i < geo.Indices.Length)
                {
                    var triangle = new Triangle3
                    {
                        V0 = geo.Vertices![geo.Indices[i++]].Pos,
                        V1 = geo.Vertices![geo.Indices[i++]].Pos,
                        V2 = geo.Vertices![geo.Indices[i++]].Pos,
                    };
                    yield return triangle;
                }
            }
            else
            {
                int i = 0;
                while (i < geo.Vertices!.Length)
                {
                    var i0 = i++;
                    var i1 = i++;
                    var i2 = i++;

                    var triangle = new Triangle3
                    {
                        V0 = geo.Vertices![i0].Pos,
                        V1 = geo.Vertices![i1].Pos,
                        V2 = geo.Vertices![i2].Pos,
                    };

                    yield return triangle;

                }
            }
        }

        public static void ComputeTangents(this Geometry3D geo)
        {

            var tan1 = new Vector3[geo.Indices!.Length];
            var tan2 = new Vector3[geo.Indices!.Length];

            int i = 0;
            while (i < geo.Indices!.Length)
            {
                var i1 = geo.Indices[i++];
                var i2 = geo.Indices[i++];
                var i3 = geo.Indices[i++];

                var v1 = geo.Vertices![i1].Pos;
                var v2 = geo.Vertices![i2].Pos;
                var v3 = geo.Vertices![i3].Pos;

                var w1 = geo.Vertices![i1].UV;
                var w2 = geo.Vertices![i2].UV;
                var w3 = geo.Vertices![i3].UV;


                float x1 = v2.X - v1.X;
                float x2 = v3.X - v1.X;
                float y1 = v2.Y - v1.Y;
                float y2 = v3.Y - v1.Y;
                float z1 = v2.Z - v1.Z;
                float z2 = v3.Z - v1.Z;

                float s1 = w2.X - w1.X;
                float s2 = w3.X - w1.X;
                float t1 = w2.Y - w1.Y;
                float t2 = w3.Y - w1.Y;

                float r = 1.0F / (s1 * t2 - s2 * t1);
                var sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                var tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan1[i3] += sdir;

                tan2[i1] += tdir;
                tan2[i2] += tdir;
                tan2[i3] += tdir;
            }


            for (int a = 0; a < geo.Indices!.Length; a++)
            {
                ref var vert = ref geo.Vertices![geo.Indices[a]];

                var n = vert.Normal;
                var t = tan1[a];

                var txyz = Vector3.Normalize(t - n * Vector3.Dot(n, t)); ;

                vert.Tangent.X = txyz.X;
                vert.Tangent.Y = txyz.Y;
                vert.Tangent.Z = txyz.Z;
                vert.Tangent.W = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0F) ? -1.0F : 1.0F;
            }

            geo.ActiveComponents |= VertexComponent.Tangent;
        }

        public static void SetVertexData<T>(this Geometry3D geo, VertexAssignDelegate<T> selector, T[] array)
        {
            if (geo.Vertices == null)
                geo.Vertices = new VertexData[array.Length];

            if (geo.Vertices.Length < array.Length)
            {
                var newArray = geo.Vertices;
                Array.Resize(ref newArray, array.Length);
                geo.Vertices = newArray;
            }

            for (var i = 0; i < array.Length; i++)
                selector(ref geo.Vertices![i], array[i]);
        }

        public static Bounds3 ComputeBounds(this Geometry3D geo, Matrix4x4 transform)
        {
            if (geo.Vertices != null)
                return ComputeBounds(geo.Vertices!.Select(a => a.Pos), transform);

            return new Bounds3();
        }

        #endregion

        #region BOUNDS

        public static Bounds3 ComputeBounds(this IEnumerable<Vector3> points, Matrix4x4 matrix)
        {
            var result = new Bounds3();

            result.Min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            result.Max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            foreach (var point in points)
            {
                var tPoint = point.Transform(matrix);

                result.Min.X = MathF.Min(result.Min.X, tPoint.X);
                result.Min.Y = MathF.Min(result.Min.Y, tPoint.Y);
                result.Min.Z = MathF.Min(result.Min.Z, tPoint.Z);

                result.Max.X = MathF.Max(result.Max.X, tPoint.X);
                result.Max.Y = MathF.Max(result.Max.Y, tPoint.Y);
                result.Max.Z = MathF.Max(result.Max.Z, tPoint.Z);
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

        public static bool Intersects(this Bounds3 bounds, Line3 line)
        {
            Vector3 dir = (line.To - line.From).Normalize(); // direction of the line
            Vector3 tMin = (bounds.Min - line.From) / dir; // minimum t to hit the box
            Vector3 tMax = (bounds.Max - line.From) / dir; // maximum t to hit the box

            // Ensure tMin <= tMax
            Vector3 t1 = Vector3.Min(tMin, tMax);
            Vector3 t2 = Vector3.Max(tMin, tMax);

            float tNear = MathF.Max(MathF.Max(t1.X, t1.Y), t1.Z);
            float tFar = MathF.Min(MathF.Min(t2.X, t2.Y), t2.Z);

            // Return whether intersection exists
            return tNear <= tFar && tFar >= 0;
        }

        #endregion

        #region CAMERA

        public static IEnumerable<Vector3> Project(this Camera camera, IEnumerable<Vector3> worldPoints)
        {
            var viewProj = camera.View * camera.Projection;

            foreach (var vertex in worldPoints)
            {
                var vec4 = new Vector4(vertex.X, vertex.Y, vertex.Z, 1);
                var vTrans = Vector4.Transform(vec4, viewProj);

                vTrans /= vTrans.W;

                yield return new Vector3(vTrans.X, -vTrans.Y, vTrans.Z);
            }
        }

        public static Vector3 Unproject(this Camera camere, Vector3 viewPoint)
        {
            var dirEye = Vector4.Transform(new Vector4(viewPoint, 1.0f), camere.ProjectionInverse);
            dirEye /= dirEye.W;
            var pos4 = Vector4.Transform(dirEye, camere.WorldMatrix);
            return new Vector3(pos4.X, pos4.Y, pos4.Z);
        }

        public static IEnumerable<Line3> FrustumLines(this Camera camera)
        {
            var minZ = 0;
            var maxZ = 1;
            yield return new Line3
            {
                From = camera.Unproject(new Vector3(-1, -1, minZ)),
                To = camera.Unproject(new Vector3(-1, -1, maxZ)),
            };
            yield return new Line3
            {
                From = camera.Unproject(new Vector3(-1, 1, minZ)),
                To = camera.Unproject(new Vector3(-1, 1, maxZ)),
            };
            yield return new Line3
            {
                From = camera.Unproject(new Vector3(1, 1, minZ)),
                To = camera.Unproject(new Vector3(1, 1, maxZ)),
            };
            yield return new Line3
            {
                From = camera.Unproject(new Vector3(1, -1, minZ)),
                To = camera.Unproject(new Vector3(1, -1, maxZ)),
            };

        }

        public static bool CanSee(this Camera camera, Bounds3 bounds)
        {
            var boundsPoints = camera.Project(bounds.Points).ToArray();

            var projBounds = boundsPoints.ComputeBounds(Matrix4x4.Identity);

            var cameraBounds = new Bounds3()
            {
                Max = new Vector3(1.1f, 1.1f, 1.1f),
                Min = new Vector3(-1.1f, -1.1f, -1.1f)
            };


            if (projBounds.Intersects(cameraBounds))
                return true;

            foreach (var line in camera.FrustumLines())
            {
                if (bounds.Intersects(line))
                    return true;
            }

            return false;
        }

        #endregion

        #region TRIANGLE

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

        #endregion

        #region RAY 

        public static Vector3? Intersects(this Ray3 ray, Triangle3 triangle, out float distance, float epsilon = 1e-6f)
        {
            distance = float.PositiveInfinity;

            var edge1 = triangle.V1 - triangle.V0;
            var edge2 = triangle.V2 - triangle.V0;
            var pVec = Vector3.Cross(ray.Direction, edge2);
            var det = Vector3.Dot(edge1, pVec);

            if (Math.Abs(det) < epsilon)
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

        public static Vector3 ToEuler(this Quaternion q)
        {
            Vector3 res;
            q = Quaternion.Normalize(q);
            res.X = MathF.Atan2(2.0f * (q.Y * q.Z + q.W * q.X), q.W * q.W - q.X * q.X - q.Y * q.Y + q.Z * q.Z);
            res.Y = MathF.Asin(-2.0f * (q.X * q.Z - q.W * q.Y));
            res.Z = MathF.Atan2(2.0f * (q.X * q.Y + q.W * q.Z), q.W * q.W + q.X * q.X - q.Y * q.Y - q.Z * q.Z);
            return res;
        }


        #endregion

        #region TRANSFORM

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPosition(this Transform3 transform, float x, float y, float z)
        {
            transform.Position = new Vector3(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetScale(this Transform3 transform, float x, float y, float z)
        {
            transform.Scale = new Vector3(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetScale(this Transform3 transform, float value)
        {
            transform.Scale = new Vector3(value, value, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPositionZ(this Transform3 transform, float value)
        {
            transform.Position = new Vector3(transform.Position.X, transform.Position.Y, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPositionX(this Transform3 transform, float value)
        {
            transform.Position = new Vector3(value, transform.Position.Y, transform.Position.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPositionY(this Transform3 transform, float value)
        {
            transform.Position = new Vector3(transform.Position.X, value, transform.Position.Z);
        }

        #endregion

        #region UNIFORMS

        /*
        public static void SetUniformStruct(this IUniformProvider up, string name, object obj, bool optional = false)
        {
            foreach (var field in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                var fullName = $"{name}.{field.Name}";
                up.SetUniformObject(fullName, field.GetValue(obj)!, optional);
            }
        }

        public static void SetUniformStructArray(this IUniformProvider up, string name, ICollection collection, bool optional = false)
        {
            var i = 0;
            foreach (var item in collection)
            {
                up.SetUniformStruct($"{name}[{i}]", item, optional);
                i++;
            }
        }

        public static unsafe void SetUniformObject(this IUniformProvider up, string name, object obj, bool optional = false)
        {
            if (obj is Vector3 vec3)
                up.SetUniform(name, vec3, optional);
            else if (obj is Matrix4x4 mat4)
                up.SetUniform(name, mat4, optional);
            else if (obj is float flt)
                up.SetUniform(name, flt, optional);
            else if (obj is int vInt)
                up.SetUniform(name, vInt, optional);
            else if (obj is float[] fArray)
                up.SetUniform(name, fArray, optional);
            else if (obj is int[] iArray)
                up.SetUniform(name, iArray, optional);
            else
            {
                var type = obj.GetType();

                if (type.IsValueType && !type.IsEnum && !type.IsPrimitive)
                    up.SetUniformStruct(name, obj, optional);

                else if (obj is ICollection coll)
                {
                    var gen = type.GetInterfaces()
                            .First(a => a.IsGenericType && a.GetGenericTypeDefinition() == typeof(ICollection<>));
                    var elType = gen.GetGenericArguments()[0];
                    if (elType.IsValueType && !elType.IsEnum && !elType.IsPrimitive)
                        up.SetUniformStructArray(name, coll, optional);
                }
                else
                    throw new NotSupportedException();
            }
        }

        */
        #endregion

        #region MISC

        public static void Update<T>(this IEnumerable<T> target, RenderContext ctx) where T : IRenderUpdate
        {
            target.ForeachSafe(a => a.Update(ctx));
        }

        #endregion
    }
}
