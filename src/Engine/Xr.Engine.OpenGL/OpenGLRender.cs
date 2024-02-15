#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;


namespace OpenXr.Engine.OpenGL
{
    public class GlobalContent
    {
        public DirectionalLight? Directional;

        public AmbientLight? Ambient;

        public PointLight? Point;

        public long Version;

        public Scene? Scene;

        public readonly Dictionary<Shader, ShaderContent> Contents = [];
    }

    public class ShaderContent
    {
        public GlProgram? Program;

        public readonly Dictionary<Geometry3D, VertexContent> Contents = [];
    }

    public class VertexContent
    {
        public GlVertexArray<VertexData, uint>? VertexArray;

        public readonly List<DrawContent> Contents = [];

        public long Version;
    }

    public class DrawContent
    {
        public Object3D? Object;

        public ShaderMaterial? Material;

        public Action? Draw;
    }

    public class OpenGLRender : IRenderEngine, IGlProgramFactory
    {
        protected GL _gl;
        protected GlVertexLayout _meshLayout;
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
            _meshLayout = GlVertexLayout.FromType<VertexData>();
            _options = options;
            _target = new GlDefaultRenderTarget(gl);
            Current = this;
        }

        GlProgram IGlProgramFactory.CreateProgram(GL gl, string vSource, string fSource, GlRenderOptions options)
        {
            return new GlProgram(_gl, vSource, fSource, _options);
        }

        protected GlProgram GetProgram(Shader shader, IGlProgramFactory programFactory)
        {
            return shader.GetResource(a =>
                    programFactory.CreateProgram(_gl, shader.VertexSource!, shader.FragmentSource!, _options));
        }

        public void Clear(Color color)
        {
            _gl.ClearColor(color.R, color.G, color.B, color.A);
            _gl.ClearDepth(1.0f);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }


        [MemberNotNull(nameof(_content))]
        protected void BuildContent(Scene scene, IGlProgramFactory programFactory)
        {
            if (_content == null)
                _content = new GlobalContent();

            _content.Ambient = scene.VisibleDescendants<AmbientLight>().FirstOrDefault();
            _content.Point = scene.VisibleDescendants<PointLight>().FirstOrDefault();
            _content.Scene = scene;
            _content.Version = scene.Version;

            _content.Contents.Clear();

            foreach (var mesh in scene.VisibleDescendants<Mesh>())
            {
                if (mesh.Geometry == null || mesh.Materials.Count == 0)
                    continue;

                foreach (var material in mesh.Materials.OfType<ShaderMaterial>())
                {
                    if (material.Shader == null)
                        continue;

                    if (!_content.Contents.TryGetValue(material.Shader, out var shaderContent))
                    {
                        shaderContent = new ShaderContent();
                        shaderContent.Program = GetProgram(material.Shader, programFactory);
                        _content.Contents[material.Shader] = shaderContent;
                    }

                    if (!shaderContent.Contents.TryGetValue(mesh.Geometry, out var vertexContent))
                    {
                        vertexContent = new VertexContent();
                        vertexContent.Version = mesh.Geometry.Version;
                        vertexContent.VertexArray = mesh.Geometry!.GetResource(geo =>
                                new GlVertexArray<VertexData, uint>(_gl, geo.Vertices!, geo.Indices!, _meshLayout));

                        shaderContent.Contents[mesh.Geometry] = vertexContent;
                    }

                    vertexContent.Contents.Add(new DrawContent
                    {
                        Draw = vertexContent!.VertexArray!.Draw,
                        Material = material,
                        Object = mesh
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
            target.Begin();

            Clear(camera.BackgroundColor);

            _gl.FrontFace(FrontFaceDirection.Ccw);
            _gl.CullFace(TriangleFace.Back);

            _gl.Viewport(view.X, view.Y, view.Width, view.Height);

            var targetProgramFactory = target as IGlProgramFactory;

            if (_content == null || _content.Scene != scene || _content.Version != scene.Version)
                BuildContent(scene, targetProgramFactory ?? this);

            foreach (var shader in _content.Contents)
            {
                var prog = shader.Value!.Program;

                prog!.Use();

                if (shader.Key.IsLit)
                {
                    if (_content.Ambient != null)
                        prog.SetAmbient(_content.Ambient);

                    if (_content.Point != null)
                        prog.AddPointLight(_content.Point!);
                }

                prog.SetCamera(camera);

                foreach (var vertex in shader.Value.Contents)
                {
                    if (vertex.Key.Version != vertex.Value.Version)
                    {
                        vertex.Value.VertexArray!.Update(vertex.Key.Vertices, vertex.Key.Indices);
                        vertex.Value.Version = vertex.Key.Version;
                    }

                    vertex.Value.VertexArray!.Bind();

                    foreach (var draw in vertex.Value.Contents)
                    {
                        ConfigureCaps(draw.Material!);

                        draw.Material!.UpdateUniforms(prog);

                        prog.SetUniform("uModel", draw.Object!.WorldMatrix);
                        draw.Draw!();
                    }

                    vertex.Value.VertexArray.Unbind();
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


        public GL GL => _gl;

        public IGlRenderTarget? RenderTarget => _target;

        public static OpenGLRender? Current { get; internal set; }

    }
}
