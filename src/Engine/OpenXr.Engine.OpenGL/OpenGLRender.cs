#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using static OpenXr.Engine.KtxReader;



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

        public readonly Dictionary<Geometry, VertexContent> Contents = [];
    }

    public class VertexContent
    {
        public GlVertexArray<VertexData, uint>? VertexArray;

        public readonly List<DrawContent> Contents = [];
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
        protected Dictionary<uint, GlFrameTextureBuffer> _imagesFrameBuffers;
        protected GlFrameTextureBuffer? _targetFrameBuffer;
        protected GlFrameTextureBuffer? _renderFrameBuffer;
        protected GlFrameTextureBuffer? _msaaFrameBuffer;
        protected GlVertexLayout _meshLayout;
        protected GlobalContent? _content;

        public static class Props
        {
            public const string GlResId = nameof(GlResId);
        }

        public OpenGLRender(GL gl)
        {
            _gl = gl;
            _imagesFrameBuffers = new Dictionary<uint, GlFrameTextureBuffer>();
            _meshLayout = GlVertexLayout.FromType<VertexData>();
        }

        protected GlProgram GetProgram(Shader shader)
        {
            return shader.GetResource(a => new GlProgram(_gl, shader.VertexSource!, shader.FragmentSource!));
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
                        shaderContent.Program = GetProgram(material.Shader);
                        _content.Contents[material.Shader] = shaderContent;
                    }

                    if (!shaderContent.Contents.TryGetValue(mesh.Geometry, out var vertexContent))
                    {
                        vertexContent = new VertexContent();
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

        public void Render(Scene scene, Camera camera, RectI view)
        {
            var renderFb = _renderFrameBuffer ?? _targetFrameBuffer;

            if (renderFb != null)
                renderFb.BindDraw();

            Clear(camera.BackgroundColor);

            _gl.FrontFace(FrontFaceDirection.Ccw);
            _gl.CullFace(TriangleFace.Back);
            _gl.Enable(EnableCap.Multisample);

            _gl.Viewport(view.X, view.Y, view.Width, view.Height);

            if (_content == null || _content.Scene != scene || _content.Version != scene.Version)
                BuildContent(scene);

            foreach (var shader in _content.Contents)
            {
                var prog = shader.Value!.Program;

                prog!.Use();

                if (shader.Key.IsLit)
                {
                    if (_content.Ambient != null)
                        prog.SetUniform("light.ambient", (Vector3)_content.Ambient.Color * _content.Ambient.Intensity);

                    if (_content.Point != null)
                    {
                        var wordPos = Vector3.Transform(_content.Point.Transform.Position, _content.Point.WorldMatrix);

                        prog.SetUniform("light.diffuse", (Vector3)_content.Point.Color * _content.Point.Intensity);
                        prog.SetUniform("light.position", wordPos);
                        prog.SetUniform("light.specular", (Vector3)_content.Point.Specular);
                    }
                }

                prog.SetUniform("uView", camera.Transform.Matrix);
                prog.SetUniform("uProjection", camera.Projection);
                prog.SetUniform("viewPos", camera.Transform.Position, true);

                foreach (var vertex in shader.Value.Contents.Values)
                {
                    vertex.VertexArray!.Bind();

                    foreach (var draw in vertex.Contents)
                    {
                        ConfigureCaps(draw.Material!);

                        draw.Material!.UpdateUniforms(prog);

                        prog.SetUniform("uModel", draw.Object!.WorldMatrix);
                        draw.Draw!();
                    }

                    vertex.VertexArray.Unbind();
                }

                prog.Unbind();
            }


            if (renderFb != null)
            {
                renderFb.Unbind();

                if (_targetFrameBuffer != null && renderFb != _targetFrameBuffer)
                    renderFb.CopyTo(_targetFrameBuffer);
            }
        }

        public void SetImageTarget(uint image, uint sampleCount)
        {
            if (!_imagesFrameBuffers.TryGetValue(image, out var frameBuffer))
            {
                frameBuffer = new GlFrameTextureBuffer(_gl, new GlTexture2D(_gl, image), sampleCount == 1);
                _imagesFrameBuffers[image] = frameBuffer;
            }

            if (sampleCount > 1)
            {
                if (_msaaFrameBuffer == null)
                {
                    var msaaTex = _gl.GenTexture();

                    _gl.BindTexture(TextureTarget.Texture2DMultisample, msaaTex);   
                    _gl.TexStorage2DMultisample(
                        TextureTarget.Texture2DMultisample,
                        sampleCount,
                        (SizedInternalFormat)frameBuffer.Color.InternalFormat,
                        frameBuffer.Color.Width,
                        frameBuffer.Color.Height, 
                        true);

                    _msaaFrameBuffer = new GlFrameTextureBuffer(_gl, 
                        new GlTexture2D(_gl, msaaTex, sampleCount), true, sampleCount);
                }
                  
                _renderFrameBuffer = _msaaFrameBuffer;
            }

            _targetFrameBuffer = frameBuffer;
        }

        public void Dispose()
        {
        }
    }
}
