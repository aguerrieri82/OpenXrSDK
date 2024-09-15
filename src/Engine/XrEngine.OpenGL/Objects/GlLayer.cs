
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.OpenGL
{
    public class GlLayer
    {
        readonly OpenGLRender _render;
        readonly GlobalContent _content;
        private readonly Scene3D _scene;
        private readonly ILayer3D? _layer;
        private long _lastUpdateVersion;

        public GlLayer(OpenGLRender render, Scene3D scene, ILayer3D? layer = null)
        {
            _render = render;
            _content = new GlobalContent();
            _scene = scene;
            _lastUpdateVersion = -1;
            _layer = layer;
        }


        public void Update()
        {
            Log.Info(this, "Building content '{0}' ({1})...", _scene.Name ?? "",  _layer?.Name ?? "Main");

            _content.Lights = [];
            _content.ShaderContents.Clear();
            _content.LightsVersion++;

            var drawId = 0;

            if (IsMain)
            {
                foreach (var light in _scene.VisibleDescendants<Light>())
                {
                    _content.Lights.Add(light);
                    if (light is ImageLight imgLight)
                    {
                        if (imgLight.Panorama?.Data != null && imgLight.Panorama.Version != _content.ImageLightVersion)
                        {
                            var options = PanoramaProcessorOptions.Default();
                            options.SampleCount = 1024;
                            options.Resolution = 256;
                            options.Mode = IBLProcessMode.GGX | IBLProcessMode.Lambertian;
                            imgLight.Textures = _render.ProcessPanoramaIBL(imgLight.Panorama.Data[0], options);
                            imgLight.Panorama.NotifyLoaded();
                            _content.ImageLightVersion = imgLight.Panorama.Version;
                            _render.ResetState();
                        }
                    }
                }
            }


            var objects = _layer != null ?
                _layer.Content.OfType<Object3D>().Where(a => a.IsVisible) :
                _scene.VisibleDescendants<Object3D>();


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

                    if (!_content.ShaderContents.TryGetValue(material.Shader, out var shaderContent))
                    {
                        shaderContent = new ShaderContent
                        {
                            ProgramGlobal = material.Shader.GetGlResource(gl => new GlProgramGlobal(_render.GL, material.GetType()))
                        };

                        _content.ShaderContents[material.Shader] = shaderContent;
                    }

                    if (!shaderContent.Contents.TryGetValue(vrtSrc.Object, out var vertexContent))
                    {
                        vertexContent = new VertexContent
                        {
                            VertexHandler = vrtSrc.Object.GetGlResource(a => GlVertexSourceHandle.Create(_render.GL, vrtSrc)),
                            ActiveComponents = VertexComponent.None
                        };

                        foreach (var attr in vertexContent.VertexHandler.Layout!.Attributes!)
                            vertexContent.ActiveComponents |= attr.Component;

                        shaderContent.Contents[vrtSrc.Object] = vertexContent;
                    }

                    vertexContent.Contents.Add(new DrawContent
                    {
                        Draw = () => vertexContent!.VertexHandler!.Draw(material.Shader.ForcePrimitive),
                        ProgramInstance = new GlProgramInstance(_render.GL, material, shaderContent.ProgramGlobal!),
                        DrawId = drawId++,
                        Object = obj3D
                    });
                }
            }

            //_content.ShaderContentsOrder.Clear();
            //_content.ShaderContentsOrder.AddRange(_content.ShaderContents);

            _lastUpdateVersion = _layer != null ? _layer.Version : _scene.Version;

            Log.Debug(this, "Content Build");

        }

        public bool NeedUpdate => _layer != null ?
            _lastUpdateVersion != _layer.Version :
            _lastUpdateVersion != _scene.Version;


        public bool IsMain => _layer == null;
        public GlobalContent Content => _content;
    }
}
