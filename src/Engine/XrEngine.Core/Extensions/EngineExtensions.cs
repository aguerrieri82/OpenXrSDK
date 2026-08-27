using SkiaSharp;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using XrEngine.Helpers;
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

        extension(EngineObject self)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Is(EngineObjectFlags flags)
            {
                return (self.Flags & flags) == flags;
            }

            public bool TryFeature<T>([NotNullWhen(true)] out T? result) where T : class
            {
                result = self.Feature<T>();
                return result != null;
            }

            public void SetFlag(EngineObjectFlags flag, bool isSet)
            {
                if (isSet)
                    self.Flags |= flag;
                else
                    self.Flags &= ~flag;
            }

            public T EnsureComponent<T>() where T : IComponent, new()
            {
                if (!self.TryComponent<T>(out var result))
                    result = self.AddComponent<T>();
                return result;
            }

            public T AddComponent<T>() where T : IComponent, new()
            {
                var result = new T();
                self.AddComponent(result);
                return result;
            }

            public IEnumerable<T> Components<T>()
            {
                return self.Components().OfType<T>();
            }

            public T Component<T>() where T : IComponent
            {
                return self.Components<T>().Single();
            }

            public bool TryComponent<T>([NotNullWhen(true)] out T? result) where T : IComponent
            {
                result = self.Components<T>().FirstOrDefault();
                return result != null;
            }

            public void SetProp<T>(string propName, T value)
            {
                self.SetProp(new DynamicProp(propName), value);
            }

            public T GetProp<T>(string propName)
            {
                return self.GetProp<T>(new DynamicProp(propName))!;
            }

            public T GetOrCreateProp<T>(int propId, Func<T> create)
            {
                var result = self.GetProp<T?>(propId);
                if (result == null)
                {
                    result = create();
                    self.SetProp(propId, result);
                }
                return result;
            }
        }
        extension<T>(T self) where T : EngineObject
        {
            public Behavior<T> AddBehavior(Action<T, RenderContext> action)
            {
                var result = new LambdaBehavior<T>(action);
                self.AddComponent(result);
                return result;
            }
        }

        #endregion

        #region OBJECT3D

        extension(Object3D self)
        {
            public IEnumerable<T> ComponentsDeep<T>()
            {
                return self.DescendantsOrSelf().SelectMany(a => a.Components<T>());
            }

            public void Remove()
            {
                self.Parent?.RemoveChild(self);
            }

            public void PropagateTransform()
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

            public void UseEnvDepth(bool value)
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

            public void CastShadows(bool value)
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

            public IEnumerable<T> MaterialsDeep<T>() where T : IMaterial
            {
                return self.DescendantsOrSelf()
                    .OfType<TriangleMesh>()
                    .SelectMany(a => a.Materials)
                    .OfType<T>();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 ToLocal(Vector3 worldPoint)
            {
                return worldPoint.Transform(self.WorldMatrixInverse);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 ToWorld(Vector3 localPoint)
            {
                return localPoint.Transform(self.WorldMatrix);
            }

            public void SetWorldPoseIfChanged(Pose3 pose, bool fromOrigin = false, float epsilonP = 0.001f, float epsilonO = 0.001f)
            {
                var curPose = self.GetWorldPose(fromOrigin);
                if (!curPose.IsSimilar(pose))
                    SetWorldPose(self, pose, fromOrigin);
            }

            public void SetWorldPose(Pose3 pose, bool fromOrigin = false)
            {
                self.WorldOrientation = pose.Orientation;
                if (fromOrigin)
                    self.MoveLocalToWorld(Vector3.Zero, pose.Position);
                else
                    self.WorldPosition = pose.Position;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Pose3 GetWorldPose(bool fromOrigin = false)
            {
                var result = new Pose3
                {
                    Orientation = self.WorldOrientation,
                    Position = fromOrigin ? self.ToWorld(Vector3.Zero) : self.WorldPosition
                };

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Pose3 GetLocalPose()
            {
                return new Pose3
                {
                    Orientation = self.Transform.Orientation,
                    Position = self.Transform.Position
                };
            }

            public void MoveLocalToWorld(Vector3 localPos, Vector3 worldPos)
            {
                var localPosAdjusted = (localPos - self.Transform.LocalPivot) * self.Transform.Scale;

                var rotatedLocalPos = localPosAdjusted.Transform(self.Transform.Orientation);

                if (self.Parent != null)
                    worldPos = worldPos.Transform(self.Parent.WorldMatrixInverse);

                self.Transform.Position = worldPos - rotatedLocalPos;
            }

            public void SetActiveTool(IObjectTool value, bool isActive)
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
            public IObjectTool? GetActiveTool()
            {
                return self.GetProp<IObjectTool?>(EngineProps.ActiveTool);
            }

            public IEnumerable<Object3D> DescendantsOrSelf()
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

            public IEnumerable<T> DescendantsOrSelfComponents<T>()
            {
                foreach (var obj in self.DescendantsOrSelf())
                {
                    foreach (var comp in obj.Components<IComponent>().OfType<T>())
                        yield return comp;
                }
            }

            public IEnumerable<Group3D> Ancestors()
            {
                var curItem = self.Parent;

                while (curItem != null)
                {
                    yield return curItem;
                    curItem = curItem.Parent;
                }
            }

            public IEnumerable<Object3D> AncestorsOrSelf()
            {
                return new Object3D[] { self }.Concat(self.Ancestors());
            }

            public T? FindAncestor<T>() where T : Group3D
            {
                return self.Ancestors().OfType<T>().FirstOrDefault();
            }

            public T? FeatureDeep<T>() where T : class
            {
                return self
                    .DescendantsOrSelfWithFeature<T>()
                    .FirstOrDefault()
                    .Feature;
            }

            public IEnumerable<ObjectFeature<T>> DescendantsOrSelfWithFeature<T>() where T : class
            {
                return self.DescendantsOrSelf().Features<T>();
            }
        }
        extension<T>(IEnumerable<T> self) where T : Object3D
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IEnumerable<T> Visible()
            {
                return self.Where(a => a.IsVisible);
            }
        }

        #endregion

        #region SCENE

        extension(Scene3D self)
        {
            public PerspectiveCamera PerspectiveCamera => (PerspectiveCamera)self.ActiveCamera!;

            public T EnsureLayer<T>() where T : ILayer3D, new()
            {
                var layer = self.Layers.OfType<T>().FirstOrDefault();
                layer ??= self.AddLayer<T>();
                return layer;
            }

            public T Layer<T>() where T : ILayer3D
            {
                return self.Layers.Layers.OfType<T>().First();
            }

            public T AddLayer<T>() where T : ILayer3D, new()
            {
                return self.AddLayer(new T());
            }

            public T AddLayer<T>(T layer) where T : ILayer3D
            {
                self.Layers.Add(layer);
                return layer;
            }

            public IEnumerable<Object3D> ObjectsWithComponent<TComp>() where TComp : IComponent
            {
                var layer = self.Layers.OfType<ComponentLayer<TComp>>().FirstOrDefault();
                if (layer == null)
                {
                    layer = new ComponentLayer<TComp>();
                    self.Layers.Add(layer);
                }

                return layer.Content.Cast<Object3D>();
            }

            public IEnumerable<T> TypeLayerContent<T>() where T : Object3D
            {
                var layer = self.Layers.OfType<TypeLayer<T>>().FirstOrDefault();
                if (layer == null)
                    return [];
                return layer.Content.Cast<T>();
            }

            public void RayCollisions(
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

            public void ContainsPoint(Vector3 worldPoint, ConcurrentBag<Object3D> result, IEnumerable<ICollider3D>? colliders = null, float tollerance = 0)
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
        }

        #endregion

        #region GROUP

        extension(Group3D self)
        {
            public IEnumerable<Object3D> FindByNames(params string[] names)
            {
                foreach (var name in names)
                {
                    var child = self.DescendantsOrSelf().FirstOrDefault(a => a.Name == name);
                    if (child != null)
                        yield return child;
                }
            }

            public Group3D GroupByName(params string[] names)
            {
                return self.GroupByName(Matrix4x4.Identity, names);
            }

            public Group3D GroupByName(Matrix4x4 grpTransform, params string[] names)
            {
                var grp = new Group3D();
                if (!grpTransform.IsIdentity)
                    grp.Transform.Set(grpTransform);
                self.AddChild(grp);

                foreach (var child in self.FindByNames(names))
                    grp.AddChild(child, true);

                return grp;
            }

            public void Clear(bool dispose = false)
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

            public T AddChild<T>() where T : Object3D, new()
            {
                return self.AddChild(new T());
            }

            public T? FindByName<T>(string name) where T : Object3D
            {
                return self.Descendants<T>()
                           .FirstOrDefault(a => a.Name == name);
            }

            public IEnumerable<ObjectFeature<T>> DescendantsWithFeature<T>() where T : class
            {
                return self.Descendants().Features<T>();
            }

            public IEnumerable<Object3D> Descendants()
            {
                return self.Descendants<Object3D>();
            }

            public IEnumerable<T> Descendants<T>() where T : Object3D
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
        }
        extension(IEnumerable<Object3D> self)
        {
            public IEnumerable<ObjectFeature<T>> Features<T>() where T : class
            {
                foreach (var item in self)
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
        }

        #endregion

        #region ENGINE APP

        extension(EngineApp self)
        {
            public void OpenScene(string name)
            {
                self.OpenScene(self.Scenes.Single(s => s.Name == name));
            }
        }

        #endregion

        #region GEOMETRY

        public delegate void VertexAssignDelegate<T>(ref VertexData vertexData, in T value);

        public delegate void SkinAssignDelegate<T>(ref SkinData vertexData, in T value);

        extension(Geometry3D self)
        {
            public unsafe void Serialize(Stream stream)
            {
                var vertices = self.Vertices;
                var indices = self.Indices;

                using var writer = new BinaryWriter(stream);

                writer.Write("GEOM");
                writer.Write((int)self.ActiveComponents);
                writer.Write(vertices.Length);

                fixed (VertexData* pVertex = &vertices[0])
                    writer.Write(new Span<byte>(pVertex, vertices.Length * sizeof(VertexData)));

                writer.Write(indices.Length);

                if (indices.Length > 0)
                {
                    fixed (uint* pIndex = &indices[0])
                        writer.Write(new Span<byte>(pIndex, indices.Length * sizeof(uint)));
                }

                writer.Flush();
            }

            public void ScaleUV(Vector2 scale)
            {
                var vertices = self.Vertices;

                for (var i = 0; i < vertices.Length; i++)
                    vertices[i].UV *= scale;

                self.NotifyChanged(ChangeType.Geometry);
            }

            public void ApplyTransform(Matrix4x4 matrix)
            {
                var inverse = matrix.Invert();

                var normalMatrix = Matrix4x4.Transpose(inverse);

                var vertices = self.Vertices;

                for (var i = 0; i < vertices.Length; i++)
                {
                    vertices[i].Pos = vertices[i].Pos.Transform(matrix);
                    vertices[i].Normal = vertices[i].Normal.Transform(normalMatrix).Normalize();
                }

                self.NotifyChanged(ChangeType.Geometry);
            }

            public void Rebuild()
            {
                var vertices = self.Vertices;
                var indices = self.Indices;

                if (indices.Length == 0)
                    return;

                var newVertices = new VertexData[indices.Length];

                for (var i = 0; i < indices.Length; i++)
                    newVertices[i] = vertices[indices[i]];

                self.Vertices = vertices;
                self.Indices = [];

                self.NotifyChanged(ChangeType.Geometry);
            }

            public void FlipYUV()
            {
                var span = self.Vertices.AsSpan();

                for (var i = 0; i < span.Length; i++)
                {
                    ref var ver = ref span[i];
                    ver.UV.Y = 1 - ver.UV.Y;
                }

                self.NotifyChanged(ChangeType.Geometry);
            }

            public void Rebuild(IEnumerable<Triangle3> triangles)
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

            public Geometry3D TransformToLine()
            {
                if (self.Primitive != DrawPrimitive.Triangle)
                    throw new NotSupportedException();

                var res = new Geometry3D
                {
                    Primitive = DrawPrimitive.Line
                };

                if (self.Indices.Length > 0)
                {
                    var srcI = 0;
                    var dstI = 0;
                    var newIndices = new uint[self.Indices.Length * 2];
                    var newSpan = newIndices.AsSpan();
                    var srcSpan = self.Indices.AsSpan();

                    while (srcI < self.Indices.Length)
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

            public unsafe Vector3[] ExtractPositions(bool useIndex = false)
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

            public void ComputeNormals()
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

            public void ToTriangles()
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

            public void EnsureIndices()
            {
                if (self.Indices == null || self.Indices.Length == 0)
                {
                    self.Indices = new uint[self.Vertices.Length];
                    for (var i = 0; i < self.Vertices.Length; i++)
                        self.Indices[i] = (uint)i;
                }
                self.NotifyChanged(ChangeType.Geometry);
            }

            public void SmoothNormals()
            {
                SmoothNormals(self, 0, (uint)self.Vertices.Length - 1);
            }

            public void SmoothNormals(uint startIndex, uint endIndex, int decimals = 4)
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

            public IEnumerable<Triangle3> Triangles()
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

            public unsafe void ComputeTangents()
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

            public void SetSkinData<T>(SkinAssignDelegate<T> selector, T[] array)
            {
                var skinGeo = self.EnsureComponent<SkinnedGeometry>();

                skinGeo.Skin ??= new SkinData[array.Length];

                if (skinGeo.Skin.Length < array.Length)
                {
                    var newArray = skinGeo.Skin;
                    Array.Resize(ref newArray, array.Length);
                    skinGeo.Skin = newArray;
                }

                for (var i = 0; i < array.Length; i++)
                    selector(ref skinGeo.Skin[i], array[i]);

                self.NotifyChanged(ChangeType.Geometry);
            }

            public void SetVertexData<T>(VertexAssignDelegate<T> selector, T[] array)
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

            public Bounds3 ComputeBounds(Matrix4x4 transform)
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

            public void EnsureCCW()
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

            public void ComputeIndices(int decimals = 5)
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
        }

        #endregion

        #region CAMERA

        extension(Camera self)
        {
            public Ray3 ScreenToRay(Vector2 screenPoint)
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
            public Rect2I WorldToScreen(in Bounds3 bounds, int eye, bool flipY)
            {
                var scrBounds = new Bounds2
                {
                    Min = new Vector2(float.PositiveInfinity, float.PositiveInfinity),
                    Max = new Vector2(float.NegativeInfinity, float.NegativeInfinity)
                };

                var eyes = eye == -1 ? 2 : 1;
                var baseEye = eye == -1 ? 0 : eye;

                foreach (var corner in bounds.Points)
                {
                    for (var curEye = 0; curEye < eyes; curEye++)
                    {
                        if (!self.TryWorldToScreen(corner, baseEye + curEye, flipY, out var screen))
                            return new Rect2I(self.ViewSize);

                        scrBounds.Min = Vector2.Min(scrBounds.Min, screen);
                        scrBounds.Max = Vector2.Max(scrBounds.Max, screen);
                    }
                }

                var rect = scrBounds.ToRect2I();

                var x0 = Math.Clamp(rect.X, 0, (int)self.ViewSize.Width);
                var y0 = Math.Clamp(rect.Y, 0, (int)self.ViewSize.Height);
                var x1 = Math.Clamp(rect.X + rect.Width, 0, self.ViewSize.Width);
                var y1 = Math.Clamp(rect.Y + rect.Height, 0, self.ViewSize.Height);

                return new Rect2I(x0, y0, (uint)(x1 - x0), (uint)(y1 - y0));
            }

            public bool TryWorldToScreen(in Vector3 worldPos, int eye, bool flipY, out Vector2 screenPos)
            {
                var viewProj = self.Eyes != null ?
                        self.Eyes[Math.Max(self.ActiveEye, eye)].ViewProj :
                        self.ViewProjection;

                return MathUtils.TryGetScreenPoint(worldPos, viewProj, self.ViewSize, flipY, out screenPos);
            }

            [Obsolete]
            public Vector2 WorldToScreen(Vector3 world)
            {
                var size = new Vector2(self.ViewSize.Width, self.ViewSize.Height);
                var proj = (world.Project(self.ViewProjection).ToVector2() + Vector2.One) * 0.5f;
                return new Vector2(proj.X, proj.Y) * size;
            }

            public void CreateViewFromDirection(Vector3 directionVector, Vector3 upVector)
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

            public Vector3 Project(Vector3 worldPoint)
            {
                return worldPoint.Project(self.ViewProjection);
            }

            public IEnumerable<Vector3> Project(IEnumerable<Vector3> worldPoints)
            {
                var viewProj = self.ViewProjection;

                foreach (var vertex in worldPoints)
                    yield return vertex.Project(viewProj);
            }

            public Vector3 Unproject(Vector3 viewPoint)
            {
                var viewProjInv = self.ViewProjectionInverse;
                return viewPoint.Project(viewProjInv);
            }

            public IEnumerable<Vector3> Unproject(IEnumerable<Vector3> viewPoint)
            {
                var viewProjInv = self.ViewProjectionInverse;
                foreach (var vertex in viewPoint)
                    yield return vertex.Project(viewProjInv);
            }

            public Vector3[] FrustumPoints(float? farPlane = null)
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

            public Plane[] FrustumPlanes(Plane[]? planes, out int count, bool fullStereo = true)
            {
                var stereo = fullStereo && self.IsStereo && self.Eyes?.Length > 1;

                count = stereo ? 12 : 6;

                if (planes == null || planes.Length < count)
                    Array.Resize(ref planes, count);

                if (stereo)
                {
                    Debug.Assert(self.Eyes != null);

                    self.Eyes[0].ViewProj.FrustumPlanes(planes.AsSpan(0, 6));
                    self.Eyes[1].ViewProj.FrustumPlanes(planes.AsSpan(6, 6));
                }
                else
                    self.ViewProjection.FrustumPlanes(planes.AsSpan(0, 6));

                return planes;
            }
        }

        extension(Matrix4x4 self)
        {
            public void FrustumPlanes(Span<Plane> planes)
            {
                if (planes.Length < 6)
                    throw new ArgumentException("Plane buffer must contain at least 6 elements.", nameof(planes));

                planes[0] = new Plane(
                    self.M14 + self.M11,
                    self.M24 + self.M21,
                    self.M34 + self.M31,
                    self.M44 + self.M41
                ).Normalize();

                planes[1] = new Plane(
                    self.M14 - self.M11,
                    self.M24 - self.M21,
                    self.M34 - self.M31,
                    self.M44 - self.M41
                ).Normalize();

                planes[2] = new Plane(
                    self.M14 - self.M12,
                    self.M24 - self.M22,
                    self.M34 - self.M32,
                    self.M44 - self.M42
                ).Normalize();

                planes[3] = new Plane(
                    self.M14 + self.M12,
                    self.M24 + self.M22,
                    self.M34 + self.M32,
                    self.M44 + self.M42
                ).Normalize();

                planes[4] = new Plane(
                    self.M13,
                    self.M23,
                    self.M33,
                    self.M43
                ).Normalize();

                planes[5] = new Plane(
                    self.M14 - self.M13,
                    self.M24 - self.M23,
                    self.M34 - self.M33,
                    self.M44 - self.M43
                ).Normalize();
            }
        }

        #endregion

        #region TRANSFORM3D

        extension(Transform3D self)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetPosition(float x, float y, float z)
            {
                self.Position = new Vector3(x, y, z);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetScale(float x, float y, float z)
            {
                self.Scale = new Vector3(x, y, z);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetScale(float value)
            {
                self.Scale = new Vector3(value, value, value);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetPositionZ(float value)
            {
                self.Position = new Vector3(self.Position.X, self.Position.Y, value);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetPositionX(float value)
            {
                self.Position = new Vector3(value, self.Position.Y, self.Position.Z);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetPositionY(float value)
            {
                self.Position = new Vector3(self.Position.X, value, self.Position.Z);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Pose3 ToPose()
            {
                return new Pose3
                {
                    Orientation = self.Orientation,
                    Position = self.Position
                };
            }
        }

        #endregion

        #region MATERIAL

        extension(Material self)
        {
            public void WriteStencilMask(byte channel, bool isOn)
            {
                int curValue = self.WriteStencil ?? 0;
                if (isOn)
                    curValue |= channel;
                else
                    curValue &= ~channel;

                self.WriteStencil = curValue == 0 ? null : (byte)curValue;
            }

            public bool UpdateColor(Color color)
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
        }

        extension(TriangleMesh self)
        {
            public void UpdateColor(Color color)
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
        }

        #endregion

        #region TEXTURE2D

        extension(Texture2D self)
        {
            public void SaveAs(string path, SKEncodedImageFormat format = SKEncodedImageFormat.Png, int quality = 100)
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

            public void Generate(bool warmUp = false)
            {
                var render = EngineApp.Current.Renderer;
                render.LoadTexture(self);
            }
        }

        extension(ShaderUpdateBuilder self)
        {
            public void PrepareTexture(Texture? texture)
            {
                if (texture == null)
                    return;

                if (texture.Format.IsSrgb())
                    self.AddFeature("TEXTURE_IS_SRGB");

                if (texture.ForceSrgb)
                    self.AddFeature("TEXTURE_FORCE_SRGB");
            }

            public void LoadTextureFixSrgb(Func<Texture2D?> value, ResourceSlot slot)
            {
                var curSlot = self.GetTextureSlot(slot);

                self.ExecuteAction((ctx, up) => up.LoadTextureFixSrgb(ctx, value(), slot));
            }
        }

        extension(IUniformProvider self)
        {
            public void LoadTextureFixSrgb(UpdateShaderContext ctx, Texture? texture, int slot)
            {
                if (texture == null)
                    return;

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

                self.LoadTexture(texture, slot);

                if (!isDecodeEnabled && isSrgb && texture.Sampler == null)
                {
                    var sampler = TextureSamplerFactory.DisableSrgbDecode(texture);

                    self.LoadSampler(sampler, slot);
                }

            }
        }

        extension(TextureSampler self)
        {
            public void Update(Texture texture)
            {
                var changed = false;

                if (self.MinFilter != texture.MinFilter)
                {
                    self.MinFilter = texture.MinFilter;
                    changed = true;
                }

                if (self.MagFilter != texture.MagFilter)
                {
                    self.MagFilter = texture.MagFilter;
                    changed = true;
                }

                if (self.WrapS != texture.WrapS)
                {
                    self.WrapS = texture.WrapS;
                    changed = true;
                }

                if (texture is Texture2D tex2d)
                {
                    if (self.MaxAnisotropy != tex2d.MaxAnisotropy)
                    {
                        self.MaxAnisotropy = tex2d.MaxAnisotropy;
                        changed = true;
                    }

                    if (self.WrapT != tex2d.WrapT)
                    {
                        self.WrapT = tex2d.WrapT;
                        changed = true;
                    }

                    if (self.BorderColor != tex2d.BorderColor)
                    {
                        self.BorderColor = tex2d.BorderColor;
                        changed = true;
                    }
                }

                if (texture is Texture3D tex3d)
                {
                    if (self.WrapR != tex3d.WrapR)
                    {
                        self.WrapR = tex3d.WrapR;
                        changed = true;
                    }
                }

                if (changed)
                    self.Invalidate();
            }
        }

        #endregion

        #region MISC

        extension(ICurve2D self)
        {
            public Poly2 ToPoly2(int numPoints, bool isClosed)
            {
                var points = new Vector2[numPoints];

                for (var i = 0; i < numPoints; i++)
                    points[i] = self.GetPointAtTime(1f / numPoints * i);

                return new Poly2
                {
                    Points = points,
                    IsClosed = isClosed
                };
            }
        }

        extension<T>(IBuffer<T> self)
        {
            public unsafe void UpdateElement(T element, int index)
            {
                var span = new ReadOnlySpan<T>(&element, 1);
                self.UpdateRange(span, index);
            }
        }

        extension<T>(IList<T> self) where T : IRenderUpdate
        {
            public void Update(RenderContext ctx)
            {
                var count = self.Count;
                for (var i = 0; i < count; i++)
                    self[i].Update(ctx);
            }
        }

        extension<T>(IEnumerable<T> self) where T : IRenderUpdate
        {
            public void Update(RenderContext ctx, bool safeMode)
            {
                if (safeMode)
                    self = self.ToArray();

                foreach (var item in self)
                    item.Update(ctx);

                //target.ForeachSafe(a => a.Update(ctx));
            }

            public void Reset(bool onlySelf)
            {
                //target.ForeachSafe(a => a.Reset(onlySelf));
            }
        }

        extension(AssetLoader self)
        {
            public T Load<T>(string fileUri, IAssetLoaderOptions? options = null) where T : EngineObject
            {
                return (T)self.Load(new Uri(fileUri, UriKind.Absolute), typeof(T), null, options);
            }

            public Uri GetMimeUri(string mimeType)
            {
                return new Uri($"stream://mime/{mimeType}.{ImageMimeToExtension(mimeType)}");
            }
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
                "image/ktx2" => ".ktx2",
                _ => null
            };
        }

        #endregion
    }
}
