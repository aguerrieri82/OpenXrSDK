using XrMath;

namespace XrEngine.OpenGL
{
    public enum GlLayerType
    {
        Main,
        CastShadow,
        Blend,
        FullReflection, 
        Custom
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

        public GlLayer(OpenGLRender render, Scene3D scene, GlLayerType type, ILayer3D? sceneLayer = null)
        {
            _render = render;
            _content = new RenderContent();
            _scene = scene;
            _lastUpdateVersion = -1;
            _sceneLayer = sceneLayer;
            _type = type;
        }

        protected void UpdateLights()
        {
            _content.Lights = [];
            _content.LightsHash = "";

            foreach (var light in _scene.Descendants<Light>().Visible())
            {
                _content.Lights.Add(light);

                if (light is ImageLight imgLight)
                {
                    if (imgLight.Panorama?.Data != null && imgLight.Panorama.Version != _content.ImageLightVersion)
                    {
                        var options = PanoramaProcessorOptions.Default();

                        options.SampleCount = 1024;
                        options.Resolution = 256;
                        options.Mode = IblProcessMode.GGX | IblProcessMode.Lambertian;

                        imgLight.Textures = _render.ProcessPanoramaIBL(imgLight.Panorama.Data[0], options);
                        imgLight.Panorama.NotifyLoaded();
                        imgLight.NotifyIBLCreated();

                        _content.ImageLightVersion = imgLight.Panorama.Version;
                        _render.ResetState();
                    }
                }

                _content.LightsHash += light.GetType().Name + "|";
            }
        }


        public virtual void Update()
        {
            Log.Info(this, "Building content '{0}' ({1})...", _scene.Name ?? "", _sceneLayer?.Name ?? "Main");

            _content.ShaderContents.Clear();
            _content.LayerVersion = Version;

            var drawId = 0;

            if (HasLights)
                UpdateLights();

            var objects = _sceneLayer != null ?
                _sceneLayer.Content.OfType<Object3D>() :
                _scene.Descendants();

            foreach (var obj3D in objects)
            {
                if (obj3D is Light light)
                    continue;

                if (!obj3D.Feature<IVertexSource>(out var vrtSrc))
                    continue;

                foreach (var material in vrtSrc.Materials.OfType<ShaderMaterial>())
                {
                    if (material.Shader == null)
                        continue;

                    if ((material.Alpha == AlphaMode.Blend && Type == GlLayerType.Main) ||
                        (material.Alpha != AlphaMode.Blend && Type == GlLayerType.Blend))
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

                    var instance = new GlProgramInstance(_render.GL, material, shaderContent.ProgramGlobal!, obj3D);

                    ConfigureProgramInstance(instance);

                    vertexContent.Contents.Add(new DrawContent
                    {
                        Draw = () => vertexContent!.VertexHandler!.Draw(material.Shader.ForcePrimitive),
                        ProgramInstance = instance,
                        DrawId = drawId++,
                        Object = obj3D
                    });
                }
            }

            _lastUpdateVersion = _sceneLayer != null ? _sceneLayer.Version : _scene.Version;

            Log.Debug(this, "Content Build");
        }

        protected virtual void ConfigureProgramInstance(GlProgramInstance instance)
        {

        }

        public void Prepare(RenderContext ctx)
        {
            if (ctx.Frame == _lastFrame)
                return;

            ComputeVisibility();

            if (_render.Options.SortByCameraDistance)
                ComputeDistance(ctx.Camera!);

            UpdateVertexHandlers();

            _lastFrame = ctx.Frame;
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

        protected void ComputeVisibility()
        {
            var updateContext = _render.UpdateContext;

            foreach (var content in _content.ShaderContents.SelectMany(a => a.Value.Contents.Values))
            {
                var allHidden = true;

                foreach (var draw in content.Contents)
                {
                    var progInst = draw.ProgramInstance!;

                    draw.IsHidden = !progInst.Material!.IsEnabled || !draw.Object!.IsVisible;

                    if (!draw.IsHidden && _render.Options.FrustumCulling && draw.Object is TriangleMesh mesh)
                        draw.IsHidden = !mesh.WorldBounds.IntersectFrustum(updateContext.FrustumPlanes!);

                    if (!draw.IsHidden)
                        allHidden = false;
                }

                content.IsHidden = allHidden;
            }
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

                    draw.Distance = draw.Object!.WorldBounds.DistanceTo(cameraPos);
                    count++;
                    sum += draw.Distance;
                }
                content.AvgDistance = sum / count;
            }
        }

        public void Dispose()
        {
            _content?.ShaderContents.Clear();
            _content?.Lights?.Clear();
            GC.SuppressFinalize(this);
        }

        public bool NeedUpdate => _lastUpdateVersion != Version;

        public GlLayerType Type => _type;

        public RenderContent Content => _content;

        public ILayer3D? SceneLayer => _sceneLayer;

        public bool HasLights { get; set; }

        public long Version => _sceneLayer != null ? _sceneLayer.Version : _scene.Version;
    }
}
