#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Text;
using XrMath;
using SkiaSharp;
using System.Runtime.InteropServices;
using XrEngine.Layers;

namespace XrEngine.OpenGL
{
    public class OpenGLRender : IRenderEngine, ISurfaceProvider, IIBLPanoramaProcessor, IFrameReader
    {


        protected IGlRenderTarget? _target;
        protected Rect2I _view;
        protected UpdateShaderContext _updateCtx;
        protected DrawBufferMode[] _drawAttachment0;
        protected GRContext? _grContext;
        protected GlTextureRenderTarget? _texRenderTarget = null;
        protected Scene3D? _lastScene;
        protected long _lastLayersVersion;
        protected GlLayer? _mainLayer;
        private int _maxTextureUnits;
        protected readonly GL _gl;
        protected readonly GlState _glState;
        protected readonly GlRenderOptions _options;
        protected readonly List<GlLayer> _layers = [];
        protected readonly QueueDispatcher _dispatcher;
        protected readonly IList<IGlRenderPass> _renderPasses = [];
        protected readonly GlDefaultRenderTarget _defaultTarget;

        public static class Props
        {
            public const string GlResId = nameof(GlResId);

            public const string GlQuery = nameof(GlQuery);
        }

        #region CONSTRUCTORS

        public OpenGLRender(GL gl)
            : this(gl, GlRenderOptions.Default())
        {
        }

        public OpenGLRender(GL gl, GlRenderOptions options)
            : this(gl, options, new GlState(gl))
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

            _updateCtx = new UpdateShaderContext
            {
                RenderEngine = this
            };

            _dispatcher = new QueueDispatcher();

            _drawAttachment0 = [DrawBufferMode.ColorAttachment0];

            if (_options.ShadowMap.Use)
                _renderPasses.Add(new GlShadowPass(this));  

            if (_options.UseDepthPass)
                _renderPasses.Add(new GlDepthPass(this)
                {
                    UseOcclusion = _options.UseOcclusionQuery
                });

            _renderPasses.Add(new GlColorPass(this));

            _gl.GetInteger(GetPName.MaxTextureImageUnits, out _maxTextureUnits);

            ConfigureCaps();
        }

        #endregion

        #region STATE

        protected internal void ResetState()
        {
            _glState.Reset();

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

        protected void ConfigureCaps()
        {
            _gl.FrontFace(FrontFaceDirection.Ccw);
            _glState.SetCullFace(TriangleFace.Back);
            _glState.EnableFeature(EnableCap.FramebufferSrgb, _options.UseSRGB);
            _glState.EnableFeature(EnableCap.Dither, true);
            _glState.EnableFeature(EnableCap.Multisample, true);
            _glState.EnableFeature(EnableCap.ScissorTest, false);
        }

        public void ConfigureCaps(ShaderMaterial material)
        {
            if (_glState.UseDepth != material.UseDepth || _glState.WriteDepth != material.WriteDepth)
                _glState.EnableFeature(EnableCap.DepthTest, material.UseDepth || material.WriteDepth);

            _glState.SetWriteDepth(material.UseDepth);
            _glState.SetWriteDepth(material.WriteDepth);
            _glState.SetDoubleSided(material.DoubleSided);
            _glState.SetWriteColor(material.WriteColor);
            _glState.SetAlphaMode(material.Alpha);
            _glState.SetWireframe(material is WireframeMaterial);   

            if (material is ILineMaterial line)
                _glState.SetLineWidth(line.LineWidth);  
        }

        #endregion

        #region RENDER

        public T? Pass<T>() where T : IGlRenderPass
        {
            return _renderPasses.OfType<T>().FirstOrDefault();
        }

        protected void UpdateLayers(Scene3D scene)
        {
            if (_lastScene != scene || _lastLayersVersion != scene.Layers.Version)
            {
                _layers.Clear();

                _mainLayer = new GlLayer(this, scene, GlLayerType.Main);

                foreach (var layer in scene.Layers.Layers.OfType<DetachedLayer>())
                    _layers.Add(new GlLayer(this, scene, GlLayerType.Custom, layer));

                _layers.Add(_mainLayer);

                if (_options.ShadowMap.Use)
                {
                    var castShadowLayer = scene.Layers.Layers.OfType<CastShadowsLayer>().First();
                    _layers.Add(new GlLayer(this, scene, GlLayerType.CastShadow, castShadowLayer));
                }

                _lastScene = scene;
                _lastLayersVersion = scene.Layers.Version;
            }

            foreach (var layer in _layers)
            {
                if (layer.NeedUpdate)
                    layer.Update();
            }
        }

        public void Clear(Color color)
        {
            _glState.SetWriteColor(true);
            _glState.SetWriteDepth(true);
            _glState.SetClearDepth(1.0f);
            _glState.SetClearColor(color);

            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        public void Render(Scene3D scene, Camera camera, Rect2I view, bool flush)
        {
            if (_target != null)
                Render(scene, camera, view, _target);
        }

        public void Render(Scene3D scene, Camera camera, Rect2I view, IGlRenderTarget target)
        {
            _target = target;
            _view = view;

            UpdateLayers(scene);

            _updateCtx.Camera = camera.Clone();
            _updateCtx.Lights = _mainLayer!.Content.Lights;
            _updateCtx.LightsHash = _mainLayer.Content.LightsHash;
            _updateCtx.FrustumPlanes = camera.FrustumPlanes();

            foreach (var pass in _renderPasses)
            {
                _gl.PushDebugGroup(DebugSource.DebugSourceApplication, 0, unchecked((uint)-1), $"Begin Pass {pass.GetType().Name}");

                pass.Render();

                _gl.PopDebugGroup();
            }

            _dispatcher.ProcessQueue();
        }

        public void SetRenderTarget(Texture2D? texture)
        {
            if (texture == null)
                _target = _defaultTarget;
            else
            {
                var glTexture = texture.GetGlResource(tex => tex.CreateGlTexture(_gl, false));
                _texRenderTarget ??= new GlTextureRenderTarget(_gl);
                _texRenderTarget.FrameBuffer.Configure(glTexture, null);
                _target = _texRenderTarget;
            }
        }

        public void SetRenderTarget(IGlRenderTarget? target)
        {
            _target = target ?? _defaultTarget;
        }


        #endregion

        #region SURFACE PROVIDER    

        public void BeginDrawSurface()
        {
            var fence = _gl.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
            _gl.WaitSync(fence, SyncBehaviorFlags.None, unchecked((ulong)-1));
            _grContext!.ResetContext(GRGlBackendState.All);
        }

        public void EndDrawSurface()
        {
            ResetState();

 
            _glState.SetActiveProgram(0);
            _glState.EnableFeature(EnableCap.Blend, false);
            _glState.EnableFeature(EnableCap.ProgramPointSize, false);
            _glState.BindTexture(TextureTarget.Texture2D, 0);

            _gl.BindVertexArray(0);
       
            _gl.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
            _gl.BindSampler(0, 0);

            _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

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

        #endregion

        #region IBL

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

        #endregion

        #region IO

        public TextureData ReadFrame()
        {
            if (_target is not GlTextureRenderTarget texTarget)
                throw new NotSupportedException();

            if (texTarget.FrameBuffer is not GlTextureFrameBuffer texFb)
                throw new NotSupportedException();

            return texFb.ReadColor();
        }

        public Texture2D? GetShadowMap()
        {
            return _updateCtx?.ShadowMap;
        }
        public Texture2D? GetDepth()
        {
            var depthId = _target?.QueryTexture(FramebufferAttachment.DepthAttachment);

            if (depthId == null || depthId == 0)
                return null;

            var glTexture = GlTexture.Attach(_gl, depthId.Value);

            glTexture.Source ??= glTexture.ToEngineTexture();

            return (Texture2D)glTexture.Source;
        }

        #endregion

        #region MISC

        public unsafe string[] GetExtensions()
        {
            var data = _gl.GetString(StringName.Extensions);
            var allExt = Marshal.PtrToStringAuto(new nint(data))!;
            return allExt.Split(' ');
        }

        public GlTexture GetGlResource(Texture2D texture)
        {
            return texture.GetGlResource(a => texture.CreateGlTexture(_gl, _options.RequireTextureCompression));
        }


        public void Dispose()
        {
        }


        public void Suspend()
        {
        }

        public void Resume()
        {
        }

        #endregion

        public IReadOnlyList<GlLayer> Layers => _layers;    

        public GL GL => _gl;

        public GlState State => _glState;

        public UpdateShaderContext UpdateContext => _updateCtx;

        public IDispatcher Dispatcher => _dispatcher;

        public IGlRenderTarget? RenderTarget => _target;

        public Rect2I RenderView => _view;

        public GlRenderOptions Options => _options;

        public static int SuspendErrors { get; set; }

        [ThreadStatic]
        public static OpenGLRender? Current;
    }
}
