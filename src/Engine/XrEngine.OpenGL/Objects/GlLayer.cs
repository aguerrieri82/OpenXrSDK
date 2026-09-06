using System.Diagnostics;
using Common.Interop;
using System.Numerics;




#if GLES
using Silk.NET.OpenGLES;
using RealGL = Silk.NET.OpenGLES.GL;
#else
using Silk.NET.OpenGL;
using RealGL = Silk.NET.OpenGL.GL;
#endif

using XrMath;

namespace XrEngine.OpenGL
{

    public enum InstanceBufferMode
    {
        Auto,
        UpdateAlways,
        UpdateIncremental,
        UpdateAllWhenChanged
    }

    public class GlLayer : IDisposable, IGlLayer
    {
        protected struct VisibilityContent
        {
            public MaterialContent Material;
            public VertexContent Vertex;
            public DrawContent Draw;
        }

        protected VisibilityContent[] _visibilityContent = [];
        protected readonly OpenGLRender _render;
        protected readonly RenderContent _content;
        protected readonly Scene3D _scene;
        protected readonly ILayer3D? _sceneLayer;
        protected readonly GlLayerType _type;
        protected long _lastUpdateVersion;
        protected long _lastFrame;
        protected Camera? _lastCamera;
        protected int _lastDrawId;
        protected bool _contentDirty;
        protected List<Action<RealGL>> _renderActions = [];
        private bool _boundsDirty;
        private Bounds3 _bounds;
        private bool _isEmpty;

        public GlLayer(OpenGLRender render, Scene3D scene, GlLayerType type, ILayer3D? sceneLayer = null)
        {
            _render = render;
            _content = new RenderContent();
            _scene = scene;
            _lastUpdateVersion = -1;
            _sceneLayer = sceneLayer;
            _type = type;

            sceneLayer?.Changed += OnSceneLayerChanged;

            Rebuild();
        }

        private async void OnSceneLayerChanged(ILayer3D layer, Layer3DChange change)
        {
            await _render.Dispatcher.Switch;

            if (change.Type == Layer3DChangeType.Removed || change.Type == Layer3DChangeType.Updated)
                RemoveContent((Object3D)change.Item, true);

            if (change.Type == Layer3DChangeType.Added || change.Type == Layer3DChangeType.Updated)
                AddContent((Object3D)change.Item, true);

            _lastUpdateVersion = Version;
        }

        protected virtual ShaderMaterial ReplaceMaterial(ShaderMaterial material)
        {
            return material;
        }

        public void Update()
        {
            if (NeedUpdate)
                Rebuild();
        }

        public void Rebuild()
        {
            GlUtils.EnsureRenderThread();

            Log.Info(this, "Building content '{0}' ({1})...", _scene.Name ?? "", _sceneLayer?.Name ?? "Main");

            _content.Contents.Clear();
            _content.LayerVersion = Version;

            _lastDrawId = 0;

            var objects = _sceneLayer != null ?
                _sceneLayer.Content.OfType<Object3D>() :
                _scene.Descendants();

            foreach (var obj3D in objects)
                AddContent(obj3D, false);

            foreach (var shader in _content.Contents.Values)
            {
                foreach (var materialContent in shader.Contents.Values)
                    Update(materialContent);
            }

            _lastUpdateVersion = _sceneLayer != null ? _sceneLayer.Version : _scene.Version;

            GlDebug.Log(this, "Content Build");
        }

        protected void RemoveContent(Object3D obj3d, bool incremental)
        {
            GlUtils.EnsureRenderThread();

            if (!obj3d.TryFeature<IVertexSource>(out var vrtSrc))
                return;

            obj3d.EnsureId();

            foreach (var shaderEntry in _content.Contents.ToArray())
            {
                var shaderContent = shaderEntry.Value;

                foreach (var materialEntry in shaderContent.Contents.ToArray())
                {
                    var materialContent = materialEntry.Value;
                    var materialChanged = false;

                    foreach (var vertexEntry in materialContent.Contents.ToArray())
                    {
                        var vertexContent = vertexEntry.Value;
                        var removed = false;

                        for (var i = vertexContent.Contents.Count - 1; i >= 0; i--)
                        {
                            var draw = vertexContent.Contents[i];

                            if (draw.Object == obj3d)
                            {
                                vertexContent.Contents.RemoveAt(i);
                                removed = true;
                            }
                        }

                        if (!removed)
                            continue;

                        vertexContent.ContentVersion++;
                        materialChanged = true;

                        if (vertexContent.Contents.Count == 0)
                            materialContent.Contents.Remove(vertexEntry.Key);
                    }

                    if (!materialChanged)
                        continue;

                    Invalidate(shaderContent);

                    if (materialContent.Contents.Count == 0)
                    {
                        shaderContent.Contents.Remove(materialEntry.Key);
                    }
                    else if (incremental)
                    {
                        Update(materialContent);
                    }
                }

                if (shaderContent.Contents.Count == 0)
                    _content.Contents.Remove(shaderEntry.Key);
            }
        }

        protected IEnumerable<Object3D> Objects
        {
            get
            {
                foreach (var shaderEntry in _content.Contents)
                {
                    var shaderContent = shaderEntry.Value;

                    foreach (var materialEntry in shaderContent.Contents)
                    {
                        var materialContent = materialEntry.Value;

                        foreach (var vertexEntry in materialContent.Contents)
                        {
                            var vertexContent = vertexEntry.Value;

                            foreach (var draw in vertexContent.Contents)
                                yield return draw.Object!;
                        }
                    }
                }
            }
        }

        protected void AddContent(Object3D obj3d, bool incremental)
        {
            GlUtils.EnsureRenderThread();

            if (!obj3d.TryFeature<IVertexSource>(out var vrtSrc))
                return;

            obj3d.EnsureId();

            foreach (var realMaterial in vrtSrc.Materials.OfType<ShaderMaterial>())
            {

#warning IMPROVE THIS!! 

                var isColor = realMaterial.Alpha == AlphaMode.Opaque ||
                              realMaterial.Alpha == AlphaMode.BlendMain ||
                              realMaterial.Alpha == AlphaMode.Mask;

                if (Type == GlLayerType.Color && !isColor)
                    continue;

                if (Type == GlLayerType.Blend && isColor)
                    continue;
                //

                var material = ReplaceMaterial(realMaterial);

                if (material.Shader == null)
                    continue;

                if (!_content.Contents.TryGetValue(material.Shader, out var shaderContent))
                {
                    shaderContent = new ShaderContent
                    {
                        ProgramGlobal = material.Shader.GetGlResource(gl => new GlProgramGlobal(_render.GL, material.Shader!))
                    };

                    _content.Contents[material.Shader] = shaderContent;
                }

                material.EnsureId();

                var materialKey = new ShaderMaterialKey
                {
                    ActiveComponent = vrtSrc.ActiveComponents,
                    MateriaId = material.Id,
                    SingleDrawId = material.IsSingleDraw ? (++shaderContent.SingleDrawCount) : 0
                };

                if (!shaderContent.Contents.TryGetValue(materialKey, out var materialContent))
                {
                    var instance = new GlProgramInstance(_render.GL, material, shaderContent.ProgramGlobal!, obj3d)
                    {
                        UseWorker = _render.Options.UseAsyncShaderCompile && !EngineNativeLib.RdcIsAttached()
                    };

                    ConfigureProgramInstance(instance);

                    materialContent = new MaterialContent
                    {
                        ProgramInstance = instance,
                        Material = material,
                        ActiveComponents = materialKey.ActiveComponent
                    };

                    shaderContent.Contents[materialKey] = materialContent;
                    Invalidate(shaderContent);
                }

                Debug.Assert(materialContent.Material == material);

                var vertexHandler = vrtSrc.Host.GetGlResource(a => GlVertexSourceHandle.Create(_render.GL, vrtSrc));

                if (!materialContent.Contents.TryGetValue(vrtSrc.Host, out var vertexContent))
                {
                    vertexContent = new VertexContent
                    {
                        VertexHandler = vertexHandler,
                        ActiveComponents = VertexComponent.None,
                    };

                    foreach (var attr in vertexHandler.Layout!.Attributes!)
                        vertexContent.ActiveComponents |= attr.Component;

                    materialContent.Contents[vrtSrc.Host] = vertexContent;

                }

                vertexContent.ContentVersion++;

                Action draw;

                if (material is ITessellationMaterial tes && tes.TessellationMode != TessellationMode.None)
                {
                    var size = vrtSrc.Primitive == DrawPrimitive.Quad ? 4 : 3;
                    //TODO: disable instance draw
                    draw = () =>
                    {
                        _render.GL.PatchParameter(PatchParameterName.Vertices, size);
                        _render.State.SetWireframe(tes.DebugTessellation);
                        _render.State.SetLineWidth(0.5f);
                        vertexContent!.VertexHandler!.Draw(DrawPrimitive.Patch);
                    };
                }
                else
                {
                    var primitive = material.Shader.ForcePrimitive;
                    draw = () => vertexContent!.VertexHandler!.Draw(primitive);
                }

                vertexContent.Contents.Add(new DrawContent
                {
                    Draw = draw,
                    DrawId = _lastDrawId++,
                    Object = obj3d,
                    UseFrustumCulling = obj3d is TriangleMesh mesh && (mesh.Flags & EngineObjectFlags.NoFrustumCulling) == 0,
                    ProgramInstance = materialContent.ProgramInstance
                });


                if (incremental)
                    Update(materialContent);
            }

            InvalidateContent();



            //Rebuild();
        }

        private void Update(MaterialContent materialContent)
        {
            var verContentList = materialContent.Contents.Values;

            materialContent.ActiveComponents = verContentList.FirstOrDefault()?.ActiveComponents ?? VertexComponent.None;

            if (materialContent.Material is not ShaderMaterial shaderMat)
                return;

            var instanceShader = shaderMat.Shader as IInstanceShader;

            materialContent.UseInstanceDraw = _render.Options.UseInstanceDraw && instanceShader != null &&
                                              verContentList.Any(a => a.Contents.Count > 1);


            var isSingleDraw = verContentList.Count == 1 &&
                                           verContentList.First().Contents.Count == 1;

            if (isSingleDraw)
                materialContent.SingleModel = verContentList.First().Contents[0].Object;
            else
                materialContent.SingleModel = null;

        }

        protected virtual void ConfigureProgramInstance(GlProgramInstance instance)
        {

        }

        public void Prepare(GlUpdateContext ctx)
        {
            var camera = ctx.PassCamera!;

            var frameChanged = ctx.Frame != _lastFrame;
            var cameraChanged = _lastCamera == null || camera.ViewProjection != _lastCamera.ViewProjection;

            if (!frameChanged && !cameraChanged)
                return;

            if (_contentDirty)
                SortMaterials();

            if (_render.Options.FrustumCulling)
            {
                ctx.FrustumPlanes = camera.FrustumPlanes(ctx.FrustumPlanes, out var count);
                ctx.FrustumPlanesCount = count;
            }

            if (cameraChanged || frameChanged)
                ComputeVisibility();

            if (frameChanged)
                UpdateVertexHandlers();

            _lastFrame = ctx.Frame;
            _lastCamera = camera;

            _contentDirty = false;

        }

        public Bounds2 GetScreenBounds(Camera camera, bool isMultiView, bool flipY)
        {
            var result = new Bounds2
            {
                Min = new(float.PositiveInfinity),
                Max = new(float.NegativeInfinity)
            };

            var objectClipping = false;

            var eyes = isMultiView ? 2 : 1;

            foreach (var corner in Bounds.Points)
            {
                for (var eye = 0; eye < eyes; eye++)
                {
                    if (!camera.TryWorldToScreen(corner, eye, flipY, out var screen))
                    {
                        objectClipping = true;
                        break;
                    }

                    result.Min = Vector2.Min(result.Min, screen);
                    result.Max = Vector2.Max(result.Max, screen);
                }
            }

            if (objectClipping)
            {
                result.Min = Vector2.Zero;
                result.Max = new Vector2(camera.ViewSize.Width, camera.ViewSize.Height);
            }

            return result;
        }

        protected void UpdateBounds()
        {
            var builder = new Bounds3Builder();

            foreach (var obj in Objects.Where(a => a.IsVisible))
                builder.Add(obj.WorldBounds);

            _bounds = builder.Result;

            _boundsDirty = false;
        }

        protected void SortMaterials()
        {
            foreach (var shaderContent in _content.Contents.Values)
            {
                if (!shaderContent.IsDirty)
                    continue;

                shaderContent.SortedContent = shaderContent.Contents
                    .OrderBy(a => a.Value.Material?.Priority)
                    .ThenBy(a => a.Value.ProgramInstance?.Program?.Handle ?? 0)
                    .ToArray();

                shaderContent.IsDirty = false;

                shaderContent.MaxPriority = shaderContent.Contents.Count == 0 ?
                    0 : shaderContent.Contents.Max(a => a.Value.Material!.Priority);
            }

            _content.SortedContent = _content.Contents
                .OrderBy(a => a.Value.MaxPriority)
                .ToArray();
        }

        protected void UpdateVertexHandlers()
        {
            foreach (var shaderEntry in _content.Contents)
            {
                var shader = shaderEntry.Key;

                var instanceShader = shader as IInstanceShader;

                foreach (var matEntry in shaderEntry.Value.Contents)
                {
                    foreach (var verContent in matEntry.Value.Contents.Values)
                    {
                        var vHandler = verContent.VertexHandler!;

                        if (vHandler.NeedUpdate)
                            vHandler.Update();

                        if (matEntry.Value.UseInstanceDraw)
                            UpdateInstanceDraws(instanceShader!, verContent, matEntry.Value.Material!);
                        else
                            verContent.Draw = null;
                    }

                }
            }
        }

        protected unsafe void UpdateInstanceDraws(IInstanceShader instanceShader, VertexContent verContent, Material material)
        {
            var ctx = _render.UpdateContext;

            var vHandler = verContent.VertexHandler!;

            var mode = InstanceBufferMode.Auto;

            var changedCount = 0;

            var elSize = MarshalCache.SizeOf(instanceShader.InstanceBufferType);

            if (verContent.InstanceBuffer == null || verContent.InstanceBuffer.Version != verContent.ContentVersion)
            {
                //TODO: store in somewhere safe, is unique for material+geometry
                verContent.InstanceBuffer ??= GlBuffer.Create(_render.GL, BufferTargetARB.ShaderStorageBuffer, instanceShader.InstanceBufferType);

                verContent.InstanceBuffer.Allocate((uint)(elSize * verContent.Contents.Count));
                verContent.InstanceBuffer.Version = verContent.ContentVersion;

                mode = InstanceBufferMode.UpdateAlways;
            }

            if (ctx.UseMotionVectors && ctx.MotionVectorProvider?.IsActive == true)
                mode = InstanceBufferMode.UpdateAlways;

            if (mode != InstanceBufferMode.UpdateAlways)
            {
                for (var i = 0; i < verContent.Contents.Count; i++)
                {
                    var draw = verContent.Contents[i]!;
                    if (instanceShader.NeedUpdate(draw.Object!, draw.InstanceVersion))
                    {
                        draw.InstanceChanged = true;
                        changedCount++;
                        if (mode == InstanceBufferMode.UpdateAllWhenChanged)
                            break;
                    }
                }
                if (changedCount == 0)
                    return;
            }

            if (mode == InstanceBufferMode.Auto)
            {
                var ratio = (float)changedCount / verContent.Contents.Count;
                if (ratio < 0.3 && changedCount < 5)
                    mode = InstanceBufferMode.UpdateIncremental;
                else
                    mode = InstanceBufferMode.UpdateAlways;
            }

            if (mode == InstanceBufferMode.UpdateAlways || mode == InstanceBufferMode.UpdateAllWhenChanged)
            {
                using var bufLock = verContent.InstanceBuffer!.Lock(BufferAccessMode.Replace);

                var data = (byte*)bufLock.Data;

                for (var i = 0; i < verContent.Contents.Count; i++)
                {
                    var draw = verContent.Contents[i];
                    draw.InstanceVersion = instanceShader.Update(ctx, data, draw.Object!, draw.Id);
                    data += elSize;
                }
            }
            else
            {

                verContent.InstanceBuffer!.BeginUpdate();
                var buffer = stackalloc byte[elSize];

                for (var i = 0; i < verContent.Contents.Count; i++)
                {
                    var draw = verContent.Contents[i];

                    if (!draw.InstanceChanged)
                        continue;

                    draw.InstanceVersion = instanceShader.Update(ctx, buffer, draw.Object!, draw.Id);
                    verContent.InstanceBuffer!.UpdateRange(new ReadOnlySpan<byte>(buffer, elSize), i, true);
                }

                verContent.InstanceBuffer!.EndUpdate();
            }

            if (verContent.Draw == null)
            {
                var instBuffer = (IGlBuffer)verContent.InstanceBuffer;

                if (material is ITessellationMaterial tes && tes.TessellationMode != TessellationMode.None)
                {
                    var size = vHandler.Source.Primitive == DrawPrimitive.Quad ? 4 : 3;

                    verContent.Draw = () =>
                    {
                        _render.GL.PatchParameter(PatchParameterName.Vertices, size);
                        _render.State.SetWireframe(tes.DebugTessellation);
                        _render.State.SetLineWidth(0.5f);

                        instBuffer.Load(BufferSlots.Instance);

                        vHandler.DrawInstances(verContent.Contents.Count, DrawPrimitive.Patch);
                    };
                }
                else
                {
                    verContent.Draw = () =>
                    {
                        instBuffer.Load(BufferSlots.Instance);

                        vHandler.DrawInstances(verContent.Contents.Count);
                    };
                }
            }
        }

        protected int ComputeVisibility()
        {
            if (_content.Contents.Count == 0)
                return 0;

            var ctx = _render.UpdateContext;
         
            var frustumCulling = _render.Options.FrustumCulling;
            var frustumPlanes = ctx.FrustumPlanes.AsSpan(0, ctx.FrustumPlanesCount);

            var totHidden = 0;

            _isEmpty = true;

            foreach (var shader in _content.Contents.Values)
            {
                foreach (var material in shader.Contents.Values)
                {
                    var materialEnabled = material.ProgramInstance!.Material!.IsEnabled;
                    var allMatHidden = true;

                    foreach (var vertex in material.Contents.Values)
                    {
                        var allVertexHidden = true;

                        foreach (var draw in vertex.Contents)
                        {
                            var obj = draw.Object!;

                            draw.IsHidden = !materialEnabled || !obj.IsVisible;

                            if (!draw.IsHidden && frustumCulling && draw.UseFrustumCulling)
                            {
                                draw.IsHidden = !draw.Object!.WorldBounds.IntersectFrustum(frustumPlanes);

                                if (draw.IsHidden)
                                    totHidden++;
                            }

                            if (!draw.IsHidden)
                            {
                                allVertexHidden = false;
                                allMatHidden = false;
                                _isEmpty = false;
                            }
                        }

                        vertex.IsHidden = allVertexHidden;
                    }

                    material.IsHidden = allMatHidden;
                }
            }

            return totHidden;
        }

        /*
        protected void UpdateVisibilityContent()
        {
            var content = new List<VisibilityContent>();

            foreach (var shader in _content.Contents.Values)
            {
                foreach (var material in shader.Contents.Values)
                {
                    foreach (var vertex in material.Contents.Values)
                    {
                        foreach (var draw in vertex.Contents)
                        {
                            content.Add(new VisibilityContent
                            {
                                Material = material,
                                Vertex = vertex,
                                Draw = draw
                            });
                        }
                    }
                }
            }

            _visibilityContent = content.ToArray();
        }


        protected int ComputeVisibilityV2()
        {
            var ctx = _render.UpdateContext;
            var frustumPlanes = ctx.FrustumPlanes.AsSpan(0, ctx.FrustumPlanesCount);

            var totHidden = 0;

            MaterialContent? curMaterial = null;
            VertexContent? curVertex = null;

            var materialEnabled = false;
            var allMatHidden = true;
            var allVertexHidden = true;

            _isEmpty = true;

            for (var i = 0; i < _visibilityContent.Length; i++)
            {
                ref var entry = ref _visibilityContent[i];

                if (!ReferenceEquals(curMaterial, entry.Material))
                {
                    if (curVertex != null)
                        curVertex.IsHidden = allVertexHidden;

                    if (curMaterial != null)
                        curMaterial.IsHidden = allMatHidden;

                    curMaterial = entry.Material;
                    curVertex = null;

                    materialEnabled = curMaterial.ProgramInstance!.Material!.IsEnabled;
                    allMatHidden = true;
                }

                if (!ReferenceEquals(curVertex, entry.Vertex))
                {
                    if (curVertex != null)
                        curVertex.IsHidden = allVertexHidden;

                    curVertex = entry.Vertex;
                    allVertexHidden = true;
                }

                var draw = entry.Draw;
                var obj = draw.Object!;

                var hidden = !materialEnabled || !obj.IsVisible;

                if (!hidden && draw.UseFrustumCulling)
                {
                    hidden = !obj.WorldBounds.IntersectFrustum(frustumPlanes);

                    if (hidden)
                        totHidden++;
                }

                draw.IsHidden = hidden;

                if (!hidden)
                {
                    allVertexHidden = false;
                    allMatHidden = false;
                    _isEmpty = false;
                }
            }

            if (curVertex != null)
                curVertex.IsHidden = allVertexHidden;

            if (curMaterial != null)
                curMaterial.IsHidden = allMatHidden;

            return totHidden;
        }
        */


        public void Dispose()
        {
            if (_sceneLayer != null)
                _sceneLayer.Changed -= OnSceneLayerChanged;
            _content?.Contents.Clear();
            GC.SuppressFinalize(this);
        }

        public void InvalidateContent()
        {
            _contentDirty = true;
            _boundsDirty = true;
        }

        internal void Invalidate(ShaderContent value)
        {
            value.IsDirty = true;
            InvalidateContent();
        }

        public void Execute(RealGL gl)
        {
            foreach (var action in _renderActions)
                action(gl);
        }

        public Bounds3 Bounds
        {
            get
            {
                if (_boundsDirty)
                    UpdateBounds();
                return _bounds;
            }
        }

        public List<Action<RealGL>> RenderActions => _renderActions;

        public bool IsStatic => (_type & GlLayerType.Static) != 0;

        public string? Name => _sceneLayer?.Name;

        public bool NeedUpdate => _lastUpdateVersion != Version;

        public GlLayerType Type => _type;

        public RenderContent Content => _content;

        public ILayer3D? SceneLayer => _sceneLayer;

        public Scene3D Scene => _scene;

        public bool IsEmpty => _isEmpty;

        public long Version => _sceneLayer != null ? _sceneLayer.Version : _scene.Version;
    }
}
