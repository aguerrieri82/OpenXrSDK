#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using System.Text;
using XrMath;
using SkiaSharp;
using System.Runtime.InteropServices;
using XrEngine.Layers;


namespace XrEngine.OpenGL
{
    public class GlobalContent
    {
        public IList<Light>? Lights;

        public long ImageLightVersion = -1;

        public long LightsVersion = -1;

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

        public GlQuery? Query;

        public bool IsHidden;
    }

    public class OpenGLRender : IRenderEngine, ISurfaceProvider, IIBLPanoramaProcessor, IFrameReader
    {
        public class GlState
        {
            public bool? WriteDepth;
            public bool? UseDepth;
            public bool? DoubleSided;
            public bool? WriteColor;
            public uint? ActiveProgram;
            public bool? Wireframe;
            public AlphaMode? Alpha;
            public Rect2I? LastView;
            public float? LineWidth;
        }

        protected GL _gl;
        protected IGlRenderTarget? _target;
        protected GlRenderOptions _options;
        protected GlDefaultRenderTarget _defaultTarget;
        protected UpdateShaderContext _updateCtx;
        protected Dictionary<uint, Texture2D> _depthCache = [];

        protected GlState _glState;
        protected DrawBufferMode[] _drawAttachment0;
        protected GRContext? _grContext;
        protected QueueDispatcher _dispatcher;
        protected GlTextureRenderTarget? _texRenderTarget = null;
        protected IList<GlLayer> _layers = [];
        protected Scene3D? _lastScene;
        protected long _lastLayersVersion;
        protected IList<IGlRenderPass> _renderPasses = [];   

        public static class Props
        {
            public const string GlResId = nameof(GlResId);

            public const string GlQuery = nameof(GlQuery);
        }

        public OpenGLRender(GL gl)
            : this(gl, GlRenderOptions.Default())
        {
        }

        public OpenGLRender(GL gl, GlRenderOptions options)
            : this(gl, options, new GlState())
        {
        }

        protected OpenGLRender(GL gl, GlRenderOptions options, GlState state)
        {
            Current = this;

            _glState = state;
            _gl = gl;
            _options = options;
            _defaultTarget = new GlDefaultRenderTarget(gl);
            _lastLayersVersion = -1;
            _target = _defaultTarget;

            _updateCtx = new UpdateShaderContext();
            _updateCtx.RenderEngine = this;

            _dispatcher = new QueueDispatcher();

            _drawAttachment0 = [DrawBufferMode.ColorAttachment0];

            ConfigureCaps();

            if (_options.UseDepthPass)
                _renderPasses.Add(new GlDepthPass(this)
                {
                    UseOcclusion = _options.UseOcclusionQuery
                });

            _renderPasses.Add(new GlColorPass(this));
        }

        public void Suspend()
        {
        }

        public void Resume()
        {
        }

        public void Clear(Color color)
        {
            if (_glState.WriteColor != true)
            {
                _glState.WriteColor = true;
                _gl.ColorMask(true, true, true, true);
            }

            if (_glState.WriteDepth != true)
            {
                _gl.DepthMask(true);
                _glState.WriteDepth = true;
            }

            _gl.ClearDepth(1.0f);
            _gl.ClearColor(color.R, color.G, color.B, color.A);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        protected internal void ResetState()
        {
            _glState = new GlState();

            GL.DrawBuffers(_drawAttachment0.AsSpan());
        }

        public unsafe void EnableDebug()
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
                   /*
                   Debug.WriteLine($"\n\n\n");
                   Debug.WriteLine($"------ OPENGL: {text}");
                   Debug.WriteLine($"\n\n\n");*/
               }
           }, null);

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
            EnableFeature(EnableCap.FramebufferSrgb, _options.UseSRGB);
            _gl.Enable(EnableCap.Dither);
            _gl.Enable(EnableCap.Multisample);
            _gl.Disable(EnableCap.ScissorTest);
        }

        public void ConfigureCaps(ShaderMaterial material)
        {
            if (_glState.UseDepth != material.UseDepth || _glState.WriteDepth != material.WriteDepth)
                EnableFeature(EnableCap.DepthTest, material.UseDepth || material.WriteDepth);

            if (_glState.UseDepth != material.UseDepth)
            {
                if (!material.UseDepth)
                    _gl.DepthFunc(DepthFunction.Always);
                else
                    _gl.DepthFunc(DepthFunction.Lequal);

                _glState.UseDepth = material.UseDepth;
            }

            if (_glState.WriteDepth != material.WriteDepth)
            {
                _gl.DepthMask(material.WriteDepth);
                _glState.WriteDepth = material.WriteDepth;
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
                EnableFeature(EnableCap.Blend, material.Alpha != AlphaMode.Opaque);
                _glState.Alpha = material.Alpha;
                _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            }

#if !GLES
            bool isWireframe = material is WireframeMaterial;
            if (isWireframe != _glState.Wireframe)
            {
                if (isWireframe)
                    _gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
                else
                {
                    _gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
                    _gl.CullFace(TriangleFace.Back);
                }
                _glState.Wireframe = isWireframe;
            }
#endif

            if (material is ILineMaterial line)
            {
                if (line.LineWidth != _glState.LineWidth)
                {
                    _gl.LineWidth(line.LineWidth);
                    _glState.LineWidth = line.LineWidth;
                }
            }
        }

        public void Render(Scene3D scene, Camera camera, Rect2I view, bool flush)
        {
            if (_target != null)
                Render(scene, camera, view, _target);
        }

        protected void UpdateLayers(Scene3D scene)
        {
            if (_lastScene != scene || _lastLayersVersion != scene.Layers.Version)
            {
                _layers.Clear();

                _layers.Add(new GlLayer(this, scene));

                foreach (var layer in scene.Layers.Layers.OfType<DetachedLayer>())
                    _layers.Add(new GlLayer(this, scene, layer));

                _lastScene = scene;
                _lastLayersVersion = scene.Layers.Version;
            }

            foreach (var layer in _layers)
            {
                if (layer.NeedUpdate)
                    layer.Update();
            }
        }

        public void Render(Scene3D scene, Camera camera, Rect2I view, IGlRenderTarget target)
        {
            target.Begin();

            if (_glState.LastView == null || !_glState.Equals(view))
            {
                _gl.Viewport(view.X, view.Y, view.Width, view.Height);
                _gl.Scissor(view.X, view.Y, view.Width, view.Height);

                _glState.LastView = view;
            }

            Clear(camera.BackgroundColor);

            UpdateLayers(scene);

            var mainLayer = _layers[0]; 

            _updateCtx.Camera = camera.Clone();
            _updateCtx.Lights = mainLayer.Content.Lights;
            _updateCtx.LightsVersion = mainLayer.Content.LightsVersion;
            _updateCtx.FrustumPlanes = camera.FrustumPlanes();

            foreach (var pass in _renderPasses)
            {
                _gl.PushDebugGroup(DebugSource.DebugSourceApplication, 0, unchecked((uint)-1), $"Begin Pass {pass.GetType().Name}");

                for (var i = _layers.Count - 1; i >= 0; i--)
                {
                    var content = _layers[i].Content;
                    pass.RenderContent(content);  
                }

                _gl.PopDebugGroup();
            }

            _glState.ActiveProgram = null;

            _gl.UseProgram(0);

            target.End();

            _dispatcher.ProcessQueue();
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
                var glTexture = GlTexture.Attach(_gl, depthId.Value);
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
            ResetState();

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
            var glTexture = texture.GetGlResource(a =>
            {
                if (handle == 0)
                    return texture.CreateGlTexture(_gl, _options.RequireTextureCompression);

                return GlTexture.Attach(_gl, (uint)handle);
            });

            glTexture.Update(texture, false);


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


        public unsafe string[] GetExtensions()
        {
            var data = _gl.GetString(StringName.Extensions);
            var allExt = Marshal.PtrToStringAuto(new nint(data))!;
            return allExt.Split(' ');
        }


        public PbrMaterial.IBLTextures ProcessPanoramaIBL(TextureData data, PanoramaProcessorOptions options)
        {
            Log.Info(this, "Processing IBL Panorama");

            using var processor = new GlIBLProcessor(_gl);

            processor.Resolution = options.Resolution;
            processor.SampleCount = options.SampleCount;
            processor.LodBias = options.LodBias;
            processor.MipLevelCount = options.MipLevelCount;

            processor.Initialize(data, options.ShaderResolver!);

            processor.PanoramaToCubeMap();

            var result = new PbrMaterial.IBLTextures
            {
                MipCount = processor.MipLevelCount
            };

            uint envId, lutId;


            result.Env = (TextureCube)_gl.TexIdToEngineTexture(processor.OutCubeMapId);


            if ((options.Mode & IBLProcessMode.Lambertian) == IBLProcessMode.Lambertian)
            {
                processor.ApplyFilter(GlIBLProcessor.Distribution.Lambertian, out envId, out lutId);

                result.LambertianEnv = (TextureCube)_gl.TexIdToEngineTexture(envId);

            }
            if ((options.Mode & IBLProcessMode.Charlie) == IBLProcessMode.Charlie)
            {
                processor.ApplyFilter(GlIBLProcessor.Distribution.Charlie, out envId, out lutId);

                result.CharlieEnv = (TextureCube)_gl.TexIdToEngineTexture(envId);
                result.CharlieLUT = (Texture2D)_gl.TexIdToEngineTexture(lutId);

                /*
                result.CharlieEnv = (TextureCube)_gl.TexIdToEngineTexture(envId, TextureFormat.RgbFloat);
                result.CharlieLUT = (Texture2D)_gl.TexIdToEngineTexture(lutId, TextureFormat.SRgb24);

                using (var out1 = File.OpenWrite("d:\\charlie.pvr"))
                    PvrTranscoder.Instance.Write(out1, result.CharlieEnv.Data!);

                using (var out1 = File.OpenWrite("d:\\charlie-lut.pvr"))
                    PvrTranscoder.Instance.Write(out1, result.CharlieLUT.Data!);
                */
            }
            if ((options.Mode & IBLProcessMode.GGX) == IBLProcessMode.GGX)
            {
                processor.ApplyFilter(GlIBLProcessor.Distribution.GGX, out envId, out lutId);

                result.GGXEnv = (TextureCube)_gl.TexIdToEngineTexture(envId);
                result.GGXLUT = (Texture2D)_gl.TexIdToEngineTexture(lutId);
            }

            Log.Debug(this, "Processing IBL Panorama OK");

            return result;
        }

        public void SetRenderTarget(Texture2D texture)
        {
            var glTexture = texture.GetGlResource(tex => tex.CreateGlTexture(_gl, false));
            _texRenderTarget ??= new GlTextureRenderTarget(_gl);
            _texRenderTarget.FrameBuffer.Configure(glTexture, null);
            _target = _texRenderTarget;
        }

        public TextureData ReadFrame()
        {
            if (_target is not GlTextureRenderTarget texTarget)
                throw new NotSupportedException();

            if (texTarget.FrameBuffer is not GlTextureFrameBuffer texFb)
                throw new NotSupportedException();

            return texFb.ReadColor();
        }

        public GL GL => _gl;

        public GlState State => _glState;

        public UpdateShaderContext UpdateContext => _updateCtx;

        public IDispatcher Dispatcher => _dispatcher;

        public IGlRenderTarget? RenderTarget => _target;

        public GlRenderOptions Options => _options;

        public static OpenGLRender? Current { get; internal set; }

        public static int SuspendErrors { get; set; }
    }
}
