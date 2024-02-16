using System.Numerics;
using System.Runtime.CompilerServices;

namespace OpenXr.Engine
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

        public static IEnumerable<T> TypeLayerContent<T>(this Scene scene) where T : Object3D
        {
            var layer = scene.Layers.OfType<TypeLayer<T>>().FirstOrDefault();
            if (layer == null)
                return [];
            return layer.Content.Cast<T>();
        }


        public static IEnumerable<Collision> RayCollisions(this Scene scene, Ray3 ray)
        {
            foreach (var obj in scene.VisibleDescendants<Object3D>())
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

        #region MISC

        public static void Update<T>(this IEnumerable<T> target, RenderContext ctx) where T : IRenderUpdate
        {
            target.ForeachSafe(a => a.Update(ctx));
        }

        #endregion

        #region GEOMETRY

        public delegate void VertexAssignDelegate<T>(ref VertexData vertexData, T value);

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

        public static Quaternion RotationTowards(this Vector3 from,  Vector3 to)
        {
            Quaternion result;

            var axis = Vector3.Cross(from, to);
            result.X = axis.X;
            result.Y = axis.Y;
            result.Z = axis.Z;
            result.W = MathF.Sqrt((from.LengthSquared() * to.LengthSquared())) + Vector3.Dot(from, to);
            return Quaternion.Normalize(result);
        }

        public static void UpdateBounds(this Geometry3D geo)
        {
            geo.Bounds = geo.ComputeBounds(Matrix4x4.Identity);
        }

        public static Bounds3 ComputeBounds(this Geometry3D geo, Matrix4x4 transform)
        {
            if (geo.Vertices != null)
                return ComputeBounds(geo.Vertices!.Select(a => a.Pos), transform);

            return new Bounds3();
        }

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

        public static bool ContainsPoint(this Bounds3 bounds, Vector3 point)
        {
            return point.X >= bounds.Min.X && point.X <= bounds.Max.X &&
                   point.Y >= bounds.Min.Y && point.Y <= bounds.Max.Y &&
                   point.Z >= bounds.Min.Z && point.Z <= bounds.Max.Z;
        }

        public static Vector3 Normal(this Triangle3 triangle)
        {
            var edge1 = triangle.V1 - triangle.V0;
            var edge2 = triangle.V2 - triangle.V0;
            var normal = Vector3.Cross(edge1, edge2);
            return Vector3.Normalize(normal);
        }

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

        public static Vector3? RayIntersect(this Triangle3 triangle, Ray3 ray, out float distance, float epsilon = 1e-6f)
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

        #endregion

        #region TRANSFORM

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPosition(this Transform transform, float x, float y, float z)
        {
            transform.Position = new Vector3(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetScale(this Transform transform, float x, float y, float z)
        {
            transform.Scale = new Vector3(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPositionZ(this Transform transform, float value)
        {
            transform.Position = new Vector3(transform.Position.X, transform.Position.Y, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPositionX(this Transform transform, float value)
        {
            transform.Position = new Vector3(value, transform.Position.Y, transform.Position.Z);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPositionY(this Transform transform, float value)
        {
            transform.Position = new Vector3(transform.Position.X, value, transform.Position.Z);
        }

        #endregion
    }
}
