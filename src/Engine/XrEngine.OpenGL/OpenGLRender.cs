#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using XrMath;
using SkiaSharp;


namespace XrEngine.OpenGL
{
    public class GlobalContent
    {
        public IList<Light>? Lights;

        public long LightsVersion;

        public long SceneVersion;

        public Scene? Scene;

        public readonly Dictionary<Shader, ShaderContent> ShaderContents = [];

        public readonly List<KeyValuePair<Shader, ShaderContent>> ShaderContentsOrder = [];
    }

    public class ShaderContent
    {
        public IShaderHandler? GlobalHandler;

        public GlProgramGlobal? ProgramGlobal;

        public readonly Dictionary<EngineObject, VertexContent> Contents = [];
    }

    public class VertexContent
    {
        public GlVertexSourceHandle? VertexHandler;

        public VertexComponent ActiveComponents;

        public readonly List<DrawContent> Contents = [];
    }

    public class DrawContent
    {
        public Object3D? Object;

        public Action? Draw;

        public int DrawId;

        public GlProgramInstance? ProgramInstance;
    }

    public class OpenGLRender : IRenderEngine, ISurfaceProvider
    {
        protected struct GlState
        {
            public bool? WriteDepth;
            public bool? UseDepth;
            public bool? DoubleSided;
            public bool? WriteColor;
            public uint? ActiveProgram;
            public AlphaMode? Alpha;
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
        protected Rect2I _lastView;
        protected GlState _glState;
        protected GRContext? _grContext;

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

            ConfigureCaps();
        }

        public void Suspend()
        {
        }

        public void Resume()
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

            _content.Lights = new List<Light>();    
            _content.Scene = scene;
            _content.SceneVersion = scene.Version;
            _content.LightsVersion = 0;

            _content.ShaderContents.Clear();

            var drawId = 0;

            foreach (var obj3D in scene.VisibleDescendants<Object3D>())
            {
                if (obj3D is Light light)
                {
                    _content.Lights.Add(light);
                    _content.LightsVersion += light.Version;
                    continue;
                }
           
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
                            ProgramGlobal = material.Shader.GetResource(gl => new GlProgramGlobal(_gl, material.GetType()))
                        };

                        _content.ShaderContents[material.Shader] = shaderContent;
                    }

                    if (!shaderContent.Contents.TryGetValue(vrtSrc.Object, out var vertexContent))
                    {
                        vertexContent = new VertexContent
                        {
                            VertexHandler = vrtSrc.Object.GetResource(a => GlVertexSourceHandle.Create(_gl, vrtSrc)),
                            ActiveComponents = VertexComponent.None
                        };

                        foreach (var attr in vertexContent.VertexHandler.Layout!.Attributes!)
                            vertexContent.ActiveComponents |= attr.Component;

                        shaderContent.Contents[vrtSrc.Object] = vertexContent;
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

            //_content.ShaderContentsOrder.Clear();
            //_content.ShaderContentsOrder.AddRange(_content.ShaderContents);
        }

        public void EnableDebug()
        {
            _gl.DebugMessageCallback((source, type, id, sev, len, msg, param) =>
           {
               if (SuspendErrors > 0)
                   return;

               unsafe
               {
                   var span = new Span<byte>((void*)msg, len);
                   var text = Encoding.UTF8.GetString(span);

                   if (sev == GLEnum.DebugSeverityNotification)
                       return;

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

        protected void ConfigureCaps()
        {
            _gl.FrontFace(FrontFaceDirection.Ccw);
            _gl.CullFace(TriangleFace.Back);
            _gl.Enable(EnableCap.FramebufferSrgb);
            _gl.Enable(EnableCap.Dither);
            _gl.Enable(EnableCap.Multisample);
            _gl.Disable(EnableCap.ScissorTest);
            _gl.LineWidth(1);
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
                EnableFeature(EnableCap.CullFace, !material.DoubleSided);
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

            if (_glState.Alpha != material.Alpha)
            {
                EnableFeature(EnableCap.Blend, material.Alpha == AlphaMode.Blend);
                _glState.Alpha = material.Alpha;
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
      
            if (!_lastView.Equals(view))
            {
                _gl.Viewport(view.X, view.Y, view.Width, view.Height);

                _lastView = view;
            }

            Clear(camera.BackgroundColor);

            var targetHandler = target as IShaderHandler;

            if (_content == null || _content.Scene != scene || _content.SceneVersion != scene.Version)
                BuildContent(scene);

            _updateCtx.Camera = camera;
            _updateCtx.Lights = _content.Lights;
            _updateCtx.LightsVersion = _content.LightsVersion;

            int skipCount = 0;

            foreach (var shader in _content.ShaderContents)
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

                    if (vHandler.NeedUpdate)
                        vHandler.Update();

                    _updateCtx.ActiveComponents = vertex.Value.ActiveComponents;

                    vHandler.Bind();

                    foreach (var draw in vertex.Value.Contents)
                    {
                        var progInst = draw.ProgramInstance!;

                        ConfigureCaps(draw.ProgramInstance!.Material!);

                        _updateCtx.Model = draw.Object;

                        progInst.UpdateProgram(_updateCtx);

                        bool updateGlobals = false;

                        if (_glState.ActiveProgram != progInst.Program!.Handle)
                        {
                            progInst.Program!.Use();
                            _glState.ActiveProgram = progInst.Program!.Handle;
                            updateGlobals = true;
                        }

                        progInst.UpdateUniforms(_updateCtx, updateGlobals);

                        draw.Draw!();
                    }

                    vHandler.Unbind();
                }
            }

            _glState.ActiveProgram = null;

            _gl.UseProgram(0);

            target.End();

            _gl.Finish();
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

        public void BeginDrawSurface()
        {
            var fence = _gl.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
            _gl.WaitSync(fence, SyncBehaviorFlags.None, unchecked((ulong)-1));
            _grContext!.ResetContext(GRGlBackendState.All);
        }

        public void EndDrawSurface()
        {
            _grContext!.Submit(true);

            _glState = new GlState();

            _gl.BindVertexArray(0);
            _gl.UseProgram(0);

            _gl.Disable(EnableCap.Blend);
            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
            _gl.BindSampler(0, 0);
            
            _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            
            _gl.Disable(EnableCap.ProgramPointSize);

            ConfigureCaps();
        }

        public SKSurface CreateSurface(Texture2D texture, nint handle = 0)
        {
            var glTexture = texture.GetResource(a =>
            {
                if (handle == 0)
                    return texture.CreateGlTexture(_gl, _options.RequireTextureCompression);

                return new GlTexture2D(_gl, (uint)handle);
            });

            if (_grContext == null)
            {
#if GLES
                var grInterface = GRGlInterface.CreateGles(name =>
                {
                    return _gl.Context.GetProcAddress(name);
                });
#else
                var grInterface = GRGlInterface.CreateOpenGl(name =>
                {
                    return _gl.Context.GetProcAddress(name);
                });

                #endif

                _grContext = GRContext.CreateGl(grInterface);
            }

            var format = glTexture.InternalFormat;

            if (format == InternalFormat.Rgba || format == 0)
                format = InternalFormat.Rgba8;

            var gerTextInfo = new GRGlTextureInfo((uint)glTexture.Target, glTexture.Handle, (uint)format); 

            var grTexture = new GRBackendTexture((int)glTexture.Width, (int)glTexture.Height, true, gerTextInfo);

            var props = new SKSurfaceProperties(SKPixelGeometry.RgbVertical);
              
            return SKSurface.Create(_grContext, grTexture, ImageUtils.GetFormat(texture.Format), props);
        }

        public GL GL => _gl;

        public IGlRenderTarget? RenderTarget => _target;

        public GlRenderOptions Options => _options;

        public static OpenGLRender? Current { get; internal set; }

        public static int SuspendErrors { get; set; }
    }
}
