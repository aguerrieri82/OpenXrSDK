using SkiaSharp;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using XrEngine.Helpers;
using XrEngine.Objects;
using XrMath;
using static XrEngine.ShaderUpdateBuilder;

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

        public static void SetFlag(this EngineObject self, EngineObjectFlags flag, bool isSet)
        {
            if (isSet)
                self.Flags |= flag;
            else
                self.Flags &= ~flag;
        }

        public static Behavior<T> AddBehavior<T>(this T self, Action<T, RenderContext> action) where T : EngineObject
        {
            var result = new LambdaBehavior<T>(action);
            self.AddComponent(result);
            return result;
        }

        public static T AddComponent<T>(this EngineObject self) where T : IComponent, new()
        {
            var result = new T();
            self.AddComponent(result);
            return result;
        }

        public static IEnumerable<T> Components<T>(this EngineObject self)
        {
            return self.Components().OfType<T>();
        }

        public static IEnumerable<T> ComponentsDeep<T>(this Object3D self)
        {
            return self.DescendantsOrSelf().SelectMany(a => a.Components<T>());
        }

        public static T Component<T>(this EngineObject self) where T : IComponent
        {
            return self.Components<T>().Single();
        }

        public static bool TryComponent<T>(this EngineObject self, [NotNullWhen(true)] out T? result) where T : IComponent
        {
            result = self.Components<T>().FirstOrDefault();
            return result != null;
        }

        public static void SetProp<T>(this EngineObject self, string propName, T value)
        {
            self.SetProp(new DynamicProp(propName), value);
        }

        public static T GetProp<T>(this EngineObject self, string propName)
        {
            return self.GetProp<T>(new DynamicProp(propName))!;
        }

        public static T GetOrCreateProp<T>(this EngineObject self, int propId, Func<T> create)
        {
            var result = self.GetProp<T?>(propId);
            if (result == null)
            {
                result = create();
                self.SetProp(propId, result);
            }
            return result;
        }

        #endregion

        #region OBJECT3D

        public static void Remove(this Object3D self)
        {
            self.Parent?.RemoveChild(self);
        }

        public static void PropagateTransform(this Object3D self)
        {
            var curLocal = self.Transform.Matrix;

            if (curLocal.IsIdentity)
                return;

            void VisitChildren(Object3D item)
            {
                if (item is not Group3D grp)
                    return;

                foreach (var child in grp.Children)
                    child.Transform.Set(child.Transform.Matrix * curLocal);
            }

            self.Transform.Set(Matrix4x4.Identity);

            VisitChildren(self);

        }

        public static IEnumerable<Object3D> FindByNames(this Group3D self, params string[] names)
        {
            foreach (var name in names)
            {
                var child = self.DescendantsOrSelf().FirstOrDefault(a => a.Name == name);
                if (child != null)
                    yield return child;
            }
        }

        public static Group3D GroupByName(this Group3D self, params string[] names)
        {
            return self.GroupByName(Matrix4x4.Identity, names);
        }

        public static Group3D GroupByName(this Group3D self, Matrix4x4 grpTransform, params string[] names)
        {
            var grp = new Group3D();
            if (!grpTransform.IsIdentity)
                grp.Transform.Set(grpTransform);
            self.AddChild(grp);

            foreach (var child in self.FindByNames(names))
                grp.AddChild(child, true);

            return grp;
        }

        public static void UseEnvDepth(this Object3D self, bool value)
        {
            foreach (var mat in self.MaterialsDeep<IEnvDepthMaterial>())
            {
                if (mat.UseEnvDepth != value)
                {
                    mat.UseEnvDepth = value;
                    mat.NotifyChanged(ChangeType.Render);
                }
            }
        }

        public static void CastShadows(this Object3D self, bool value)
        {
            foreach (var mat in self.MaterialsDeep<IShadowMaterial>())
            {
                if (mat.CastShadows != value)
                {
                    mat.CastShadows = value;
                    mat.NotifyChanged(ChangeType.Render);
                }
            }
        }

        public static IEnumerable<T> MaterialsDeep<T>(this Object3D self) where T : IMaterial
        {
            return self.DescendantsOrSelf()
                .OfType<TriangleMesh>()
                .SelectMany(a => a.Materials)
                .OfType<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToLocal(this Object3D self, Vector3 worldPoint)
        {
            return worldPoint.Transform(self.WorldMatrixInverse);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToWorld(this Object3D self, Vector3 localPoint)
        {
            return localPoint.Transform(self.WorldMatrix);
        }

        public static void SetWorldPoseIfChanged(this Object3D self, Pose3 pose, bool fromOrigin = false, float epsilonP = 0.001f, float epsilonO = 0.001f)
        {
            var curPose = self.GetWorldPose(fromOrigin);
            if (!curPose.IsSimilar(pose))
                SetWorldPose(self, pose, fromOrigin);
        }

        public static void SetWorldPose(this Object3D self, Pose3 pose, bool fromOrigin = false)
        {
            self.WorldOrientation = pose.Orientation;
            if (fromOrigin)
                self.MoveLocalToWorld(Vector3.Zero, pose.Position);
            else
                self.WorldPosition = pose.Position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pose3 GetWorldPose(this Object3D self, bool fromOrigin = false)
        {
            var result = new Pose3
            {
                Orientation = self.WorldOrientation,
                Position = fromOrigin ? self.ToWorld(Vector3.Zero) : self.WorldPosition
            };

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pose3 GetLocalPose(this Object3D self)
        {
            return new Pose3
            {
                Orientation = self.Transform.Orientation,
                Position = self.Transform.Position
            };
        }

        public static void MoveLocalToWorld(this Object3D self, Vector3 localPos, Vector3 worldPos)
        {
            var localPosAdjusted = (localPos - self.Transform.LocalPivot) * self.Transform.Scale;

            var rotatedLocalPos = localPosAdjusted.Transform(self.Transform.Orientation);

            if (self.Parent != null)
                worldPos = worldPos.Transform(self.Parent.WorldMatrixInverse);

            self.Transform.Position = worldPos - rotatedLocalPos;
        }

        public static void SetActiveTool(this Object3D self, IObjectTool value, bool isActive)
        {
            var curTool = self.GetActiveTool();

            if (isActive)
            {
                if (curTool != value)
                    curTool?.Deactivate();

                self.SetProp(EngineProps.ActiveTool, value);
            }

            else if (curTool == value)
                self.SetProp(EngineProps.ActiveTool, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IObjectTool? GetActiveTool(this Object3D self)
        {
            return self.GetProp<IObjectTool?>(EngineProps.ActiveTool);
        }

        public static IEnumerable<Object3D> DescendantsOrSelf(this Object3D self)
        {
            var stack = new Stack<Object3D>();

            stack.Push(self);

            while (stack.Count > 0)
            {
                var cur = stack.Pop();
                yield return cur;

                if (cur is Group3D g && g.Children is not null)
                {
                    for (var i = g.Children.Count - 1; i >= 0; i--)
                        stack.Push(g.Children[i]);
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

        public static IEnumerable<Object3D> AncestorsOrSelf(this Object3D self)
        {
            return new Object3D[] { self }.Concat(self.Ancestors());
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
            {
                foreach (var child in group.Children)
                {
                    result = child.FeatureDeep<T>();
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is(this EngineObject self, EngineObjectFlags flags)
        {
            return (self.Flags & flags) == flags;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> Visible<T>(this IEnumerable<T> self) where T : Object3D
        {
            return self.Where(a => a.IsVisible);
        }

        #endregion

        #region SCENE

        public static T EnsureLayer<T>(this Scene3D self) where T : ILayer3D, new()
        {
            var layer = self.Layers.OfType<T>().FirstOrDefault();
            layer ??= self.AddLayer<T>();
            return layer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PerspectiveCamera PerspectiveCamera(this Scene3D self)
        {
            return ((PerspectiveCamera)self.ActiveCamera!);
        }

        public static T Layer<T>(this Scene3D self) where T : ILayer3D
        {
            return self.Layers.Layers.OfType<T>().First();
        }

        public static T AddLayer<T>(this Scene3D self) where T : ILayer3D, new()
        {
            return self.AddLayer(new T());
        }

        public static T AddLayer<T>(this Scene3D self, T layer) where T : ILayer3D
        {
            self.Layers.Add(layer);
            return layer;
        }

        public static IEnumerable<Object3D> ObjectsWithComponent<TComp>(this Scene3D self) where TComp : IComponent
        {
            var layer = self.Layers.OfType<ComponentLayer<TComp>>().FirstOrDefault();
            if (layer == null)
            {
                layer = new ComponentLayer<TComp>();
                self.Layers.Add(layer);
            }

            return layer.Content.Cast<Object3D>();
        }

        public static IEnumerable<T> TypeLayerContent<T>(this Scene3D self) where T : Object3D
        {
            var layer = self.Layers.OfType<TypeLayer<T>>().FirstOrDefault();
            if (layer == null)
                return [];
            return layer.Content.Cast<T>();
        }

        public static void RayCollisions(this Scene3D self,
            Ray3 ray,
            ConcurrentBag<Collision> result,
            IEnumerable<ICollider3D>? colliders = null,
            bool isParallel = false,
            bool excludeMesh = false)
        {
            IEnumerable<ICollider3D> GetColliders()
            {
                foreach (var obj in self.ObjectsWithComponent<ICollider3D>())
                {
                    foreach (var collider in obj.Components<ICollider3D>())
                    {
                        if (collider != null &&
                            collider.IsEnabled &&
                            (collider.Usage & ColliderUsage.Collisions) != 0 &&
                            ((Object3D)collider.Host!).IsVisible)
                        {
                            yield return collider;
                        }
                    }
                }
            }

            colliders ??= GetColliders();

            if (excludeMesh)
                colliders = colliders.Where(a => a is not MeshCollider);

            if (isParallel)
            {
                Parallel.ForEach(colliders, collider =>
                {
                    var collision = collider.CollideWith(ray);
                    if (collision != null)
                        result.Add(collision);
                });
            }
            else
            {
                foreach (var collider in colliders)
                {
                    var collision = collider.CollideWith(ray);
                    if (collision != null)
                        result.Add(collision);
                }
            }
        }

        public static void ContainsPoint(this Scene3D self, Vector3 worldPoint, ConcurrentBag<Object3D> result, IEnumerable<ICollider3D>? colliders = null, float tollerance = 0)
        {
            IEnumerable<ICollider3D> GetColliders()
            {
                foreach (var obj in self.DescendantsOrSelf().Visible())
                {
                    var collider = obj.Feature<ICollider3D>();
                    if (collider != null && collider.IsEnabled)
                        yield return collider;
                }
            }

            colliders ??= GetColliders();

            result.Clear();

            Parallel.ForEach(colliders, collider =>
            {
                if (collider.ContainsPoint(worldPoint, tollerance))
                    result.Add((Object3D)collider.Host!);

            });
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

        public static IEnumerable<ObjectFeature<T>> DescendantsWithFeature<T>(this Group3D self) where T : class
        {
            foreach (var item in self.Descendants())
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

        public static IEnumerable<Object3D> Descendants(this Group3D self)
        {
            return self.Descendants<Object3D>();
        }

        public static IEnumerable<T> Descendants<T>(this Group3D self) where T : Object3D
        {

            var stack = new Stack<Object3D>();

            for (var i = self.Children.Count - 1; i >= 0; i--)
                stack.Push(self.Children[i]);

            while (stack.Count > 0)
            {
                var cur = stack.Pop();

                if (cur is T valid)
                    yield return valid;

                if (cur is Group3D g && g.Children is not null)
                {
                    for (var i = g.Children.Count - 1; i >= 0; i--)
                        stack.Push(g.Children[i]);
                }
            }
        }

        #endregion

        #region ENGINE APP

        public static void OpenScene(this EngineApp self, string name)
        {
            self.OpenScene(self.Scenes.Single(s => s.Name == name));
        }

        #endregion

        #region GEOMETRY

        public delegate void VertexAssignDelegate<T>(ref VertexData vertexData, in T value);

        public delegate void SkinAssignDelegate<T>(ref SkinData vertexData, in T value);

        public static void FlipYUV(this Geometry3D self)
        {
            var span = self.Vertices.AsSpan();

            for (var i = 0; i < span.Length; i++)
            {
                ref var ver = ref span[i];
                ver.UV.Y = 1 - ver.UV.Y;
            }

            self.NotifyChanged(ChangeType.Geometry);
        }

        public static void Rebuild(this Geometry3D self, IEnumerable<Triangle3> triangles)
        {
            var vertex = new List<VertexData>();

            var indices = triangles.SelectMany(a => a.Indices);

            foreach (var index in indices)
                vertex.Add(self.Vertices[index]);

            self.Vertices = vertex.ToArray();
            self.Indices = [];

            self.ComputeIndices();

            self.NotifyChanged(ChangeType.Geometry);
        }

        public static Geometry3D TransformToLine(this Geometry3D self)
        {
            if (self.Primitive != DrawPrimitive.Triangle)
                throw new NotSupportedException();

            var res = new Geometry3D();
            res.Primitive = DrawPrimitive.Line;
            if (self.Indices.Length > 0)
            {
                var srcI = 0;
                var dstI = 0;
                var newIndices = new uint[self.Indices.Length * 2];
                var newSpan = newIndices.AsSpan();
                var srcSpan = self.Indices.AsSpan();

                while (srcI < self.Indices!.Length)
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

                res.Vertices = self.Vertices;
                res.Indices = newIndices;
            }
            else
            {
                throw new NotSupportedException();
            }

            return res;
        }

        public static unsafe Vector3[] ExtractPositions(this Geometry3D self, bool useIndex = false)
        {
            if (!useIndex)
            {
                var result = new Vector3[self.Vertices.Length];
                var len = result.Length;
                fixed (Vector3* pDst = result)
                fixed (VertexData* pSrc = self.Vertices)
                {
                    for (var i = 0; i < len; i++)
                        pDst[i] = pSrc[i].Pos;
                }
                return result;
            }
            else
            {
                var result = new Vector3[self.Indices.Length];
                var len = result.Length;
                fixed (Vector3* pDst = result)
                fixed (VertexData* pSrc = self.Vertices)
                fixed (uint* pIdx = self.Indices)
                {
                    for (var i = 0; i < len; i++)
                        pDst[i] = pSrc[(int)pIdx[i]].Pos;
                }
                return result;
            }
        }

        public static void ComputeNormals(this Geometry3D self)
        {

            if (self.Primitive != DrawPrimitive.Triangle)
                throw new NotSupportedException();

            if (self.Indices.Length > 0)
            {
                var i = 0;
                while (i < self.Indices.Length)
                {
                    var i0 = self.Indices[i++];
                    var i1 = self.Indices[i++];
                    var i2 = self.Indices[i++];

                    var triangle = new Triangle3
                    {
                        V0 = self.Vertices[i0].Pos,
                        V1 = self.Vertices[i1].Pos,
                        V2 = self.Vertices[i2].Pos,
                    };

                    var normal = triangle.Normal();
                    self.Vertices[i0].Normal = normal;
                    self.Vertices[i1].Normal = normal;
                    self.Vertices[i2].Normal = normal;
                }
            }
            else
            {
                var i = 0;
                while (i < self.Vertices.Length)
                {
                    var i0 = i++;
                    var i1 = i++;
                    var i2 = i++;

                    var triangle = new Triangle3
                    {
                        V0 = self.Vertices[i0].Pos,
                        V1 = self.Vertices[i1].Pos,
                        V2 = self.Vertices[i2].Pos,
                    };

                    var normal = triangle.Normal();
                    self.Vertices[i0].Normal = normal;
                    self.Vertices[i1].Normal = normal;
                    self.Vertices[i2].Normal = normal;
                }
            }
            self.ActiveComponents |= VertexComponent.Normal;
            self.NotifyChanged(ChangeType.Geometry);
        }

        public static void ToTriangles(this Geometry3D self)
        {
            if (self.Primitive != DrawPrimitive.Quad)
                throw new NotSupportedException();

            var newIndices = new List<uint>(self.Indices.Length / 2 * 3);

            for (var i = 0; i < self.Indices.Length; i += 4)
            {
                // First triangle of the quad
                newIndices.Add(self.Indices[i]);
                newIndices.Add(self.Indices[i + 1]);
                newIndices.Add(self.Indices[i + 2]);

                // Second triangle of the quad
                newIndices.Add(self.Indices[i]);
                newIndices.Add(self.Indices[i + 2]);
                newIndices.Add(self.Indices[i + 3]);
            }

            self.Indices = newIndices.ToArray();
            self.Primitive = DrawPrimitive.Triangle;
            self.NotifyChanged(ChangeType.Geometry);
        }

        public static void EnsureIndices(this Geometry3D self)
        {
            if (self.Indices == null || self.Indices.Length == 0)
            {
                self.Indices = new uint[self.Vertices.Length];
                for (var i = 0; i < self.Vertices.Length; i++)
                    self.Indices[i] = (uint)i;
            }
            self.NotifyChanged(ChangeType.Geometry);
        }

        public static void SmoothNormals(this Geometry3D self)
        {
            SmoothNormals(self, 0, (uint)self.Vertices.Length - 1);
        }

        public static void SmoothNormals(this Geometry3D self, uint startIndex, uint endIndex, int decimals = 4)
        {
            if (self.Primitive != DrawPrimitive.Triangle)
                throw new NotSupportedException();

            Dictionary<Vector3, List<uint>> groups = [];

            for (var i = startIndex; i <= endIndex; i++)
            {
                var pos = self.Vertices[i].Pos.Round(decimals);

                if (!groups.TryGetValue(pos, out var list))
                {
                    list = [i];
                    groups[pos] = list;
                }
                else
                    list.Add(i);
            }
            foreach (var group in groups.Values)
            {
                if (group.Count > 1)
                {
                    var avg = Vector3.Zero;
                    var count = 0;
                    foreach (var index in group)
                    {
                        var normal = self.Vertices[index].Normal;
                        if (!normal.IsFinite())
                            continue;
                        avg += normal;
                        count++;
                    }

                    avg /= count;

                    foreach (var index in group)
                        self.Vertices[index].Normal = avg;
                }
            }
            self.NotifyChanged(ChangeType.Geometry);
        }

        public static IEnumerable<Triangle3> Triangles(this Geometry3D self)
        {
            if (self.Primitive != DrawPrimitive.Triangle)
                throw new NotSupportedException();

            if (self.Indices.Length > 0)
            {
                uint i = 0;
                while (i < self.Indices.Length)
                {
                    var triangle = new Triangle3
                    {
                        I0 = self.Indices[i++],
                        I1 = self.Indices[i++],
                        I2 = self.Indices[i++]
                    };

                    triangle.V0 = self.Vertices[triangle.I0].Pos;
                    triangle.V1 = self.Vertices[triangle.I1].Pos;
                    triangle.V2 = self.Vertices[triangle.I2].Pos;

                    yield return triangle;
                }
            }
            else
            {
                uint i = 0;
                while (i < self.Vertices.Length)
                {
                    var i0 = i++;
                    var i1 = i++;
                    var i2 = i++;

                    var triangle = new Triangle3
                    {
                        I0 = i0,
                        I1 = i1,
                        I2 = i2,
                        V0 = self.Vertices[i0].Pos,
                        V1 = self.Vertices[i1].Pos,
                        V2 = self.Vertices[i2].Pos,
                    };

                    yield return triangle;

                }
            }
        }

        public static unsafe void ComputeTangents(this Geometry3D self)
        {
            if (self.Primitive != DrawPrimitive.Triangle)
                throw new NotSupportedException();

            var vertexCount = self.Vertices.Length;
            var indexCount = self.Indices.Length;

            // Arrays to accumulate the tangent and bitangent vectors
            var tan1 = new Vector3[vertexCount];
            var tan2 = new Vector3[vertexCount];

            fixed (Vector3* pTan1 = tan1)
            fixed (Vector3* pTan2 = tan2)
            fixed (uint* pIndex = self.Indices)
            fixed (VertexData* pVertex = self.Vertices)
            {
                // Iterate over each triangle
                for (var i = 0; i < indexCount; i += 3)
                {
                    var i1 = pIndex[i];
                    var i2 = pIndex[i + 1];
                    var i3 = pIndex[i + 2];

                    var v1 = pVertex[i1].Pos;
                    var v2 = pVertex[i2].Pos;
                    var v3 = pVertex[i3].Pos;

                    var w1 = pVertex[i1].UV;
                    var w2 = pVertex[i2].UV;
                    var w3 = pVertex[i3].UV;

                    var x1 = v2.X - v1.X;
                    var y1 = v2.Y - v1.Y;
                    var z1 = v2.Z - v1.Z;

                    var x2 = v3.X - v1.X;
                    var y2 = v3.Y - v1.Y;
                    var z2 = v3.Z - v1.Z;

                    var s1 = w2.X - w1.X;
                    var t1 = w2.Y - w1.Y;

                    var s2 = w3.X - w1.X;
                    var t2 = w3.Y - w1.Y;

                    var r = (s1 * t2 - s2 * t1);
                    var f = r == 0.0f ? 0.0f : 1.0f / r;

                    var sdir = new Vector3(
                        (t2 * x1 - t1 * x2) * f,
                        (t2 * y1 - t1 * y2) * f,
                        (t2 * z1 - t1 * z2) * f
                    );

                    var tdir = new Vector3(
                        (s1 * x2 - s2 * x1) * f,
                        (s1 * y2 - s2 * y1) * f,
                        (s1 * z2 - s2 * z1) * f
                    );

                    // Accumulate the tangent and bitangent vectors
                    pTan1[i1] += sdir;
                    pTan1[i2] += sdir;
                    pTan1[i3] += sdir;

                    pTan2[i1] += tdir;
                    pTan2[i2] += tdir;
                    pTan2[i3] += tdir;
                }

                // Orthogonalize and normalize the tangent vectors
                for (var i = 0; i < vertexCount; ++i)
                {
                    var n = pVertex[i].Normal;
                    var t = pTan1[i];

                    // Gram-Schmidt orthogonalization
                    MathUtils.OrthoNormalize(ref n, ref t);

                    // Calculate the handedness (w component)
                    var c = Vector3.Cross(n, t);
                    var w = (Vector3.Dot(c, pTan2[i]) < 0.0f) ? -1.0f : 1.0f;

                    // Set the tangent with the calculated w component
                    pVertex[i].Tangent = new Vector4(t.X, t.Y, t.Z, w);
                }
            }

            self.ActiveComponents |= VertexComponent.Tangent;
            self.NotifyChanged(ChangeType.Geometry);
        }


        public static void SetSkinData<T>(this SkinnedGeometry3D self, SkinAssignDelegate<T> selector, T[] array)
        {
            if (self.Skin == null)
                self.Skin = new SkinData[array.Length];

            if (self.Skin.Length < array.Length)
            {
                var newArray = self.Skin;
                Array.Resize(ref newArray, array.Length);
                self.Skin = newArray;
            }

            for (var i = 0; i < array.Length; i++)
                selector(ref self.Skin[i], array[i]);

            self.NotifyChanged(ChangeType.Geometry);
        }

        public static void SetVertexData<T>(this Geometry3D self, VertexAssignDelegate<T> selector, T[] array)
        {
            if (self.Vertices == null)
                self.Vertices = new VertexData[array.Length];

            if (self.Vertices.Length < array.Length)
            {
                var newArray = self.Vertices;
                Array.Resize(ref newArray, array.Length);
                self.Vertices = newArray;
            }

            for (var i = 0; i < array.Length; i++)
                selector(ref self.Vertices[i], array[i]);

            self.NotifyChanged(ChangeType.Geometry);
        }

        public static Bounds3 ComputeBounds(this Geometry3D self, Matrix4x4 transform)
        {
            if (self.Vertices != null)
            {
                var builder = new Bounds3Builder();

                if (transform.IsIdentity)
                {
                    foreach (var v in self.Vertices)
                        builder.Add(v.Pos);
                }
                else
                {
                    foreach (var v in self.Vertices)
                        builder.Add(v.Pos.Transform(transform));
                }

                return builder.Result;
            }

            return Bounds3.Zero;
        }

        public static void EnsureCCW(this Geometry3D self)
        {
            if (self.Indices.Length == 0)
                throw new NotSupportedException();

            var i = 0;

            var vSpan = new Span<VertexData>(self.Vertices);
            var iSpan = new Span<uint>(self.Indices);

            while (i < self.Indices.Length)
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

        public static void ComputeIndices(this Geometry3D self, int decimals = 5)
        {
            var sourceVertices = self.Vertices!;
            var sourceIndices = self.Indices;

            var map = new Dictionary<Vector256<float>, uint>();
            var newVertices = new List<VertexData>(sourceVertices.Length);
            map.EnsureCapacity(sourceVertices.Length);

            Vector256<float> Hash(in VertexData vert)
            {
                var pos = vert.Pos.Round(decimals);

                return Vector256.Create(
                    pos.X,
                    pos.Y,
                    pos.Z,
                    vert.Normal.X,
                    vert.Normal.Y,
                    vert.Normal.Z,
                    vert.UV.X,
                    vert.UV.Y);
            }

            uint GetIndex(in VertexData vert)
            {
                var hash = Hash(vert);

                if (map.TryGetValue(hash, out var index))
                    return index;

                index = (uint)newVertices.Count;
                map.Add(hash, index);
                newVertices.Add(vert);

                return index;
            }

            if (sourceIndices != null && sourceIndices.Length > 0 && sourceIndices.Length != sourceVertices.Length)
            {
                var newIndices = new uint[sourceIndices.Length];

                for (var i = 0; i < sourceIndices.Length; i++)
                    newIndices[i] = GetIndex(sourceVertices[(int)sourceIndices[i]]);

                self.Indices = newIndices;
            }
            else
            {
                var newIndices = new uint[sourceVertices.Length];

                for (var i = 0; i < sourceVertices.Length; i++)
                    newIndices[i] = GetIndex(sourceVertices[i]);

                self.Indices = newIndices;
            }

            self.Vertices = newVertices.ToArray();

            self.NotifyChanged(ChangeType.Geometry);
        }

        #endregion

        #region CAMERA

        public static Ray3 ScreenToRay(this Camera self, Vector2 screenPoint)
        {
            var normPoint = new Vector3(
                2.0f * screenPoint.X / self.ViewSize.Width - 1.0f,
                1.0f - 2.0f * screenPoint.Y / self.ViewSize.Height,
                -1
            );

            var dirEye = Vector4.Transform(new Vector4(normPoint, 1.0f), self.ProjectionInverse);
            dirEye.W = 0;

            var dirWorld = Vector4.Transform(dirEye, self.WorldMatrix);

            return new Ray3
            {
                Origin = self.WorldPosition,
                Direction = new Vector3(dirWorld.X, dirWorld.Y, dirWorld.Z).Normalize()
            };
        }

        public static Vector2 WorldToScreen(this Camera self, Vector3 world)
        {
            var size = new Vector2(self.ViewSize.Width, self.ViewSize.Height);
            var proj = (world.Project(self.ViewProjection).ToVector2() + Vector2.One) * 0.5f;
            return new Vector2(proj.X, proj.Y) * size;
        }

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

        public static Vector3 Project(this Camera self, Vector3 worldPoint)
        {
            return worldPoint.Project(self.ViewProjection);
        }

        public static IEnumerable<Vector3> Project(this Camera self, IEnumerable<Vector3> worldPoints)
        {
            var viewProj = self.ViewProjection;

            foreach (var vertex in worldPoints)
                yield return vertex.Project(viewProj);
        }

        public static Vector3 Unproject(this Camera self, Vector3 viewPoint)
        {
            var viewProjInv = self.ViewProjectionInverse;
            return viewPoint.Project(viewProjInv);
        }

        public static IEnumerable<Vector3> Unproject(this Camera self, IEnumerable<Vector3> viewPoint)
        {
            var viewProjInv = self.ViewProjectionInverse;
            foreach (var vertex in viewPoint)
                yield return vertex.Project(viewProjInv);
        }

        public static Vector3[] FrustumPoints(this Camera self, float? farPlane = null)
        {
            static void Fill(Camera self, Matrix4x4 viewProjInv, Vector3[] corners, int offset, float? farPlane)
            {
                var n0 = new Vector3(-1, -1, 0).Project(viewProjInv);
                var n1 = new Vector3(1, -1, 0).Project(viewProjInv);
                var n2 = new Vector3(-1, 1, 0).Project(viewProjInv);
                var n3 = new Vector3(1, 1, 0).Project(viewProjInv);

                var f0 = new Vector3(-1, -1, 1).Project(viewProjInv);
                var f1 = new Vector3(1, -1, 1).Project(viewProjInv);
                var f2 = new Vector3(-1, 1, 1).Project(viewProjInv);
                var f3 = new Vector3(1, 1, 1).Project(viewProjInv);

                if (farPlane.HasValue)
                {
                    var t = (farPlane.Value - self.Near) / (self.Far - self.Near);

                    f0 = n0 + (f0 - n0) * t;
                    f1 = n1 + (f1 - n1) * t;
                    f2 = n2 + (f2 - n2) * t;
                    f3 = n3 + (f3 - n3) * t;
                }

                corners[offset + 0] = n0;
                corners[offset + 1] = n1;
                corners[offset + 2] = n2;
                corners[offset + 3] = n3;

                corners[offset + 4] = f0;
                corners[offset + 5] = f1;
                corners[offset + 6] = f2;
                corners[offset + 7] = f3;
            }

            var isStereo = self.Eyes != null && self.Eyes.Length > 1;

            var corners = new Vector3[isStereo ? 16 : 8];

            Fill(self, isStereo ? self.Eyes![0].ViewProjInv : self.ViewProjectionInverse, corners, 0, farPlane);

            if (isStereo)
                Fill(self, self.Eyes![1].ViewProjInv, corners, 8, farPlane);

            return corners;
        }

        public static void FrustumPlanes(this Matrix4x4 viewProj, Span<Plane> planes)
        {
            if (planes.Length < 6)
                throw new ArgumentException("Plane buffer must contain at least 6 elements.", nameof(planes));

            planes[0] = new Plane(
                viewProj.M14 + viewProj.M11,
                viewProj.M24 + viewProj.M21,
                viewProj.M34 + viewProj.M31,
                viewProj.M44 + viewProj.M41
            ).Normalize();

            planes[1] = new Plane(
                viewProj.M14 - viewProj.M11,
                viewProj.M24 - viewProj.M21,
                viewProj.M34 - viewProj.M31,
                viewProj.M44 - viewProj.M41
            ).Normalize();

            planes[2] = new Plane(
                viewProj.M14 - viewProj.M12,
                viewProj.M24 - viewProj.M22,
                viewProj.M34 - viewProj.M32,
                viewProj.M44 - viewProj.M42
            ).Normalize();

            planes[3] = new Plane(
                viewProj.M14 + viewProj.M12,
                viewProj.M24 + viewProj.M22,
                viewProj.M34 + viewProj.M32,
                viewProj.M44 + viewProj.M42
            ).Normalize();

            planes[4] = new Plane(
                viewProj.M13,
                viewProj.M23,
                viewProj.M33,
                viewProj.M43
            ).Normalize();

            planes[5] = new Plane(
                viewProj.M14 - viewProj.M13,
                viewProj.M24 - viewProj.M23,
                viewProj.M34 - viewProj.M33,
                viewProj.M44 - viewProj.M43
            ).Normalize();
        }

        public static Plane[] FrustumPlanes(
            this Camera self,
            Plane[]? planes,
            out int count,
            bool fullStereo = true)
        {
            var stereo =
                fullStereo &&
                self.IsStereo &&
                self.Eyes?.Length > 1;

            count = stereo ? 12 : 6;

            if (planes == null || planes.Length < count)
                Array.Resize(ref planes, count);

            if (stereo)
            {
                self.Eyes![0].ViewProj.FrustumPlanes(planes.AsSpan(0, 6));
                self.Eyes[1].ViewProj.FrustumPlanes(planes.AsSpan(6, 6));
            }
            else
                self.ViewProjection.FrustumPlanes(planes.AsSpan(0, 6));

            return planes;
        }

        #endregion

        #region TRANSFORM

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPosition(this Transform3D self, float x, float y, float z)
        {
            self.Position = new Vector3(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetScale(this Transform3D self, float x, float y, float z)
        {
            self.Scale = new Vector3(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetScale(this Transform3D self, float value)
        {
            self.Scale = new Vector3(value, value, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPositionZ(this Transform3D self, float value)
        {
            self.Position = new Vector3(self.Position.X, self.Position.Y, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPositionX(this Transform3D self, float value)
        {
            self.Position = new Vector3(value, self.Position.Y, self.Position.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPositionY(this Transform3D self, float value)
        {
            self.Position = new Vector3(self.Position.X, value, self.Position.Z);
        }

        public static Pose3 ToPose(this Transform3D self)
        {
            return new Pose3
            {
                Orientation = self.Orientation,
                Position = self.Position
            };
        }

        #endregion

        #region MATERIAL

        public static void WriteStencilMask(this Material self, byte channel, bool isOn)
        {
            int curValue = self.WriteStencil ?? 0;
            if (isOn)
                curValue |= channel;
            else
                curValue &= ~channel;

            self.WriteStencil = curValue == 0 ? null : (byte)curValue;
        }

        public static void UpdateColor(this TriangleMesh self, Color color)
        {
            foreach (var material in self.Materials.OfType<IColorSource>())
            {
                if (material.Color != color)
                {
                    material.Color = color;
                    ((Material)material).NotifyChanged(ChangeType.Render);
                }
            }

        }

        public static bool UpdateColor(this Material self, Color color)
        {
            var src = (IColorSource)self;

            if ((Vector4)src.Color != (Vector4)color)
            {
                ((IColorSource)self).Color = color;
                self.NotifyChanged(ChangeType.Render);
                return true;
            }
            return false;
        }

        #endregion

        #region MISC

        public static Poly2 ToPoly2(this ICurve2D curve, int numPoints, bool isClosed)
        {
            var points = new Vector2[numPoints];

            for (var i = 0; i < numPoints; i++)
                points[i] = curve.GetPointAtTime(1f / numPoints * i);

            return new Poly2
            {
                Points = points,
                IsClosed = isClosed
            };
        }

        public static unsafe void UpdateElement<T>(this IBuffer<T> self, T element, int index)
        {
            var span = new ReadOnlySpan<T>(&element, 1);
            self.UpdateRange(span, index);
        }

        public static void SaveAs(this Texture2D self, string path, SKEncodedImageFormat format = SKEncodedImageFormat.Png, int quality = 100)
        {
            using var bmp = ImageUtils.ToBitmap(self.Data![0], false);
            if (bmp == null)
                return;
            using var enc = bmp.Encode(format, quality);
            if (File.Exists(path))
                File.Delete(path);
            using var file = File.OpenWrite(path);
            enc.SaveTo(file);
        }

        public static void Update<T>(this IList<T> self, RenderContext ctx) where T : IRenderUpdate
        {
            var count = self.Count;
            for (var i = 0; i < count; i++)
                self[i].Update(ctx);
        }

        public static void Update<T>(this IEnumerable<T> self, RenderContext ctx, bool safeMode) where T : IRenderUpdate
        {
            if (safeMode)
                self = self.ToArray();

            foreach (var item in self)
                item.Update(ctx);

            //target.ForeachSafe(a => a.Update(ctx));
        }

        public static void Reset<T>(this IEnumerable<T> self, bool onlySelf) where T : IRenderUpdate
        {
            //target.ForeachSafe(a => a.Reset(onlySelf));
        }

        public static T Load<T>(this AssetLoader self, string fileUri, IAssetLoaderOptions? options = null) where T : EngineObject
        {
            return (T)self.Load(new Uri(fileUri, UriKind.Absolute), typeof(T), null, options);
        }

        public static string? ImageMimeToExtension(string? mimeType)
        {
            if (string.IsNullOrWhiteSpace(mimeType))
                return null;

            return mimeType.Trim().ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/bmp" => ".bmp",
                "image/x-bmp" => ".bmp",
                "image/tiff" => ".tiff",
                "image/svg+xml" => ".svg",
                "image/x-icon" => ".ico",
                "image/vnd.microsoft.icon" => ".ico",
                "image/avif" => ".avif",
                "image/heic" => ".heic",
                "image/heif" => ".heif",
                _ => null
            };
        }

        public static Uri GetMimeUri(this AssetLoader self, string mimeType)
        {
            return new Uri($"stream://mime/{mimeType}.{ImageMimeToExtension(mimeType)}");
        }

        #endregion

        #region TEXTURE2D

        public static void Generate(this Texture2D texture, bool warmUp = false)
        {
            var render = EngineApp.Current.Renderer;
            render.LoadTexture(texture);
        }

        public static void PrepareTexture(this ShaderUpdateBuilder builder, Texture? texture)
        {
            if (texture == null)
                return;

            if (texture.Format.IsSrgb())
                builder.AddFeature("TEXTURE_IS_SRGB");

            if (texture.ForceSrgb)
                builder.AddFeature("TEXTURE_FORCE_SRGB");
        }

        public static void LoadTextureFixSrgb(this ShaderUpdateBuilder builder, UpdateAction<Texture2D> value, int slot)
        {
            builder.ExecuteAction((ctx, up) => up.LoadTextureFixSrgb(ctx, value(ctx), slot));
        }

        public static void LoadTextureFixSrgb(this IUniformProvider uniform, UpdateShaderContext ctx, Texture texture, int slot)
        {
            //DO NOT MERGE LoadTexture, LoadSampler MUST BE AFTER in the other branch

            var isSrgb = texture.Format.IsSrgb();

            var isDecodeEnabled = !ctx.NeedSrgbEncode;

            if (isSrgb && texture.Sampler != null)
            {
                if (texture.Sampler.DecodeSrgb != isDecodeEnabled)
                {
                    texture.Sampler.DecodeSrgb = isDecodeEnabled;
                    texture.Sampler.Invalidate();
                }
            }

            uniform.LoadTexture(texture, slot);

            if (!isDecodeEnabled && isSrgb && texture.Sampler == null)
            {
                var sampler = TextureSamplerFactory.DisableSrgbDecode(texture);

                uniform.LoadSampler(sampler, slot);
            }

        }

        public static void Update(this TextureSampler sampler, Texture texture)
        {
            var changed = false;

            if (sampler.MinFilter != texture.MinFilter)
            {
                sampler.MinFilter = texture.MinFilter;
                changed = true;
            }

            if (sampler.MagFilter != texture.MagFilter)
            {
                sampler.MagFilter = texture.MagFilter;
                changed = true;
            }

            if (sampler.WrapS != texture.WrapS)
            {
                sampler.WrapS = texture.WrapS;
                changed = true;
            }

            if (texture is Texture2D tex2d)
            {
                if (sampler.MaxAnisotropy != tex2d.MaxAnisotropy)
                {
                    sampler.MaxAnisotropy = tex2d.MaxAnisotropy;
                    changed = true;
                }

                if (sampler.WrapT != tex2d.WrapT)
                {
                    sampler.WrapT = tex2d.WrapT;
                    changed = true;
                }

                if (sampler.BorderColor != tex2d.BorderColor)
                {
                    sampler.BorderColor = tex2d.BorderColor;
                    changed = true;
                }
            }

            if (texture is Texture3D tex3d)
            {
                if (sampler.WrapR != tex3d.WrapR)
                {
                    sampler.WrapR = tex3d.WrapR;
                    changed = true;
                }
            }

            if (changed)
                sampler.Invalidate();
        }

        #endregion
    }
}
