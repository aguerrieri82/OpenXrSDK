using SkiaSharp;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
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

        public static T GetOrCreateProp<T>(this EngineObject self, string name, Func<T> create)
        {
            var result = self.GetProp<T?>(name);
            if (result == null)
            {
                result = create();
                self.SetProp(name, result);
            }
            return result;
        }

        #endregion

        #region OBJECT3D

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
                    mat.NotifyChanged(ObjectChangeType.Render);
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

            /*
             WORKING 
            pose.Position -= Vector3.Transform(self.Transform.LocalPivot, pose.Orientation);

            self.WorldMatrix = Matrix4x4.CreateScale(self.Transform.Scale) *
                         Matrix4x4.CreateFromQuaternion(pose.Orientation) *
                         Matrix4x4.CreateTranslation(pose.Position);


            var curPivot = Vector3.Zero;

            self.BeginUpdate();

            if (fromOrigin)
            {
                curPivot = self.Transform.LocalPivot;
                if (curPivot != Vector3.Zero)
                    self.Transform.SetLocalPivot(Vector3.Zero, true);
            }

            self.WorldPosition = pose.Position;
            self.WorldOrientation = pose.Orientation;

            if (curPivot != Vector3.Zero)
                self.Transform.SetLocalPivot(curPivot, true);

            self.EndUpdate();

            */
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pose3 GetWorldPose(this Object3D self, bool fromOrigin = false)
        {
            var result = new Pose3
            {
                Orientation = self.WorldOrientation,
                Position = fromOrigin ? self.ToWorld(Vector3.Zero) : self.WorldPosition
            };

            /*
            var result = new Pose3
            {
                Orientation = self.WorldOrientation,
                Position = self.WorldPosition
            };

            if (fromOrigin)
                result.Position -= self.Transform.LocalPivot;
            */

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

                self.SetProp("ActiveTool", value);
            }

            else if (curTool == value)
                self.SetProp("ActiveTool", null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IObjectTool? GetActiveTool(this Object3D self)
        {
            return self.GetProp<IObjectTool?>("ActiveTool");
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

        public static void RayCollisions(this Scene3D self, Ray3 ray, ConcurrentBag<Collision> result, IEnumerable<ICollider3D>? colliders = null)
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
                var collision = collider.CollideWith(ray);
                if (collision != null)
                    result.Add(collision);
            });
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
            foreach (var child in self.Children)
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

        public static void OpenScene(this EngineApp self, string name)
        {
            self.OpenScene(self.Scenes.Single(s => s.Name == name));
        }

        #endregion

        #region GEOMETRY

        public delegate void VertexAssignDelegate<T>(ref VertexData vertexData, T value);

        public static void Rebuild(this Geometry3D self, IEnumerable<Triangle3> triangles)
        {
            var vertex = new List<VertexData>();

            var indices = triangles.SelectMany(a => a.Indices);

            foreach (var index in indices)
                vertex.Add(self.Vertices[index]);

            self.Vertices = vertex.ToArray();
            self.ComputeIndices();
            self.NotifyChanged(ObjectChangeType.Geometry);
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
                int i = 0;
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
                int i = 0;
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
            self.NotifyChanged(ObjectChangeType.Geometry);
        }

        public static void ToTriangles(this Geometry3D self)
        {
            if (self.Primitive != DrawPrimitive.Quad)
                throw new NotSupportedException();

            var newIndices = new List<uint>(self.Indices.Length / 2 * 3);

            for (int i = 0; i < self.Indices.Length; i += 4)
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
            self.NotifyChanged(ObjectChangeType.Geometry);
        }

        public static void EnsureIndices(this Geometry3D self)
        {
            if (self.Indices == null || self.Indices.Length == 0)
            {
                self.Indices = new uint[self.Vertices.Length];
                for (var i = 0; i < self.Vertices.Length; i++)
                    self.Indices[i] = (uint)i;
            }
            self.NotifyChanged(ObjectChangeType.Geometry);
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
                    int count = 0;
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
            self.NotifyChanged(ObjectChangeType.Geometry);
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

            int vertexCount = self.Vertices.Length;
            int indexCount = self.Indices.Length;

            // Arrays to accumulate the tangent and bitangent vectors
            var tan1 = new Vector3[vertexCount];
            var tan2 = new Vector3[vertexCount];

            fixed (Vector3* pTan1 = tan1)
            fixed (Vector3* pTan2 = tan2)
            fixed (uint* pIndex = self.Indices)
            fixed (VertexData* pVertex = self.Vertices)
            {
                // Iterate over each triangle
                for (int i = 0; i < indexCount; i += 3)
                {
                    uint i1 = pIndex[i];
                    uint i2 = pIndex[i + 1];
                    uint i3 = pIndex[i + 2];

                    Vector3 v1 = pVertex[i1].Pos;
                    Vector3 v2 = pVertex[i2].Pos;
                    Vector3 v3 = pVertex[i3].Pos;

                    Vector2 w1 = pVertex[i1].UV;
                    Vector2 w2 = pVertex[i2].UV;
                    Vector2 w3 = pVertex[i3].UV;

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
                    pTan1[i1] += sdir;
                    pTan1[i2] += sdir;
                    pTan1[i3] += sdir;

                    pTan2[i1] += tdir;
                    pTan2[i2] += tdir;
                    pTan2[i3] += tdir;
                }

                // Orthogonalize and normalize the tangent vectors
                for (int i = 0; i < vertexCount; ++i)
                {
                    var n = pVertex[i].Normal;
                    var t = pTan1[i];

                    // Gram-Schmidt orthogonalization
                    MathUtils.OrthoNormalize(ref n, ref t);

                    // Calculate the handedness (w component)
                    Vector3 c = Vector3.Cross(n, t);
                    float w = (Vector3.Dot(c, pTan2[i]) < 0.0f) ? -1.0f : 1.0f;

                    // Set the tangent with the calculated w component
                    pVertex[i].Tangent = new Vector4(t.X, t.Y, t.Z, w);
                }
            }

            self.ActiveComponents |= VertexComponent.Tangent;
            self.NotifyChanged(ObjectChangeType.Geometry);
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

            self.NotifyChanged(ObjectChangeType.Geometry);
        }

        public static Bounds3 ComputeBounds(this Geometry3D self, Matrix4x4 transform)
        {
            if (self.Vertices != null)
                return self.ExtractPositions().ComputeBounds(transform);

            return new Bounds3();
        }


        public static void EnsureCCW(this Geometry3D self)
        {
            if (self.Indices.Length == 0)
                throw new NotSupportedException();

            int i = 0;

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

            var hasesh = new Dictionary<Vector512<float>, uint>();
            var newVertices = new List<VertexData>(self.Vertices.Length);

            Vector512<float> Hash(VertexData vert)
            {
                var pos2 = vert.Pos.Round(decimals);
                return Vector512.Create(pos2.X, pos2.Y, pos2.Z, vert.Normal.X, vert.Normal.Y, vert.Normal.Z, vert.UV.X, vert.UV.Y, 0, 0, 0, 0, 0, 0, 0, 0);
            }

            foreach (var vert in self.Vertices)
            {
                var hash = Hash(vert);
                if (!hasesh.ContainsKey(hash))
                {
                    hasesh[hash] = (uint)newVertices.Count;
                    newVertices.Add(vert);
                }

            }

            var indices = new uint[self.Vertices.Length];
            for (var i = 0; i < indices.Length; i++)
                indices[i] = hasesh[Hash(self.Vertices[i])];

            self.Indices = indices;
            self.Vertices = newVertices.ToArray();

            self.NotifyChanged(ObjectChangeType.Geometry);
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

        public static Vector3[] FrustumPoints(this Camera self)
        {
            var viewProjInvLeft = self.ViewProjectionInverse;

            var isStereo = self.Eyes != null && self.Eyes.Length > 1;

            Vector3[] corners = new Vector3[isStereo ? 16 : 8];

            corners[0] = new Vector3(-1, -1, 0).Project(viewProjInvLeft);
            corners[1] = new Vector3(1, -1, 0).Project(viewProjInvLeft);
            corners[2] = new Vector3(-1, 1, 0).Project(viewProjInvLeft);
            corners[3] = new Vector3(1, 1, 0).Project(viewProjInvLeft);

            corners[4] = new Vector3(-1, -1, 1).Project(viewProjInvLeft);
            corners[5] = new Vector3(1, -1, 1).Project(viewProjInvLeft);
            corners[6] = new Vector3(-1, 1, 1).Project(viewProjInvLeft);
            corners[7] = new Vector3(1, 1, 1).Project(viewProjInvLeft);

            if (isStereo)
            {
                Matrix4x4.Invert(self.Eyes![1].ViewProj, out var viewProjInvRight);

                corners[8] = new Vector3(-1, -1, 0).Project(viewProjInvRight);
                corners[9] = new Vector3(1, -1, 0).Project(viewProjInvRight);
                corners[10] = new Vector3(-1, 1, 0).Project(viewProjInvRight);
                corners[11] = new Vector3(1, 1, 0).Project(viewProjInvRight);

                corners[12] = new Vector3(-1, -1, 1).Project(viewProjInvRight);
                corners[13] = new Vector3(1, -1, 1).Project(viewProjInvRight);
                corners[14] = new Vector3(-1, 1, 1).Project(viewProjInvRight);
                corners[15] = new Vector3(1, 1, 1).Project(viewProjInvRight);
            }

            return corners;
        }

        public static IList<Plane> FrustumPlanes(this Camera self, Plane[] planes)
        {
            var viewProjLeft = self.ViewProjection;
            var viewProjRight = viewProjLeft;

            if (self.Eyes != null && self.Eyes.Length > 1 && self.IsStereo)
                viewProjRight = self.Eyes[1].ViewProj;

            // Left plane
            planes[0] = new Plane(
                viewProjLeft.M14 + viewProjLeft.M11,
                viewProjLeft.M24 + viewProjLeft.M21,
                viewProjLeft.M34 + viewProjLeft.M31,
                viewProjLeft.M44 + viewProjLeft.M41
            );

            // Right plane
            planes[1] = new Plane(
                viewProjRight.M14 - viewProjRight.M11,
                viewProjRight.M24 - viewProjRight.M21,
                viewProjRight.M34 - viewProjRight.M31,
                viewProjRight.M44 - viewProjRight.M41
            );

            // Top plane
            planes[2] = new Plane(
                viewProjLeft.M14 - viewProjLeft.M12,
                viewProjLeft.M24 - viewProjLeft.M22,
                viewProjLeft.M34 - viewProjLeft.M32,
                viewProjLeft.M44 - viewProjLeft.M42
            );

            // Bottom plane
            planes[3] = new Plane(
                viewProjLeft.M14 + viewProjLeft.M12,
                viewProjLeft.M24 + viewProjLeft.M22,
                viewProjLeft.M34 + viewProjLeft.M32,
                viewProjLeft.M44 + viewProjLeft.M42
            );

            // Near plane
            planes[4] = new Plane(
                viewProjLeft.M13,
                viewProjLeft.M23,
                viewProjLeft.M33,
                viewProjLeft.M43
            );

            // Far plane
            planes[5] = new Plane(
                viewProjLeft.M14 - viewProjLeft.M13,
                viewProjLeft.M24 - viewProjLeft.M23,
                viewProjLeft.M34 - viewProjLeft.M33,
                viewProjLeft.M44 - viewProjLeft.M43
            );

            for (int i = 0; i < 6; i++)
                planes[i] = Plane.Normalize(planes[i]);

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
                    ((Material)material).NotifyChanged(ObjectChangeType.Render);
                }
            }

        }

        public static void UpdateColor(this Material self, Color color)
        {
            var src = (IColorSource)self;

            if ((Vector4)src.Color != (Vector4)color)
            {
                ((IColorSource)self).Color = color;
                self.NotifyChanged(ObjectChangeType.Render);
            }

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


        #endregion
    }
}
