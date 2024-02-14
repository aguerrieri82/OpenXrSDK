using SkiaSharp;
using System.Numerics;

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


        public static T? FindByName<T>(this Group group, string name)    where T : Object3D
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

        public static IEnumerable<Triangle3> Triangles(this Geometry geo)
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
                throw new NotImplementedException();
        }

        public static Triangle3 Transform(this Triangle3 triangle, Matrix4x4 matrix)
        {
            return new Triangle3
            {
                V0 = Vector3.Transform(triangle.V0, matrix),
                V1 = Vector3.Transform(triangle.V1, matrix),
                V2 = Vector3.Transform(triangle.V2, matrix),
            };
        }

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

        public static Vector3? RayIntersect(this Triangle3 triangle, Ray3 ray, out float distance)
        {
            distance = float.PositiveInfinity;

            Vector3 edge1 = triangle.V1 - triangle.V0;
            Vector3 edge2 = triangle.V2 - triangle.V0;
            Vector3 pvec = Vector3.Cross(ray.Direction, edge2);
            float det = Vector3.Dot(edge1, pvec);

            if (Math.Abs(det) < 1e-6f)
                return null;

            float invDet = 1.0f / det;
            Vector3 tvec = ray.Origin - triangle.V0;
            float u = Vector3.Dot(tvec, pvec) * invDet;

            if (u < 0 || u > 1)
                return null;

            Vector3 qvec = Vector3.Cross(tvec, edge1);
            float v = Vector3.Dot(ray.Direction, qvec) * invDet;

            if (v < 0 || u + v > 1)
                return null;

            float t = Vector3.Dot(edge2, qvec) * invDet;

            if (t > 0) // Ray intersection
            {
                // Compute the intersection point
                Vector3 intersectionPoint = ray.Origin + t * ray.Direction;
                distance = t;
                return intersectionPoint;
            }
            else // Line intersection but not ray intersection.
                return null;
        }


        #endregion

        public static void SetZ(this Transform transform, float value)
        {
            transform.Position = new Vector3(transform.Position.X, transform.Position.Y, value);
        }
    }
}
