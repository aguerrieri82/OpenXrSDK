#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using Xr.Engine.OpenGL;


namespace OpenXr.Engine.OpenGL
{
    public class GlobalContent
    {

        public AmbientLight? Ambient;

        public PointLight[]? Points;

        public SpotLight[]? Spots;

        public DirectionalLight[]? Directionals;

        public long Version;

        public Scene? Scene;

        public readonly Dictionary<Shader, ShaderContent> ShaderContents = [];
    }

    public class ShaderContent
    {
        public GlProgram? Program;

        public readonly Dictionary<Object3D, VertexContent> Contents = [];
    }

    public class VertexContent
    {
        public GlVertexSourceHandle? VertexHandler;

        public readonly List<DrawContent> Contents = [];

        public long Version;
    }

    public class DrawContent
    {
        public Object3D? Object;

        public ShaderMaterial? Material;

        public Action? Draw;
    }

    public class OpenGLRender : IRenderEngine
    {
        protected GL _gl;
        protected GlobalContent? _content;
        protected IGlRenderTarget? _target;
        protected GlRenderOptions _options;

        public static class Props
        {
            public const string GlResId = nameof(GlResId);
        }

        public OpenGLRender(GL gl)
            : this(gl, GlRenderOptions.Default)
        {
        }

        public OpenGLRender(GL gl, GlRenderOptions options)
        {
            _gl = gl;
            _options = options;
            _target = new GlDefaultRenderTarget(gl);
            _options.ProgramFactory ??= new GlDefaultProgramFactory();

            Current = this;
        }

        protected GlProgram GetProgram(ShaderMaterial material)
        {
            return material.Shader!.GetResource(a =>
                    _options.ProgramFactory!.CreateProgram(_gl, material, _options));
        }

        public void Clear(Color color)
        {
            _gl.ClearColor(color.R, color.G, color.B, color.A);
            _gl.ClearDepth(1.0f);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }


        [MemberNotNull(nameof(_content))]
        protected void BuildContent(Scene scene)
        {
            if (_content == null)
                _content = new GlobalContent();

            _content.Ambient = scene.VisibleDescendants<AmbientLight>().SingleOrDefault();
            _content.Points = scene.VisibleDescendants<PointLight>().ToArray();
            _content.Spots = scene.VisibleDescendants<SpotLight>().ToArray();
            _content.Directionals = scene.VisibleDescendants<DirectionalLight>().ToArray();
            _content.Scene = scene;
            _content.Version = scene.Version;

            _content.ShaderContents.Clear();

            foreach (var obj3D in scene.VisibleDescendants<Object3D>())
            {
                if (obj3D is not IVertexSource vrtSrc)
                    continue;

                foreach (var material in vrtSrc.Materials.OfType<ShaderMaterial>())
                {
                    if (material.Shader == null)
                        continue;

                    if (!_content.ShaderContents.TryGetValue(material.Shader, out var shaderContent))
                    {
                        shaderContent = new ShaderContent();
                        shaderContent.Program = GetProgram(material);
                        _content.ShaderContents[material.Shader] = shaderContent;
                    }

                    if (!shaderContent.Contents.TryGetValue(obj3D, out var vertexContent))
                    {
                        vertexContent = new VertexContent
                        {
                            Version = obj3D.Version,
                            VertexHandler = obj3D.GetResource(a => GlVertexSourceHandle.Create(_gl, vrtSrc))
                        };

                        shaderContent.Contents[obj3D] = vertexContent;
                    }

                    vertexContent.Contents.Add(new DrawContent
                    {
                        Draw = () => vertexContent!.VertexHandler!.Draw(),
                        Material = material,
                        Object = obj3D
                    });
                }
            }
        }

        public void EnableDebug()
        {
            _gl.DebugMessageCallback((source, type, id, sev, len, msg, param) =>
           {
               unsafe
               {
                   if (sev == GLEnum.DebugSeverityNotification)
                       return;
                   var span = new Span<byte>((void*)msg, len);
                   var text = Encoding.UTF8.GetString(span);
                   Debug.WriteLine($"\n\n\n");
                   Debug.WriteLine($"------ OPENGL: {text}");
                   Debug.WriteLine($"\n\n\n");
               }
           }, 0);

            _gl.Enable(EnableCap.DebugOutput);
        }

        public void EnableFeature(EnableCap cap, bool value)
        {
            if (value)
                _gl.Enable(cap);
            else
                _gl.Disable(cap);
        }

        protected void ConfigureCaps(ShaderMaterial material)
        {
            _gl.DepthMask(material.WriteDepth);
            EnableFeature(EnableCap.DepthTest, material.UseDepth);
            EnableFeature(EnableCap.CullFace, !material.DoubleSided);

            if (!material.WriteColor)
                _gl.ColorMask(false, false, false, false);
            else
                _gl.ColorMask(true, true, true, true);
        }

        public void Render(Scene scene, Camera camera, Rect2I view)
        {
            if (_target != null)
                Render(scene, camera, view, _target);
        }

        public void Render(Scene scene, Camera camera, Rect2I view, IGlRenderTarget target)
        {
            _view = view;

            target.Begin();

            Clear(camera.BackgroundColor);

            _gl.FrontFace(FrontFaceDirection.Ccw);
            _gl.CullFace(TriangleFace.Back);

            _gl.Viewport(view.X, view.Y, view.Width, view.Height);

            var targetOverride = target as IGlProgramOverride;

            if (_content == null || _content.Scene != scene || _content.Version != scene.Version)
                BuildContent(scene);

            foreach (var shader in _content.ShaderContents.OrderBy(a => a.Key.Priority))
            {
                var prog = shader.Value!.Program;

                prog!.BeginEdit();

                targetOverride?.BeginEdit(prog);

                if (shader.Key.IsLit)
                {
                    if (_content.Ambient != null)
                        prog.SetAmbient(_content.Ambient);

                    if (_content.Points != null)
                        foreach (var light in _content.Points)
                            prog.AddLight(light);
                    
                    if (_content.Spots != null)
                        foreach (var light in _content.Spots)
                            prog.AddLight(light);

                    if (_content.Directionals != null)
                        foreach (var light in _content.Directionals)
                            prog.AddLight(light);
                }

                foreach (var vertex in shader.Value.Contents)
                {
                    if (vertex.Key.Version != vertex.Value.Version)
                    {
                        vertex.Value.VertexHandler!.Update();
                        vertex.Value.Version = vertex.Key.Version;
                    }

                    vertex.Value.VertexHandler!.Bind();

                    prog.SetLayout(vertex.Value.VertexHandler.Layout);

                    foreach (var draw in vertex.Value.Contents)
                    {
                        ConfigureCaps(draw.Material!);

                        draw.Material!.ExtractFeatures(prog);

                        prog.Commit();

                        prog.Use();

                        if (targetOverride == null || !targetOverride.SetCamera(prog, camera))
                            prog.SetCamera(camera);

                        prog.ConfigureLights();

                        draw.Material!.UpdateUniforms(prog);

                        var det = draw.Object!.WorldMatrix.GetDeterminant();

                        prog.SetModel(draw.Object!.WorldMatrix);

                        draw.Draw!();
                    }

                    vertex.Value.VertexHandler.Unbind();
                }

                prog.Unbind();
            }

            target.End();
        }

        public void SetRenderTarget(IGlRenderTarget target)
        {
            _target = target;
        }

        public void Dispose()
        {
        }


        Dictionary<uint, Texture2D> _depthCache = [];
        private Rect2I _view;

        public Texture2D? GetDepth()
        {
            var depthId = _target?.QueryTexture(FramebufferAttachment.DepthAttachment);

            if (depthId == null || depthId == 0)
                return null;


            if (!_depthCache.TryGetValue(depthId.Value, out var texture))
            {
                var glTexture = new GlTexture2D(_gl, depthId.Value);
                glTexture.Bind();
                _gl.TexParameter(glTexture.Target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                glTexture.Unbind();

                texture = new Texture2D
                {
                    Width = glTexture.Width,
                    Height = glTexture.Height,
                    Format = TextureFormat.Depth24Float,
                    SampleCount = glTexture.SampleCount
                };
                texture.SetProp(Props.GlResId, glTexture);
                _depthCache[depthId.Value] = texture;
            }

            return texture;

        }

        public GL GL => _gl;

        public Rect2I View => _view;

        public IGlRenderTarget? RenderTarget => _target;

        public static OpenGLRender? Current { get; internal set; }

    }
}
