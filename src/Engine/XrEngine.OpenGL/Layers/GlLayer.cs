using XrMath;

namespace XrEngine.OpenGL
{
    [Flags]
    public enum GlLayerType
    {
        Unknown = 0,
        Color = 0x1,
        Opaque = 0x2 | Color,
        Blend = 0x4 | Color,
        CastShadow = 0x8,
        FullReflection = 0x10,
        Custom = 0x20,
        Light = 0x40,
    }

    public class GlLayer : IDisposable
    {
        protected readonly OpenGLRender _render;
        protected readonly RenderContent _content;
        protected readonly Scene3D _scene;
        protected readonly ILayer3D? _sceneLayer;
        protected readonly GlLayerType _type;
        protected long _lastUpdateVersion;
        protected long _lastFrame;
        private Camera? _lastCamera;
        private int _lastDrawId;

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

            var objects = _sceneLayer != null ?
                _sceneLayer.Content.OfType<Object3D>() :
                _scene.Descendants();

            foreach (var obj3D in objects)
                AddContent(obj3D);

            _lastUpdateVersion = _sceneLayer != null ? _sceneLayer.Version : _scene.Version;

            Log.Debug(this, "Content Build");
        }

        protected void RemoveContent(Object3D obj3d)
        {
            if (!obj3d.Feature<IVertexSource>(out var vrtSrc))
                return;

            var clean = new List<Action>();

            foreach (var shader in _content.ShaderContents)
            {
                foreach (var vertex in shader.Value.Contents)
                {
                    for (var i = vertex.Value.Contents.Count - 1; i >= 0; i--)
                    {
                        var draw = vertex.Value.Contents[i];

                        if (draw.Object == obj3d)
                            vertex.Value.Contents.RemoveAt(i);
                    }

                    if (vertex.Value.Contents.Count == 0)
                        clean.Add(() => shader.Value.Contents.Remove(vertex.Key));
                }

                if (shader.Value.Contents.Count == 0)
                    clean.Add(() => _content.ShaderContents.Remove(shader.Key));
            }

            foreach (var action in clean)
                action();
        }

        protected void AddContent(Object3D obj3d)
        {
            if (!obj3d.Feature<IVertexSource>(out var vrtSrc))
                return;

            foreach (var realMaterial in vrtSrc.Materials.OfType<ShaderMaterial>())
            {
                var material = ReplaceMaterial(realMaterial);

                if (material.Shader == null)
                    continue;

                if (!_content.ShaderContents.TryGetValue(material.Shader, out var shaderContent))
                {
                    shaderContent = new ShaderContent
                    {
                        ProgramGlobal = material.Shader.GetGlResource(gl => new GlProgramGlobal(_render.GL, material.Shader!))
                    };

                    _content.ShaderContents[material.Shader] = shaderContent;
                }

                if (!shaderContent.Contents.TryGetValue(vrtSrc.Object, out var vertexContent))
                {
                    vertexContent = new VertexContent
                    {
                        VertexHandler = vrtSrc.Object.GetGlResource(a => GlVertexSourceHandle.Create(_render.GL, vrtSrc)),
                        ActiveComponents = VertexComponent.None,
                        RenderPriority = vrtSrc.RenderPriority
                    };

                    foreach (var attr in vertexContent.VertexHandler.Layout!.Attributes!)
                        vertexContent.ActiveComponents |= attr.Component;

                    shaderContent.Contents[vrtSrc.Object] = vertexContent;
                }

                var instance = new GlProgramInstance(_render.GL, material, shaderContent.ProgramGlobal!, obj3d);

                ConfigureProgramInstance(instance);

                vertexContent.Contents.Add(new DrawContent
                {
                    Draw = () => vertexContent!.VertexHandler!.Draw(material.Shader.ForcePrimitive),
                    ProgramInstance = instance,
                    DrawId = _lastDrawId++,
                    Object = obj3d
                });
            }
        }

        protected virtual void ConfigureProgramInstance(GlProgramInstance instance)
        {

        }

        public void Prepare(RenderContext ctx)
        {
            var curCamera = _render.UpdateContext.PassCamera!;

            if (ctx.Frame == _lastFrame && curCamera == _lastCamera)
                return;

            curCamera.FrustumPlanes(_render.UpdateContext.FrustumPlanes);

            ComputeVisibility();

            if (_render.Options.SortByCameraDistance)
                ComputeDistance(curCamera);

            UpdateVertexHandlers();

            _lastFrame = ctx.Frame;
            _lastCamera = curCamera;
        }

        protected void UpdateVertexHandlers()
        {
            foreach (var content in _content.ShaderContents.SelectMany(a => a.Value.Contents.Values))
            {
                var vHandler = content.VertexHandler!;

                if (!content.IsHidden && vHandler.NeedUpdate)
                    vHandler.Update();
            }
        }

        protected int ComputeVisibility()
        {
            var updateContext = _render.UpdateContext;

            int totHidden = 0;
            int totDraw = 0;

            foreach (var content in _content.ShaderContents.SelectMany(a => a.Value.Contents.Values))
            {
                var allHidden = true;

                foreach (var draw in content.Contents)
                {
                    totDraw++;

                    var progInst = draw.ProgramInstance!;

                    draw.IsHidden = !progInst.Material!.IsEnabled || !draw.Object!.IsVisible;

                    if (!draw.IsHidden && _render.Options.FrustumCulling && draw.Object is TriangleMesh mesh)
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
            var cameraPos = camera.WorldPosition;

            foreach (var content in _content.ShaderContents.SelectMany(a => a.Value.Contents.Values))
            {
                if (content.IsHidden)
                    continue;

                var count = 0;
                var sum = 0f;

                foreach (var draw in content.Contents)
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

        public long Version => _sceneLayer != null ? _sceneLayer.Version : _scene.Version;
    }
}
