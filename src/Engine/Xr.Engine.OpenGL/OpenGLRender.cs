#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;


namespace Xr.Engine.OpenGL
{
    public class GlobalContent
    {

        public Light[]? Lights;

        public long Version;

        public Scene? Scene;

        public readonly Dictionary<Shader, ShaderContent> ShaderContents = [];
    }

    public class ShaderContent
    {
        public IShaderHandler? GlobalHandler;

        public GlProgramGlobal? ProgramGlobal;

        public readonly Dictionary<Object3D, VertexContent> Contents = [];
    }

    public class VertexContent
    {
        public GlVertexSourceHandle? VertexHandler;

        public VertexComponent ActiveComponents;

        public readonly List<DrawContent> Contents = [];

        public long Version;
    }

    public class DrawContent
    {
        public Object3D? Object;

        public Action? Draw;
        
        public int DrawId;

        public GlProgramInstance? ProgramInstance;
    }


    public class OpenGLRender : IRenderEngine
    {
        protected struct GlState
        {
            public bool WriteDepth;
            public bool UseDepth;
            public bool DoubleSided;
            public bool WriteColor;
            public uint ActiveProgram;
        }

        protected GL _gl;
        protected GlobalContent? _content;
        protected IGlRenderTarget? _target;
        protected GlRenderOptions _options;
        protected GlDefaultRenderTarget _defaultTarget;
        protected UpdateShaderContext _updateCtx;
        protected GlSimpleProgram? _writeStencil;
        protected ShaderUpdate? _writeStencilUpdate;
        protected Dictionary<uint, Texture2D> _depthCache = [];
        protected Rect2I _view;
        protected GlState _glState;


        public static class Props
        {
            public const string GlResId = nameof(GlResId);
        }

        public OpenGLRender(GL gl)
            : this(gl, GlRenderOptions.Default())
        {
        }

        public OpenGLRender(GL gl, GlRenderOptions options)
        {
            Current = this;

            _gl = gl;
            _options = options;
            _defaultTarget = new GlDefaultRenderTarget(gl);
            _target = _defaultTarget;

            _updateCtx = new UpdateShaderContext();
            _updateCtx.RenderEngine = this;

            _gl.FrontFace(FrontFaceDirection.Ccw);
            _gl.CullFace(TriangleFace.Back);
            _gl.LineWidth(1);
        }

        public void ReleaseContext(bool release)
        {
        }

        public void Clear(Color color)
        {
            _gl.ClearDepth(1.0f);
            _gl.ClearColor(color.R, color.G, color.B, color.A);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }


        [MemberNotNull(nameof(_content))]
        protected void BuildContent(Scene scene)
        {
            if (_content == null)
                _content = new GlobalContent();

            _content.Lights = scene.VisibleDescendants<Light>().ToArray();
            _content.Scene = scene;
            _content.Version = scene.Version;

            _content.ShaderContents.Clear();

            var drawId = 0;

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
                        shaderContent = new ShaderContent
                        {
                            ProgramGlobal = material.Shader.GetResource(gl => new GlProgramGlobal(_gl, material.GetType()))
                        };

                        _content.ShaderContents[material.Shader] = shaderContent;
                    }

                    if (!shaderContent.Contents.TryGetValue(obj3D, out var vertexContent))
                    {
                        vertexContent = new VertexContent
                        {
                            Version = obj3D.Version,
                            VertexHandler = vrtSrc.Object.GetResource(a => GlVertexSourceHandle.Create(_gl, vrtSrc)),
                            ActiveComponents = VertexComponent.None
                        };

                        foreach (var attr in vertexContent.VertexHandler.Layout!.Attributes!)
                            vertexContent.ActiveComponents |= attr.Component;

                        shaderContent.Contents[obj3D] = vertexContent;
                    }

                    vertexContent.Contents.Add(new DrawContent
                    {
                        Draw = () => vertexContent!.VertexHandler!.Draw(),
                        ProgramInstance = material.GetResource(gl => new GlProgramInstance(_gl, material, shaderContent.ProgramGlobal!)),
                        DrawId = drawId++,
                        Object = obj3D
                    });
                }
            }
        }

        public void EnableDebug()
        {
            _gl.DebugMessageCallback((source, type, id, sev, len, msg, param) =>
           {
               if (SuspendErrors > 0)
                   return;

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
            if (_glState.WriteDepth != material.WriteDepth)
            {
                _gl.DepthMask(material.WriteDepth);
                _glState.WriteDepth = material.WriteDepth;  
            }
    
            if (_glState.UseDepth != material.UseDepth)
            {
                EnableFeature(EnableCap.DepthTest, material.UseDepth);
                _glState.UseDepth = material.UseDepth;  
            }

            if (_glState.DoubleSided != material.DoubleSided)
            {
                EnableFeature(EnableCap.CullFace, material.DoubleSided);
                _glState.DoubleSided = material.DoubleSided;
            }

            if (_glState.WriteColor != material.WriteColor)
            {
                if (!material.WriteColor)
                    _gl.ColorMask(false, false, false, false);
                else
                    _gl.ColorMask(true, true, true, true);

                _glState.WriteColor = material.WriteColor;
            }
        }

        public void Render(Scene scene, Camera camera, Rect2I view, bool flush)
        {
            if (_target != null)
                Render(scene, camera, view, _target);
        }

        public void Render(Scene scene, Camera camera, Rect2I view, IGlRenderTarget target)
        {
            target.Begin();

            Clear(camera.BackgroundColor);

            if (!_view.Equals(view))
            {
                _gl.Viewport(view.X, view.Y, view.Width, view.Height);

                _view = view;
            }

            var targetHandler = target as IShaderHandler;

            if (_content == null || _content.Scene != scene || _content.Version != scene.Version)
                BuildContent(scene);

            _updateCtx.Camera = camera;
            _updateCtx.Lights = _content.Lights;
            _updateCtx.LightsVersion = _content.Version;

            int skipCount = 0;

            foreach (var shader in _content.ShaderContents.OrderBy(a => a.Key.Priority))
            {
                var progGlobal = shader.Value!.ProgramGlobal;

                progGlobal!.UpdateProgram(_updateCtx, _target as IShaderHandler);

                foreach (var vertex in shader.Value.Contents)
                {
                    var vHandler = vertex.Value.VertexHandler!;

                    if (vertex.Key is TriangleMesh mesh)
                    {
                        if (!camera.CanSee(mesh.WorldBounds))
                        {
                            skipCount++;
                           // continue;
                        }
                    }

                    if (vertex.Key.Version != vertex.Value.Version)
                    {
                        vHandler.Update();
                        vertex.Value.Version = vertex.Key.Version;
                    }

                    vHandler.Bind();

                    _updateCtx.ActiveComponents = vertex.Value.ActiveComponents;

                    foreach (var draw in vertex.Value.Contents)
                    {
                        var progInst = draw.ProgramInstance!;

                        ConfigureCaps(draw.ProgramInstance!.Material!);

                        _updateCtx.Model = draw.Object;

                        progInst.UpdateProgram(_updateCtx);

                        if (_glState.ActiveProgram != progInst.Program!.Handle)
                        {
                            progInst.Program!.Use();
                            _glState.ActiveProgram = progInst.Program!.Handle;
                        }
                 
                        progInst.UpdateUniforms(_updateCtx);

                        draw.Draw!();
                    }
                }
            }

            _glState.ActiveProgram = 0;

            _gl.UseProgram(0);
            
            _gl.BindVertexArray(0);

            target.End();
        }

        public void SetRenderTarget(IGlRenderTarget target)
        {
            _target = target;
        }

        public void SetDefaultRenderTarget()
        {
            _target = _defaultTarget;
        }

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
                    Format = TextureFormat.Depth24Float, //TODO is not true
                    SampleCount = glTexture.SampleCount
                };
                texture.SetProp(Props.GlResId, glTexture);
                _depthCache[depthId.Value] = texture;
            }

            return texture;

        }
        public void Dispose()
        {
        }

        public GL GL => _gl;

        public Rect2I View => _view;

        public IGlRenderTarget? RenderTarget => _target;

        public GlRenderOptions Options => _options;

        public static OpenGLRender? Current { get; internal set; }

        public static int SuspendErrors { get; set; }
    }
}
