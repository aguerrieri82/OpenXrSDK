#if GLES
using Silk.NET.OpenGLES;
using System.Numerics;

#else
using Silk.NET.OpenGL;
using System.Numerics;

#endif

using XrMath;

namespace XrEngine.OpenGL
{

    public class GlLayer : IDisposable, IGlLayer
    {
        protected readonly OpenGLRender _render;
        protected readonly RenderContent _content;
        protected readonly Scene3D _scene;
        protected readonly ILayer3D? _sceneLayer;
        protected readonly GlLayerType _type;
        protected long _lastUpdateVersion;
        protected long _lastFrame;
        protected Camera? _lastCamera;
        protected int _lastDrawId;
        protected bool _isContentDirty;

        public GlLayer(OpenGLRender render, Scene3D scene, GlLayerType type, ILayer3D? sceneLayer = null)
        {
            _render = render;
            _content = new RenderContent();
            _scene = scene;
            _lastUpdateVersion = -1;
            _sceneLayer = sceneLayer;
            _type = type;
            if (sceneLayer != null)
                sceneLayer.Changed += OnSceneLayerChanged;
        }

        private void OnSceneLayerChanged(ILayer3D layer, Layer3DChange change)
        {
            if (change.Type == Layer3DChangeType.Added)
                AddContent((Object3D)change.Item);

            else if (change.Type == Layer3DChangeType.Removed)
                RemoveContent((Object3D)change.Item);

            _lastUpdateVersion = layer.Version;
        }

        protected virtual ShaderMaterial ReplaceMaterial(ShaderMaterial material)
        {
            return material;
            /*
            if (material is IPbrMaterial pbr)
            {
                return new BasicMaterial
                {
                    Alpha = material.Alpha,
                    CastShadows = material.CastShadows,
                    IsEnabled = material.IsEnabled,
                    Color = pbr.Color,
                    DoubleSided = material.DoubleSided,
                    UseClipDistance = material.UseClipDistance,
                    UseDepth = material.UseDepth,
                    WriteStencil = material.WriteStencil,
                    WriteDepth = material.WriteDepth,
                    WriteColor = material.WriteColor,
                    StencilFunction = material.StencilFunction,
                    CompareStencilMask = material.CompareStencilMask,
                    DiffuseTexture = pbr.ColorMap,
                    Shininess = Math.Max(1, (1 - pbr.Roughness) * 20),
                    Specular = pbr.Color,
                    Ambient = Color.White
                };
            }

            return material;
            */
        }

        public void Update()
        {
            if (NeedUpdate)
                Rebuild();
        }

        public void Rebuild()
        {
            Log.Info(this, "Building content '{0}' ({1})...", _scene.Name ?? "", _sceneLayer?.Name ?? "Main");

            _content.ShaderContents.Clear();
            _content.LayerVersion = Version;

            _lastDrawId = 0;

            IEnumerable<Object3D> objects = _sceneLayer != null ?
                _sceneLayer.Content.OfType<Object3D>() :
                _scene.Descendants();

            foreach (Object3D obj3D in objects)
                AddContent(obj3D);

            _lastUpdateVersion = _sceneLayer != null ? _sceneLayer.Version : _scene.Version;

            Log.Debug(this, "Content Build");
        }

        protected void RemoveContent(Object3D obj3d)
        {
            if (!obj3d.Feature<IVertexSource>(out IVertexSource? vrtSrc))
                return;

            List<Action> clean = new List<Action>();

            foreach (KeyValuePair<Shader, ShaderContent> shader in _content.ShaderContents)
            {
                foreach (KeyValuePair<EngineObject, VertexContent> vertex in shader.Value.Contents)
                {
                    for (int i = vertex.Value.Contents.Count - 1; i >= 0; i--)
                    {
                        DrawContent draw = vertex.Value.Contents[i];

                        if (draw.Object == obj3d)
                            vertex.Value.Contents.RemoveAt(i);
                    }

                    if (vertex.Value.Contents.Count == 0)
                        clean.Add(() => shader.Value.Contents.Remove(vertex.Key));
                }

                if (shader.Value.Contents.Count == 0)
                    clean.Add(() => _content.ShaderContents.Remove(shader.Key));
            }

            foreach (Action action in clean)
                action();

            _isContentDirty = true;
        }

        protected void AddContent(Object3D obj3d)
        {
            if (!obj3d.Feature<IVertexSource>(out IVertexSource? vrtSrc))
                return;

            foreach (ShaderMaterial realMaterial in vrtSrc.Materials.OfType<ShaderMaterial>())
            {
                ShaderMaterial material = ReplaceMaterial(realMaterial);

                if (material.Shader == null)
                    continue;

                if (!_content.ShaderContents.TryGetValue(material.Shader, out ShaderContent? shaderContent))
                {
                    shaderContent = new ShaderContent
                    {
                        ProgramGlobal = material.Shader.GetGlResource(gl => new GlProgramGlobal(_render.GL, material.Shader!))
                    };

                    _content.ShaderContents[material.Shader] = shaderContent;
                }

                if (!shaderContent.Contents.TryGetValue(vrtSrc.Object, out VertexContent? vertexContent))
                {
                    vertexContent = new VertexContent
                    {
                        VertexHandler = vrtSrc.Object.GetGlResource(a => GlVertexSourceHandle.Create(_render.GL, vrtSrc)),
                        ActiveComponents = VertexComponent.None,
                        RenderPriority = vrtSrc.RenderPriority
                    };

                    foreach (GlVertexAttribute attr in vertexContent.VertexHandler.Layout!.Attributes!)
                        vertexContent.ActiveComponents |= attr.Component;

                    shaderContent.Contents[vrtSrc.Object] = vertexContent;
                }

                GlProgramInstance instance = new GlProgramInstance(_render.GL, material, shaderContent.ProgramGlobal!, obj3d);

                ConfigureProgramInstance(instance);

                Action draw;

                if (material is ITessellationMaterial tes && tes.TessellationMode != TessellationMode.None)
                {
                    int size = vrtSrc.Primitive == DrawPrimitive.Quad ? 4 : 3;
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
                    DrawPrimitive? primitive = material.Shader.ForcePrimitive;
                    draw = () => vertexContent!.VertexHandler!.Draw(primitive);
                }

                vertexContent.Contents.Add(new DrawContent
                {
                    Draw = draw,
                    ProgramInstance = instance,
                    DrawId = _lastDrawId++,
                    Object = obj3d
                });
            }

            _isContentDirty = true;
        }

        protected virtual void ConfigureProgramInstance(GlProgramInstance instance)
        {

        }

        public void Prepare(RenderContext ctx)
        {
            Camera curCamera = _render.UpdateContext.PassCamera!;

            if (ctx.Frame == _lastFrame && curCamera == _lastCamera)
                return;

            if (_render.Options.FrustumCulling)
                curCamera.FrustumPlanes(_render.UpdateContext.FrustumPlanes);

            ComputeVisibility();

            if (_render.Options.SortByCameraDistance)
                ComputeDistance(curCamera);

            UpdateVertexHandlers();

            SortContent();

            _lastFrame = ctx.Frame;
            _lastCamera = curCamera;
        }


        protected void SortContent()
        {
            if (!_isContentDirty)
                return;

            _content.ShaderContentsSorted = _content.ShaderContents.OrderBy(a => a.Key.Priority).ToArray();

            foreach (ShaderContent shader in _content.ShaderContents.Values)
                shader.ContentsSorted = shader.Contents.Values.OrderBy(a => a.RenderPriority).ToArray();

            _isContentDirty = false;
        }

        protected void UpdateVertexHandlers()
        {
            foreach (VertexContent? content in _content.ShaderContents.SelectMany(a => a.Value.Contents.Values))
            {
                GlVertexSourceHandle vHandler = content.VertexHandler!;

                if (!content.IsHidden && vHandler.NeedUpdate)
                    vHandler.Update();
            }
        }

        protected int ComputeVisibility()
        {
            GlUpdateContext updateContext = _render.UpdateContext;

            int totHidden = 0;
            int totDraw = 0;

            foreach (VertexContent? content in _content.ShaderContents.SelectMany(a => a.Value.Contents.Values))
            {
                bool allHidden = true;

                foreach (DrawContent draw in content.Contents)
                {
                    totDraw++;

                    GlProgramInstance progInst = draw.ProgramInstance!;

                    draw.IsHidden = !progInst.Material!.IsEnabled || !draw.Object!.IsVisible;

                    if (!draw.IsHidden && _render.Options.FrustumCulling && draw.Object is TriangleMesh mesh && (mesh.Flags & EngineObjectFlags.NoFrustumCulling) == 0)
                    {
                        draw.IsHidden = !mesh.WorldBounds.IntersectFrustum(updateContext.FrustumPlanes!);
                        if (draw.IsHidden)
                            totHidden++;
                    }

                    if (!draw.IsHidden)
                        allHidden = false;
                }

                content.IsHidden = allHidden;
            }

            return totHidden;
        }

        protected void ComputeDistance(Camera camera)
        {
            Vector3 cameraPos = camera.WorldPosition;

            foreach (VertexContent? content in _content.ShaderContents.SelectMany(a => a.Value.Contents.Values))
            {
                if (content.IsHidden)
                    continue;

                int count = 0;
                float sum = 0f;

                foreach (DrawContent draw in content.Contents)
                {
                    if (draw.IsHidden)
                        continue;

                    draw.Distance = draw.Object!.DistanceTo(cameraPos);
                    count++;
                    sum += draw.Distance;
                }
                content.AvgDistance = sum / count;
            }
        }

        public void Dispose()
        {
            if (_sceneLayer != null)
                _sceneLayer.Changed -= OnSceneLayerChanged;
            _content?.ShaderContents.Clear();
            GC.SuppressFinalize(this);
        }

        public string? Name => _sceneLayer?.Name;

        public bool NeedUpdate => _lastUpdateVersion != Version;

        public GlLayerType Type => _type;

        public RenderContent Content => _content;

        public ILayer3D? SceneLayer => _sceneLayer;

        public Scene3D Scene => _scene;

        public bool IsEmpty => _content.ShaderContents.Count == 0;

        public long Version => _sceneLayer != null ? _sceneLayer.Version : _scene.Version;
    }
}
