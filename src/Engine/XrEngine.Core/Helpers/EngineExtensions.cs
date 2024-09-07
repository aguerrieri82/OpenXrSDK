using System.Diagnostics.CodeAnalysis;
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

        public static void Clear(this Group3D self)
        {
            self.BeginUpdate();
            try
            {
                for (var i = self.Children.Count - 1; i >= 0; i--)
                    self.RemoveChild(self.Children[i]);
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
            Dictionary<Vector3, List<int>> groups = [];

            for (var i = 0; i < geo.Vertices.Length; i++)
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

        public static void ComputeTangents(this Geometry3D geo)
        {
            var tan1 = new Vector3[geo.Indices.Length];
            var tan2 = new Vector3[geo.Indices.Length];

            int i = 0;
            while (i < geo.Indices!.Length)
            {
                var i1 = geo.Indices[i++];
                var i2 = geo.Indices[i++];
                var i3 = geo.Indices[i++];

                var v1 = geo.Vertices[i1].Pos;
                var v2 = geo.Vertices[i2].Pos;
                var v3 = geo.Vertices[i3].Pos;

                var w1 = geo.Vertices[i1].UV;
                var w2 = geo.Vertices[i2].UV;
                var w3 = geo.Vertices[i3].UV;


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


            for (int a = 0; a < geo.Indices.Length; a++)
            {
                ref var vert = ref geo.Vertices[geo.Indices[a]];

                var n = vert.Normal;
                var t = tan1[a];

                var txyz = Vector3.Normalize(t - n * Vector3.Dot(n, t));

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
                if (bounds.Intersects(line, out var _))
                    return true;
            }

            return false;
        }

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
            target.ForeachSafe(a => a.Update(ctx));
        }

        public static T Load<T>(this AssetLoader self, string filePath, IAssetLoaderOptions? options = null) where T : EngineObject
        {
            return (T)self.Load(new Uri(filePath, UriKind.RelativeOrAbsolute), typeof(T), null, options);
        }


        #endregion


    }
}
