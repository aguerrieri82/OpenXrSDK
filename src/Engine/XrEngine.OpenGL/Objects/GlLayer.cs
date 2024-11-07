using XrMath;

namespace XrEngine.OpenGL
{
    public enum GlLayerType
    {
        Main,
        CastShadow,
        Blend,
        Custom
    }

    public class GlLayer : IDisposable
    {
        readonly OpenGLRender _render;
        readonly RenderContent _content;
        private readonly Scene3D _scene;
        private readonly ILayer3D? _layer;
        private readonly GlLayerType _type;
        private long _lastUpdateVersion;
        private long _lastFrame;

        public GlLayer(OpenGLRender render, Scene3D scene, GlLayerType type, ILayer3D? layer = null)
        {
            _render = render;
            _content = new RenderContent();
            _scene = scene;
            _lastUpdateVersion = -1;
            _layer = layer;
            _type = type;
        }


        public void Update()
        {
            Log.Info(this, "Building content '{0}' ({1})...", _scene.Name ?? "", _layer?.Name ?? "Main");

            _content.Lights = [];
            _content.ShaderContents.Clear();
            _content.LightsHash = "";
            _content.LayerVersion = Version;

            var drawId = 0;

            if (_type == GlLayerType.Main)
            {
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


            var objects = _layer != null ?
                _layer.Content.OfType<Object3D>().Visible() :
                _scene.Descendants().Visible();

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

                    if ((material.Alpha == AlphaMode.Blend && Type != GlLayerType.Blend) ||
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

                    vertexContent.Contents.Add(new DrawContent
                    {
                        Draw = () => vertexContent!.VertexHandler!.Draw(material.Shader.ForcePrimitive),
                        ProgramInstance = new GlProgramInstance(_render.GL, material, shaderContent.ProgramGlobal!, obj3D),
                        DrawId = drawId++,
                        Object = obj3D
                    });
                }
            }

            _lastUpdateVersion = _layer != null ? _layer.Version : _scene.Version;

            Log.Debug(this, "Content Build");

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

                    draw.Distance = draw.Object!.DistanceTo(cameraPos);
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

        public long Version => _layer != null ? _layer.Version : _scene.Version;
    }
}
