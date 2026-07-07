#if GLES
using Silk.NET.OpenGLES;
using GlStencilFunction = Silk.NET.OpenGLES.StencilFunction;
#else
using Silk.NET.OpenGL;
using GlStencilFunction = Silk.NET.OpenGL.StencilFunction;
#endif

using System.Text;
using XrMath;
using SkiaSharp;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Common.Interop;

namespace XrEngine.OpenGL
{


    public class OpenGLRender : IRenderEngine, ISurfaceProvider, IIBLPanoramaProcessor, IFrameReader
    {
        protected class LayersCache
        {
            public long Version = -1;

            public List<IGlLayer> Layers = [];
        }

        protected Scene3D? _lastScene;
        protected long _lastLightLayerVersion;
        protected IGlRenderTarget? _target;
        protected Rect2I _view;

        protected GRContext? _grContext;
        protected GlTextureRenderTarget? _texRenderTarget = null;


        protected readonly GlUpdateContext _updateCtx;
        protected readonly int _maxTextureUnits;
        protected readonly GL _gl;
        protected readonly GlState _glState;
        protected readonly GlRenderOptions _options;
        protected readonly QueueDispatcher _dispatcher;

        protected readonly IList<IGlRenderPass> _renderPasses = [];
        protected readonly GlDefaultRenderTarget _defaultTarget;
        protected readonly GlShadowPass? _shadowPass;
        protected readonly Thread _thread;
        protected readonly Dictionary<Scene3D, LayersCache> _layersCache = [];
        protected List<IGlLayer> _activeLayers = [];
        protected GlTextureFilter _textureFilter;

        private bool _isDebug;

        public static class Props
        {
            public static readonly DynamicProp GlResId = new(nameof(GlResId));

            public static readonly DynamicProp GlQuery = new(nameof(GlQuery));

            public static readonly DynamicProp BufferMap = new(nameof(BufferMap));

            public static readonly DynamicProp[] RenderTarget = [new("RenderTargetEye0"), new("RenderTargetEye1")];

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

            _thread = Thread.CurrentThread;

            _glState = state;
            _gl = gl;
            _options = options;

            _defaultTarget = new GlDefaultRenderTarget(gl, !options.UseDepthPass && !options.ContactShadow.Use, options.SampleCount);
            _target = _defaultTarget;

            _updateCtx = new GlUpdateContext
            {
                RenderEngine = this,
            };

            _dispatcher = new QueueDispatcher();

            if (_options.ShadowMap.Mode != ShadowMapMode.None)
            {
                _shadowPass = new GlShadowPass(this);
                _renderPasses.Add(_shadowPass);
                _updateCtx.ShadowMapProvider = _shadowPass;
            }

            if (_options.UseDepthPass)
            {
                var depthPass = new GlDepthPass(this)
                {
                    UseOcclusionQuery = _options.UseOcclusionQuery
                };
                _renderPasses.Add(depthPass);
                _updateCtx.DepthCullProvider = depthPass;
            }

            if (_options.UsePlanarReflection)
                _renderPasses.Add(new GlReflectionPass(this));

            _renderPasses.Add(new GlColorPass(this));

            if (_options.ContactShadow.Use)
            {
                var contact = new GlContactShadowPass(this, -1, _options.ContactShadow.IsMultiView);
                _renderPasses.Add(contact);
            }

            if (_options.Outline.Use)
            {
                var outline = new GlOutlinePass(this, -1, _options.Outline.IsMultiView);
                _renderPasses.Add(outline);
            }

            if (_options.UseHitTest)
            {
                var hitTest = new GlHitTestPass(this);
                _renderPasses.Add(hitTest);
                Context.Implement<IViewHitTest>(hitTest);
            }

            if (_options.UseResolve)
                _renderPasses.Add(new GlResolvePass(this));

            _gl.GetInteger(GetPName.MaxTextureImageUnits, out _maxTextureUnits);

            var exts = GetExtensions();
            foreach (var ex in exts)
                Debug.WriteLine(ex);

            _textureFilter = new GlTextureFilter(this);

            ConfigureCaps();

            PbrMaterial.SHADER.ToneMap = _options.ToneMap;
        }

        #endregion

        #region STATE

        protected internal void ResetState()
        {
            _glState.Reset();
            //_gl.DrawBuffers(GlState.DRAW_COLOR_0);
        }

        public unsafe void EnableDebug(bool sync = false)
        {
            _gl.DebugMessageCallback((source, type, id, sev, len, msg, param) =>
           {
               if (SuspendErrors > 0)
                   return;

               try
               {
                   var span = new Span<byte>((void*)msg, len);
                   var text = Encoding.UTF8.GetString(span);

                   if (sev == GLEnum.DebugSeverityNotification)
                       return;

                   Debug.WriteLine($"\n\n\n");
                   Debug.WriteLine($"------ OPENGL: {text}");
                   Debug.WriteLine($"\n\n\n");

               }
               catch
               {

               }
           }, null);

            _gl.Enable(EnableCap.DebugOutput);
            if (sync)
                _gl.Enable(EnableCap.DebugOutputSynchronous);

            _isDebug = true;
        }

        protected void ConfigureCaps()
        {
            _gl.FrontFace(FrontFaceDirection.Ccw);
            
            _glState.SetCullFace(TriangleFace.Back);

            _glState.EnableFeature(EnableCap.FramebufferSrgb, _options.UseSRGB);
            _glState.EnableFeature(EnableCap.Dither, true);
            _glState.EnableFeature(EnableCap.Multisample, true);
            _glState.EnableFeature(EnableCap.ScissorTest, false);
            _glState.EnableFeature(EnableCap.ProgramPointSize, true);
            _glState.EnableFeature(EnableCap.TextureCubeMapSeamless, true);

            _gl.Disable(EnableCap.SampleAlphaToCoverage);
            _gl.Disable(EnableCap.SampleAlphaToOne);
            _gl.Disable(EnableCap.SampleCoverage);
        }

        public void ConfigureCaps(ShaderMaterial material)
        {
            _glState.SetCullFace(material.CullFront ? TriangleFace.Front : TriangleFace.Back);
            _glState.SetUseDepth(material.UseDepth);
            _glState.SetWriteDepth(material.WriteDepth);
            _glState.SetDoubleSided(material.DoubleSided);
            _glState.SetWriteColor(material.WriteColor);
            _glState.SetAlphaMode(material.Alpha);
            _glState.SetWireframe(material is WireframeMaterial);

            _glState.SetStencilFunc((GlStencilFunction)material.StencilFunction);
            _glState.SetWriteStencil(material.WriteStencil);
            _glState.SetStencilRef(material.CompareStencilMask);

            _glState.EnableFeature(EnableCap.ClipDistance0, material.UseClipDistance);

            if (material.PolygonOffset.Y != 0)
            {
                _glState.EnableFeature(EnableCap.PolygonOffsetFill, true);
                _gl.PolygonOffset(material.PolygonOffset.X, material.PolygonOffset.Y);
            }
            else
                _glState.EnableFeature(EnableCap.PolygonOffsetFill, false);

            if (material is ILineMaterial line)
                _glState.SetLineWidth(line.LineWidth);

            _glState.Commit();
        }

        #endregion

        #region RENDER

        public IEnumerable<T> Passes<T>() where T : IGlRenderPass
        {
            return _renderPasses.OfType<T>();
        }

        public void AddPass(IGlRenderPass pass, int position)
        {
            if (position == -1)
                _renderPasses.Add(pass);
            else
                _renderPasses.Insert(position, pass);

        }

        protected void UpdateLights(Scene3D scene)
        {
            var lights = scene.EnsureLayer<LightLayer>();

            if (_lastLightLayerVersion == lights.Version)
                return;

            _updateCtx.Lights = [];
            _updateCtx.LightsHash = "";

            foreach (var light in scene.Descendants<Light>().Visible())
            {
                _updateCtx.Lights.Add(light);

                if (light is ImageLight imgLight)
                {
                    if (imgLight.Panorama?.Data != null && imgLight.Panorama.Version != _updateCtx.ImageLightVersion)
                    {
                        var options = PanoramaProcessorOptions.Default();

                        options.SampleCount = 1024;
                        options.Resolution = 256;
                        options.Mode = IblProcessMode.GGX | IblProcessMode.Lambertian;

                        imgLight.Textures = ProcessPanoramaIBL(imgLight.Panorama.Data[0], options);
                        imgLight.Panorama.NotifyLoaded();
                        imgLight.NotifyIBLCreated();

                        _updateCtx.ImageLightVersion = imgLight.Panorama.Version;

                        ResetState();
                    }
                }

                _updateCtx.LightsHash += light.GetType().Name + "|";
            }

            _lastLightLayerVersion = lights.Version;
        }

        public IGlLayer AddLayer(Scene3D scene, GlLayerType type, ILayer3D? sceneLayer = null)
        {
            var layer = new GlLayer(this, scene, type, sceneLayer);
            _activeLayers.Add(layer);
            return layer;
        }

        protected void UpdateLayers(Scene3D scene)
        {
            if (!_layersCache.TryGetValue(scene, out var cache))
            {
                cache = new LayersCache();
                _layersCache[scene] = cache;
            }

            _activeLayers = cache.Layers;

            if (cache.Version != scene.Layers.Version)
            {
                foreach (var layer in _activeLayers)
                    layer.Dispose();

                _activeLayers.Clear();

                var opaque = scene.EnsureLayer<OpaqueLayer>();
                AddLayer(scene, GlLayerType.Opaque, opaque);

                /*
                var staticLayer = scene.EnsureLayer<StaticLayer>();
                AddLayer(scene, GlLayerType.Static, staticLayer);
                */

                foreach (var layer in scene.Layers.Layers.OfType<DetachedLayer>())
                    AddLayer(scene, GlLayerType.Custom, layer);

                var blend = scene.EnsureLayer<BlendLayer>();
                AddLayer(scene, GlLayerType.Blend, blend);

                if (_options.ShadowMap.Mode != ShadowMapMode.None)
                {
                    var castShadowLayer = scene.EnsureLayer<CastShadowsLayer>();
                    scene.EnsureLayer<ReceiveShadowsLayer>();
                    AddLayer(scene, GlLayerType.CastShadow, castShadowLayer);
                }

                if (_options.UsePlanarReflection)
                {
                    scene.EnsureLayer<HasReflectionLayer>();
                    AddLayer(scene, GlLayerType.FullReflection, opaque);
                }

                if (_options.UseVolume)
                {
                    var volume = scene.EnsureLayer<VolumeLayer>();
                    AddLayer(scene, GlLayerType.Volume, volume);
                }


                _lastScene = scene;
                cache.Version = scene.Layers.Version;
            }

            /*
            foreach (var layer in _activeLayers)
            {
                if (layer.NeedUpdate)
                    layer.Rebuild();
            }
            */
        }

        protected void EnsureThread()
        {
            if (_thread != Thread.CurrentThread)
                throw new InvalidOperationException("Invalid GL Thread");
        }

        public void Clear(Color color)
        {
            _glState.SetWriteColor(true);
            _glState.SetWriteDepth(true);
            _glState.SetClearDepth(1.0f);
            _glState.SetClearColor(color);
            _glState.SetClearStencil(0);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        }

        public void Render(RenderContext ctx, Rect2I view, bool flush)
        {
            if (_target != null)
                Render(ctx, view, _target, flush);
        }

        [Conditional("DEBUG")]
        public void PushGroup(string message)
        {
            _gl.PushDebugGroup(DebugSource.DebugSourceApplication, 0, (uint)message.Length, message);
        }

        [Conditional("DEBUG")]
        public void PopGroup()
        {
            _gl.PopDebugGroup();
        }

        public void Render(RenderContext ctx, Rect2I view, IGlRenderTarget target, bool flush)
        {
            EnsureThread();

            Debug.Assert(ctx.Scene != null && ctx.Camera != null);

            _target = target;
            _view = view;

            PushGroup($"Render {(target == null ? "Default" : target.GetType().Name)}");

            UpdateLayers(ctx.Scene);

            UpdateLights(ctx.Scene);

            _updateCtx.PassCamera = ctx.Camera;
            _updateCtx.MainCamera = ctx.Camera;
            _updateCtx.Frame = ctx.Frame;
            _updateCtx.Time = (float)ctx.Time;
            _updateCtx.ContextVersion++;

            if (!GlState.Current!.Features.TryGetValue(EnableCap.FramebufferSrgb, out _updateCtx.IsSrgb))
                _updateCtx.IsSrgb = false;
            /*
            if (_updateCtx.IsSrgb && RenderTarget is IGlFrameBufferProvider prov)
            {
                var color = prov.FrameBuffer.Color;
                if (color != null && !color.InternalFormat.IsSrgb())
                    _updateCtx.IsSrgb = false;
            }
            */

            foreach (var pass in _renderPasses)
                pass.Configure(ctx);

            foreach (var pass in _renderPasses)
            {
                _updateCtx.Pass = pass;

                PushGroup($"Pass {pass.GetType().Name}");

                pass.Render(ctx);

                PopGroup();
            }

            _dispatcher.ProcessQueue();

            _target.End(_options.InvalidateDepth);

            if (flush)
                _gl.Flush();

            PopGroup();

            //new GlBenchmark(_gl).Bench();   
        }

        public void SetRenderTarget(Texture2D? texture)
        {
            EnsureThread();

            if (texture == null)
                _target = _defaultTarget;
            else
            {
                var glTexture = texture.ToGlTexture();
                _texRenderTarget ??= new GlTextureRenderTarget(_gl);
                _texRenderTarget.FrameBuffer.Configure(glTexture, null, glTexture.SampleCount);
                _target = _texRenderTarget;
            }
        }

        public void SetRenderTarget(IGlRenderTarget? target)
        {
            _target = target ?? _defaultTarget;
        }

        public void SetRenderTargetDefault()
        {
            _target = _defaultTarget;
        }


        #endregion

        #region ISurfaceProvider

        public void BeginDrawSurface(SKSurface surface, Texture2D texture)
        {
            EnsureThread();

            PushGroup("Draw surface");

            var fence = _gl.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
            _gl.WaitSync(fence, SyncBehaviorFlags.None, unchecked((ulong)-1));
            _grContext!.ResetContext(GRGlBackendState.All);
        }

        public void EndDrawSurface(SKSurface surface, Texture2D texture)
        {
            EnsureThread();

            ResetState();

            _glState.SetActiveProgram(0, true);
            _glState.EnableFeature(EnableCap.Blend, false, true);
            _glState.EnableFeature(EnableCap.ProgramPointSize, false, true);
            _glState.BindTexture(TextureTarget.Texture2D, 0, true);

            _glState.BindVertexArray(0);

            _gl.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
            _gl.BindSampler(0, 0);

            _glState.BindFrameBuffer(FramebufferTarget.ReadFramebuffer, 0, true);
            _glState.BindFrameBuffer(FramebufferTarget.DrawFramebuffer, 0, true);
            _glState.BindFrameBuffer(FramebufferTarget.Framebuffer, 0, true);

            _glState.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
            _glState.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

            ConfigureCaps();

            if (texture.MipLevelCount > 1)
                texture.ToGlTexture().GenerateMipmap();

            PopGroup();
        }

        public SKSurface CreateSurface(Texture2D texture)
        {
            EnsureThread();

            var glTexture = texture.ToGlTexture();

            if (glTexture.Version != texture.Version)
                glTexture.Update(texture);

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


            var grTexture = new GRBackendTexture((int)glTexture.Width, (int)glTexture.Height, glTexture.MaxLevel > 0, gerTextInfo);

            var props = new SKSurfaceProperties(SKPixelGeometry.RgbVertical);

            var surface = SKSurface.Create(_grContext, grTexture, ImageUtils.GetSkFormat(texture.Format), props);
            
            _glState.Reset();

            return surface ?? throw new Exception("Surface creation failed");
        }

        #endregion

        #region IIBLPanoramaProcessor

        public IBLTextures ProcessPanoramaIBL(TextureData data, PanoramaProcessorOptions options)
        {
            EnsureThread();

            Log.Info(this, "Processing IBL Panorama");

            using var processor = new GlIblProcessor(_gl);

            processor.Resolution = options.Resolution;
            processor.MipLevelCount = options.MipLevelCount;
            processor.SampleCount = options.SampleCount;

            processor.Initialize(data, options.ShaderResolver!);

            processor.PanoramaToCubeMap();

            var result = new IBLTextures
            {
                MipCount = processor.MipLevelCount
            };

            result.Env = (TextureCube)_gl.TexIdToEngineTexture(processor.OutCubeMapId);

            if ((options.Mode & IblProcessMode.Lambertian) == IblProcessMode.Lambertian)
            {
                var texId = processor.ApplyFilter(GlIblProcessor.Distribution.Irradiance);

                result.LambertianEnv = (TextureCube)_gl.TexIdToEngineTexture(texId);
            }

            if ((options.Mode & IblProcessMode.GGX) == IblProcessMode.GGX)
            {
                var ggx = processor.ApplyFilter(GlIblProcessor.Distribution.GGX);
                var ggxLut = processor.ApplyFilter(GlIblProcessor.Distribution.GGXLut);

                result.GGXEnv = (TextureCube)_gl.TexIdToEngineTexture(ggx);
                result.GGXLUT = (Texture2D)_gl.TexIdToEngineTexture(ggxLut);
            }

            Log.Debug(this, "Processing IBL Panorama OK");

            return result;
        }


        #endregion

        #region TEXTURE

        public Texture2D AttachTexture(uint texId)
        {
            var glTex = GlTexture.Attach(_gl, texId);
            return glTex.ToEngineTexture(new Texture2D());
        }

        public void CopyTexture(Texture2D src, Texture2D dst)
        {
            src.ToGlTexture().CopyTo(dst.ToGlTexture());
        }

        public IList<TextureData>? ReadTexture(Texture texture, TextureFormat format, uint startMipLevel = 0, uint? endMipLevel = null, IList<IMemoryBuffer<byte>>? buffers = null)
        {
            EnsureThread();

            var glTex = texture.ToGlTexture();

            PushGroup($"ReadTexture {glTex.Handle}");

            var data = glTex.Read(format, startMipLevel, endMipLevel, buffers);

            PopGroup();

            return data;
        }

        public void LoadTexture(Texture2D texture)
        {
            texture.ToGlTexture();
        }

        #endregion

        #region IO

        public TextureData ReadFrame(TextureFormat format = TextureFormat.Rgba32)
        {
            EnsureThread();

            if (_target is not GlTextureRenderTarget texTarget)
                throw new NotSupportedException();

            if (texTarget.FrameBuffer is not GlTextureFrameBuffer texFb)
                throw new NotSupportedException();

            return texFb.ReadColor(format);
        }

        public Texture2D? GetShadowMap()
        {
            return _shadowPass?.DepthTexture;
        }

        public Texture2D? GetDepth()
        {
            var glDepth = _target?.QueryTexture(FramebufferAttachment.DepthAttachment);

            if (glDepth == null)
                return null;

            //TODO not always true need nearest

            if (glDepth.MinFilter != TextureMinFilter.Nearest || glDepth.MagFilter != TextureMagFilter.Nearest)
            {
                glDepth.MinFilter = TextureMinFilter.Nearest;
                glDepth.MagFilter = TextureMagFilter.Nearest;

                glDepth.Bind();
                _gl.TexParameter(glDepth.Target, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
                _gl.TexParameter(glDepth.Target, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
                glDepth.Unbind();
            }

            return (Texture2D)glDepth.ToEngineTexture();
        }

        #endregion

        #region MISC

        public T? Feature<T>() where T : class
        {
            if (this is T result)
                return result;

            if (typeof(T) == typeof(IShadowMapProvider))
                return _shadowPass as T;

            if (typeof(T) == typeof(ITextureFilterProvider))
                return _textureFilter as T;

            return (T?)_renderPasses.FirstOrDefault(a => a is T);
        }


        public unsafe string[] GetExtensions()
        {
            var data = _gl.GetString(StringName.Extensions);
            var allExt = Marshal.PtrToStringAuto(new nint(data))!;
            if (string.IsNullOrWhiteSpace(allExt))
                return [];
            return allExt.Split(' ');
        }

        public void Dispose()
        {
            _textureFilter.Dispose();

            foreach (var pass in _renderPasses)
                pass.Dispose();
            _renderPasses.Clear();

            foreach (var layer in _activeLayers)
                layer.Dispose();

            foreach (var program in GlProgramInstance._programs)
                program.Value.Dispose();
            GlProgramInstance._programs.Clear();

            foreach (var texture in GlTexture._attached)
                texture.Value.Dispose();
            GlTexture._attached.Clear();

            GlProgramInstance._programs.Clear();

            GC.SuppressFinalize(this);
        }

        public void Suspend()
        {
        }

        public void Resume()
        {
        }


        #endregion

        public IReadOnlyList<IGlLayer> Layers => _activeLayers;

        public GL GL => _gl;

        public GlState State => _glState;

        public GlUpdateContext UpdateContext => _updateCtx;

        public IDispatcher Dispatcher => _dispatcher;

        public IGlRenderTarget? RenderTarget => _target;

        public GlRenderOptions Options => _options;

        public bool IsDebug => _isDebug;

        public static int SuspendErrors { get; set; }


        [ThreadStatic]
        public static OpenGLRender? Current;

    }
}
