﻿using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using XrEngine.Services;
using XrMath;

namespace XrEngine
{


    public struct ObjectFeature<T> where T : notnull
    {

        public Object3D Object;

        public T Feature;
    }

    public static class EngineExtensions
    {
        #region EngineObject

        public static void SetFlag(this EngineObject obj, EngineObjectFlags flag, bool isSet)
        {
            if (isSet)
                obj.Flags |= flag;
            else
                obj.Flags &= ~flag;
        }

        public static Behavior<T> AddBehavior<T>(this T obj, Action<T, RenderContext> action) where T : EngineObject
        {
            var result = new LambdaBehavior<T>(action);
            obj.AddComponent(result);
            return result;
        }

        public static T AddComponent<T>(this EngineObject obj) where T : IComponent, new()
        {
            var result = new T();
            obj.AddComponent(result);
            return result;
        }

        public static T Component<T>(this EngineObject obj) where T : IComponent
        {
            return obj.Components<T>().Single();
        }

        public static T GetOrCreateProp<T>(this EngineObject obj, string name, Func<T> create)
        {
            var result = obj.GetProp<T?>(name);
            if (result == null)
            {
                result = create();
                obj.SetProp(name, result);
            }
            return result;
        }

        #endregion

        #region OBJECT3D

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToLocal(this Object3D obj, Vector3 vector)
        {
            return vector.Transform(obj.WorldMatrixInverse);
        }

        public static bool IsManipulating(this Object3D obj)
        {
            return obj.GetProp<bool>("IsManipulating");
        }

        public static void IsManipulating(this Object3D obj, bool value)
        {
            obj.SetProp("IsManipulating", value);
        }

        public static IEnumerable<Object3D> DescendantsOrSelf(this Object3D self)
        {
            yield return self;

            if (self is Group3D group)
            {
                foreach (var child in group.Children)
                {
                    foreach (var descendent in child.DescendantsOrSelf())
                        yield return descendent;
                }
            }
        }

        public static IEnumerable<T> DescendantsOrSelfComponents<T>(this Object3D self)
        {
            foreach (var obj in self.DescendantsOrSelf())
            {
                foreach (var comp in obj.Components<IComponent>().OfType<T>())
                    yield return comp;
            }
        }

        public static IEnumerable<Group3D> Ancestors(this Object3D self)
        {
            var curItem = self.Parent;

            while (curItem != null)
            {
                yield return curItem;
                curItem = curItem.Parent;
            }
        }

        public static T? FindAncestor<T>(this Object3D self) where T : Group3D
        {
            return self.Ancestors().OfType<T>().FirstOrDefault();
        }

        public static bool Feature<T>(this Object3D self, [NotNullWhen(true)] out T? result) where T : class
        {
            result = self.Feature<T>();
            return result != null;
        }


        public static T? FeatureDeep<T>(this Object3D self) where T : class
        {
            var result = self.Feature<T>();

            if (result != null)
                return result;

            if (self is Group3D group)
                return group.DescendantsWithFeature<T>().FirstOrDefault().Feature;

            return null;
        }

        public static bool Is(this EngineObject self, EngineObjectFlags flags)
        {
            return (self.Flags & flags) == flags;
        }

        public static void SetGlobalPoseIfChanged(this Object3D self, Pose3 pose, float epsilon = 0.001f)
        {
            var deltaPos = (pose.Position - self.WorldPosition).Length();
            var deltaOri = (pose.Orientation - self.WorldOrientation).Length();

            if (deltaPos > epsilon)
                self.WorldPosition = pose.Position;

            if (deltaOri > epsilon)
                self.WorldOrientation = pose.Orientation;
        }

        #endregion

        #region SCENE

        public static PerspectiveCamera PerspectiveCamera(this Scene3D scene)
        {
            return ((PerspectiveCamera)scene.ActiveCamera!);
        }

        public static T AddLayer<T>(this Scene3D scene) where T : ILayer3D, new()
        {
            return scene.AddLayer(new T());
        }

        public static T AddLayer<T>(this Scene3D scene, T layer) where T : ILayer3D
        {
            scene.Layers.Add(layer);
            return layer;
        }

        public static IEnumerable<Object3D> ObjectsWithComponent<TComp>(this Scene3D scene) where TComp : IComponent
        {
            var layer = scene.Layers.OfType<ComponentLayer<TComp>>().FirstOrDefault();
            if (layer == null)
            {
                layer = new ComponentLayer<TComp>();
                scene.Layers.Add(layer);
            }

            return layer.Content.Cast<Object3D>();
        }

        public static IEnumerable<T> TypeLayerContent<T>(this Scene3D scene) where T : Object3D
        {
            var layer = scene.Layers.OfType<TypeLayer<T>>().FirstOrDefault();
            if (layer == null)
                return [];
            return layer.Content.Cast<T>();
        }

        public static IEnumerable<Collision> RayCollisions(this Scene3D scene, Ray3 ray)
        {

            foreach (var obj in scene.ObjectsWithComponent<ICollider3D>())
            {
                foreach (var collider in obj.Components<ICollider3D>())
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

        public static void Clear(this Group3D self, bool dispose = false)
        {
            self.BeginUpdate();
            try
            {
                for (var i = self.Children.Count - 1; i >= 0; i--)
                {
                    if (dispose)
                        self.Children[i].Dispose();
                    else
                        self.RemoveChild(self.Children[i]);
                }
            }
            finally
            {
                self.EndUpdate();
            }
        }

        public static T AddChild<T>(this Group3D self) where T : Object3D, new()
        {
            return self.AddChild(new T());
        }

        public static T? FindByName<T>(this Group3D self, string name) where T : Object3D
        {
            return self.Descendants<T>().Where(a => a.Name == name).FirstOrDefault();
        }


        public static IEnumerable<ObjectFeature<T>> DescendantsWithFeature<T>(this Group3D group) where T : class
        {
            foreach (var item in group.Descendants())
            {
                var feat = item.Feature<T>();
                if (feat != null)
                    yield return new ObjectFeature<T>
                    {
                        Object = item,
                        Feature = feat
                    };
            }
        }

        public static IEnumerable<T> VisibleDescendants<T>(this Group3D target) where T : Object3D
        {
            return target.Descendants<T>().Where(a => a.IsVisible);
        }

        public static IEnumerable<Object3D> Descendants(this Group3D target)
        {
            return target.Descendants<Object3D>();
        }



        public static IEnumerable<T> Descendants<T>(this Group3D target) where T : Object3D
        {
            foreach (var child in target.Children)
            {
                if (child is T validChild)
                    yield return validChild;

                if (child is Group3D group)
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

        public static Geometry3D TransformToLine(this Geometry3D src)
        {
            var res = new Geometry3D();
            if (src.Indices.Length > 0)
            {
                var srcI = 0;
                var dstI = 0;
                var newIndices = new uint[src.Indices.Length * 2];
                var newSpan = newIndices.AsSpan();
                var srcSpan = src.Indices.AsSpan();

                while (srcI < src.Indices!.Length)
                {
                    newSpan[dstI + 0] = srcSpan[srcI + 0];
                    newSpan[dstI + 1] = srcSpan[srcI + 1];
                    newSpan[dstI + 2] = srcSpan[srcI + 1];
                    newSpan[dstI + 3] = srcSpan[srcI + 2];
                    newSpan[dstI + 4] = srcSpan[srcI + 2];
                    newSpan[dstI + 5] = srcSpan[srcI + 0];
                    srcI += 3;
                    dstI += 6;
                }

                res.Vertices = src.Vertices;
                res.Indices = newIndices;

            }
            else
            {

            }

            return res;
        }

        public static unsafe Vector3[] ExtractPositions(this Geometry3D geo, bool useIndex = false)
        {
            if (!useIndex)
            {
                var result = new Vector3[geo.Vertices.Length];
                var dstSpan = result.AsSpan();
                var srcSpan = geo.Vertices.AsSpan();
                for (var i = 0; i < dstSpan.Length; i++)
                    dstSpan[i] = srcSpan[i].Pos;
                return result;
            }
            else
            {
                var result = new Vector3[geo.Indices.Length];
                var dstSpan = result.AsSpan();
                var srcSpan = geo.Vertices.AsSpan();
                var srcIdx = geo.Indices.AsSpan();
                for (var i = 0; i < srcIdx.Length; i++)
                    dstSpan[i] = srcSpan[(int)srcIdx[i]].Pos;
                return result;
            }
        }

        public static void ComputeNormals(this Geometry3D geo)
        {
            if (geo.Indices.Length > 0)
            {
                int i = 0;
                while (i < geo.Indices.Length)
                {
                    var i0 = geo.Indices[i++];
                    var i1 = geo.Indices[i++];
                    var i2 = geo.Indices[i++];

                    var triangle = new Triangle3
                    {
                        V0 = geo.Vertices[i0].Pos,
                        V1 = geo.Vertices[i1].Pos,
                        V2 = geo.Vertices[i2].Pos,
                    };

                    var normal = triangle.Normal();
                    geo.Vertices[i0].Normal = normal;
                    geo.Vertices[i1].Normal = normal;
                    geo.Vertices[i2].Normal = normal;
                }
            }
            else
            {
                int i = 0;
                while (i < geo.Vertices.Length)
                {
                    var i0 = i++;
                    var i1 = i++;
                    var i2 = i++;

                    var triangle = new Triangle3
                    {
                        V0 = geo.Vertices[i0].Pos,
                        V1 = geo.Vertices[i1].Pos,
                        V2 = geo.Vertices[i2].Pos,
                    };

                    var normal = triangle.Normal();
                    geo.Vertices[i0].Normal = normal;
                    geo.Vertices[i1].Normal = normal;
                    geo.Vertices[i2].Normal = normal;
                }
            }
            geo.ActiveComponents |= VertexComponent.Normal;
            geo.Version++;
        }

        public static void EnsureIndices(this Geometry3D geo)
        {
            if (geo.Indices == null || geo.Indices.Length == 0)
            {
                geo.Indices = new uint[geo.Vertices.Length];
                for (var i = 0; i < geo.Vertices.Length; i++)
                    geo.Indices[i] = (uint)i;
            }
        }

        public static void SmoothNormals(this Geometry3D geo)
        {
            SmoothNormals(geo, 0, (uint)geo.Vertices.Length - 1);
        }

        public static void SmoothNormals(this Geometry3D geo, uint startIndex, uint endIndex)
        {
            Dictionary<Vector3, List<uint>> groups = [];

            for (var i = startIndex; i <= endIndex; i++)
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
            if (geo.Indices.Length > 0)
            {
                int i = 0;
                while (i < geo.Indices.Length)
                {
                    var triangle = new Triangle3
                    {
                        V0 = geo.Vertices[geo.Indices[i++]].Pos,
                        V1 = geo.Vertices[geo.Indices[i++]].Pos,
                        V2 = geo.Vertices[geo.Indices[i++]].Pos,
                    };
                    yield return triangle;
                }
            }
            else
            {
                int i = 0;
                while (i < geo.Vertices.Length)
                {
                    var i0 = i++;
                    var i1 = i++;
                    var i2 = i++;

                    var triangle = new Triangle3
                    {
                        V0 = geo.Vertices[i0].Pos,
                        V1 = geo.Vertices[i1].Pos,
                        V2 = geo.Vertices[i2].Pos,
                    };

                    yield return triangle;

                }
            }
        }

        public static void OrthoNormalize(ref Vector3 normal, ref Vector3 tangent)
        {
            // Normalize the normal vector
            normal = Vector3.Normalize(normal);

            // Project the tangent onto the normal
            Vector3 proj = normal * Vector3.Dot(tangent, normal);

            // Subtract the projection from the tangent to make it orthogonal to the normal
            tangent -= proj;

            // Normalize the tangent vector
            float tangentLength = tangent.Length();
            if (tangentLength > 1e-6f) // Avoid division by zero
            {
                tangent /= tangentLength;
            }
            else
            {
                // If the tangent length is zero, set it to an arbitrary orthogonal vector
                tangent = Vector3.Cross(normal, Vector3.UnitX);
                if (tangent.LengthSquared() < 1e-6f)
                {
                    tangent = Vector3.Cross(normal, Vector3.UnitY);
                }
                tangent = Vector3.Normalize(tangent);
            }
        }

        public static void ComputeTangents(this Geometry3D geo)
        {
            int vertexCount = geo.Vertices.Length;
            int indexCount = geo.Indices.Length;

            // Arrays to accumulate the tangent and bitangent vectors
            Vector3[] tan1 = new Vector3[vertexCount];
            Vector3[] tan2 = new Vector3[vertexCount];

            // Iterate over each triangle
            for (int i = 0; i < indexCount; i += 3)
            {
                uint i1 = geo.Indices[i];
                uint i2 = geo.Indices[i + 1];
                uint i3 = geo.Indices[i + 2];

                Vector3 v1 = geo.Vertices[i1].Pos;
                Vector3 v2 = geo.Vertices[i2].Pos;
                Vector3 v3 = geo.Vertices[i3].Pos;

                Vector2 w1 = geo.Vertices[i1].UV;
                Vector2 w2 = geo.Vertices[i2].UV;
                Vector2 w3 = geo.Vertices[i3].UV;

                float x1 = v2.X - v1.X;
                float y1 = v2.Y - v1.Y;
                float z1 = v2.Z - v1.Z;

                float x2 = v3.X - v1.X;
                float y2 = v3.Y - v1.Y;
                float z2 = v3.Z - v1.Z;

                float s1 = w2.X - w1.X;
                float t1 = w2.Y - w1.Y;

                float s2 = w3.X - w1.X;
                float t2 = w3.Y - w1.Y;

                float r = (s1 * t2 - s2 * t1);
                float f = r == 0.0f ? 0.0f : 1.0f / r;

                Vector3 sdir = new Vector3(
                    (t2 * x1 - t1 * x2) * f,
                    (t2 * y1 - t1 * y2) * f,
                    (t2 * z1 - t1 * z2) * f
                );

                Vector3 tdir = new Vector3(
                    (s1 * x2 - s2 * x1) * f,
                    (s1 * y2 - s2 * y1) * f,
                    (s1 * z2 - s2 * z1) * f
                );

                // Accumulate the tangent and bitangent vectors
                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan1[i3] += sdir;

                tan2[i1] += tdir;
                tan2[i2] += tdir;
                tan2[i3] += tdir;
            }

            // Orthogonalize and normalize the tangent vectors
            for (int i = 0; i < vertexCount; ++i)
            {
                Vector3 n = geo.Vertices[i].Normal;
                Vector3 t = tan1[i];

                // Gram-Schmidt orthogonalization
                OrthoNormalize(ref n, ref t);

                // Calculate the handedness (w component)
                Vector3 c = Vector3.Cross(n, t);
                float w = (Vector3.Dot(c, tan2[i]) < 0.0f) ? -1.0f : 1.0f;

                // Set the tangent with the calculated w component
                geo.Vertices[i].Tangent = new Vector4(t.X, t.Y, t.Z, w);
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
                selector(ref geo.Vertices[i], array[i]);
        }

        public static Bounds3 ComputeBounds(this Geometry3D geo, Matrix4x4 transform)
        {
            if (geo.Vertices != null)
                return geo.ExtractPositions().ComputeBounds(transform);

            return new Bounds3();
        }

        public static void EnsureCCW(this Geometry3D geo)
        {
            if (geo.Indices.Length == 0)
                throw new NotSupportedException();

            int i = 0;

            var vSpan = new Span<VertexData>(geo.Vertices);
            var iSpan = new Span<uint>(geo.Indices);

            while (i < geo.Indices.Length)
            {
                var i0 = iSpan[i];
                var i1 = iSpan[i + 1];
                var i2 = iSpan[i + 2];

                var tri = new Triangle3
                {
                    V0 = vSpan[(int)i0].Pos,
                    V1 = vSpan[(int)i1].Pos,
                    V2 = vSpan[(int)i2].Pos,
                };

                var normal = (
                    (vSpan[(int)i0].Normal +
                     vSpan[(int)i1].Normal +
                     vSpan[(int)i2].Normal) / 3).Normalize();

                var dot = Vector3.Dot(normal, tri.Normal());

                if (dot < 0)
                {
                    iSpan[i] = i2;
                    iSpan[i + 2] = i0;
                }

                i += 3;
            }
        }

        #endregion

        #region CAMERA

        public static void CreateViewFromDirection(this Camera self, Vector3 directionVector, Vector3 upVector)
        {
            var lookDirection = Vector3.Normalize(-directionVector);

            var right = Vector3.Normalize(Vector3.Cross(upVector, lookDirection));

            var cameraUp = Vector3.Cross(lookDirection, right);

            var cameraPosition = new Vector3(0, 5, 0);

            self.View = new Matrix4x4(
                right.X, cameraUp.X, lookDirection.X, 0,
                right.Y, cameraUp.Y, lookDirection.Y, 0,
                right.Z, cameraUp.Z, lookDirection.Z, 0,
                -Vector3.Dot(right, cameraPosition),
                -Vector3.Dot(cameraUp, cameraPosition),
                -Vector3.Dot(lookDirection, cameraPosition),
                1
            );
        }

        public static Vector3 Project(this Camera camera, Vector3 worldPoint)
        {

            return worldPoint.Project(camera.ViewProjection);
        }

        public static IEnumerable<Vector3> Project(this Camera camera, IEnumerable<Vector3> worldPoints)
        {
            var viewProj = camera.ViewProjection;

            foreach (var vertex in worldPoints)
                yield return vertex.Project(viewProj);
        }

        public static Vector3 Unproject(this Camera camera, Vector3 viewPoint)
        {
            var viewProjInv = camera.ViewProjectionInverse;
            return viewPoint.Project(viewProjInv);
        }

        public static IEnumerable<Vector3> Unproject(this Camera camera, IEnumerable<Vector3> viewPoint)
        {
            var viewProjInv = camera.ViewProjectionInverse;
            foreach (var vertex in viewPoint)
                yield return vertex.Project(viewProjInv);
        }

        public static Vector3[] FrustumPoints(this Camera camera)
        {
            var viewProjInv = camera.ViewProjectionInverse;

            Vector3[] corners = new Vector3[8];

            corners[0] = new Vector3(-1, -1, 0).Project(viewProjInv);
            corners[1] = new Vector3(1, -1, 0).Project(viewProjInv);
            corners[2] = new Vector3(-1, 1, 0).Project(viewProjInv);
            corners[3] = new Vector3(1, 1, 0).Project(viewProjInv);
            corners[4] = new Vector3(-1, -1, 1).Project(viewProjInv);
            corners[6] = new Vector3(-1, 1, 1).Project(viewProjInv);
            corners[7] = new Vector3(1, 1, 1).Project(viewProjInv);

            return corners;

        }

        public static IList<Plane> FrustumPlanes(this Camera camera)
        {
            var viewProjectionMatrix = camera.ViewProjection;

            var planes = new Plane[6];

            // Left plane
            planes[0] = new Plane(
                viewProjectionMatrix.M14 + viewProjectionMatrix.M11,
                viewProjectionMatrix.M24 + viewProjectionMatrix.M21,
                viewProjectionMatrix.M34 + viewProjectionMatrix.M31,
                viewProjectionMatrix.M44 + viewProjectionMatrix.M41
            );

            // Right plane
            planes[1] = new Plane(
                viewProjectionMatrix.M14 - viewProjectionMatrix.M11,
                viewProjectionMatrix.M24 - viewProjectionMatrix.M21,
                viewProjectionMatrix.M34 - viewProjectionMatrix.M31,
                viewProjectionMatrix.M44 - viewProjectionMatrix.M41
            );

            // Top plane
            planes[2] = new Plane(
                viewProjectionMatrix.M14 - viewProjectionMatrix.M12,
                viewProjectionMatrix.M24 - viewProjectionMatrix.M22,
                viewProjectionMatrix.M34 - viewProjectionMatrix.M32,
                viewProjectionMatrix.M44 - viewProjectionMatrix.M42
            );

            // Bottom plane
            planes[3] = new Plane(
                viewProjectionMatrix.M14 + viewProjectionMatrix.M12,
                viewProjectionMatrix.M24 + viewProjectionMatrix.M22,
                viewProjectionMatrix.M34 + viewProjectionMatrix.M32,
                viewProjectionMatrix.M44 + viewProjectionMatrix.M42
            );

            // Near plane
            planes[4] = new Plane(
                viewProjectionMatrix.M13,
                viewProjectionMatrix.M23,
                viewProjectionMatrix.M33,
                viewProjectionMatrix.M43
            );

            // Far plane
            planes[5] = new Plane(
                viewProjectionMatrix.M14 - viewProjectionMatrix.M13,
                viewProjectionMatrix.M24 - viewProjectionMatrix.M23,
                viewProjectionMatrix.M34 - viewProjectionMatrix.M33,
                viewProjectionMatrix.M44 - viewProjectionMatrix.M43
            );

            // Normalize the planes
            for (int i = 0; i < 6; i++)
            {
                planes[i] = Plane.Normalize(planes[i]);
            }
            return planes;
        }


        /*
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
                if (bounds.Intersects(line, out var _))
                    return true;
            }

            return false;
        }
        */

        #endregion

        #region TRANSFORM

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPosition(this Transform3D transform, float x, float y, float z)
        {
            transform.Position = new Vector3(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetScale(this Transform3D transform, float x, float y, float z)
        {
            transform.Scale = new Vector3(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetScale(this Transform3D transform, float value)
        {
            transform.Scale = new Vector3(value, value, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPositionZ(this Transform3D transform, float value)
        {
            transform.Position = new Vector3(transform.Position.X, transform.Position.Y, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPositionX(this Transform3D transform, float value)
        {
            transform.Position = new Vector3(value, transform.Position.Y, transform.Position.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPositionY(this Transform3D transform, float value)
        {
            transform.Position = new Vector3(transform.Position.X, value, transform.Position.Z);
        }

        public static Pose3 ToPose(this Transform3D transform)
        {
            return new Pose3
            {
                Orientation = transform.Orientation,
                Position = transform.Position
            };
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

        #region MATERIAL

        public static void UpdateColor(this TriangleMesh mesh, Color color)
        {
            mesh.Materials[0].UpdateColor(color);
        }

        public static void UpdateColor(this Material material, Color color)
        {
            var src = (IColorSource)material;

            if ((Vector4)src.Color != (Vector4)color)
            {
                ((IColorSource)material).Color = color;
                material.NotifyChanged(ObjectChangeType.Render);
            }

        }

        #endregion

        #region MISC

        public static void Update<T>(this IEnumerable<T> target, RenderContext ctx) where T : IRenderUpdate
        {
            foreach (var item in target.ToArray())
                item.Update(ctx);

            //target.ForeachSafe(a => a.Update(ctx));
        }

        public static void Reset<T>(this IEnumerable<T> target, bool onlySelf) where T : IRenderUpdate
        {
            //target.ForeachSafe(a => a.Reset(onlySelf));
        }

        public static T Load<T>(this AssetLoader self, string fileUri, IAssetLoaderOptions? options = null) where T : EngineObject
        {
            return (T)self.Load(new Uri(fileUri, UriKind.Absolute), typeof(T), null, options);
        }


        #endregion


    }
}
