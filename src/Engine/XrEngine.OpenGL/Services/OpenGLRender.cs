#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Text;
using XrMath;
using SkiaSharp;
using System.Diagnostics;
using Common.Interop;
using XrEngine.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;


namespace XrEngine.OpenGL
{

    public class OpenGLRender : IRenderEngine, ISurfaceProvider, IIBLPanoramaProcessor, IFrameReader
    {
        protected class LayersCache
        {
            public long Version = -1;

            public List<IGlLayer> Layers = [];
        }

        [ThreadStatic]
        internal static  OpenGLRender? _current;

        protected Scene3D? _lastScene;
        protected long _lastLightLayerVersion;
        protected IGlRenderTarget? _target;
        protected Rect2I _view;
        protected GRContext? _grContext;
        protected GlTextureRenderTarget? _texRenderTarget = null;
        protected readonly GlUpdateContext _updateCtx;
        protected readonly GL _gl;
        protected readonly GlState _glState;
        protected readonly GlRenderOptions _options;
        protected readonly QueueDispatcher _dispatcher;
        protected readonly List<IGlRenderPass> _renderPasses = [];
        protected readonly IGlRenderTarget _defaultTarget;
        protected GlShadowPass? _shadowPass;
        protected Thread _thread;
        protected readonly Dictionary<Scene3D, LayersCache> _layersCache = [];
        protected List<IGlLayer> _activeLayers = [];
        [MaybeNull]
        protected readonly GlTextureFilter _textureFilter;
        protected readonly HashSet<string> _extensions;
        protected bool _isDebug;
        protected GlProfiler _profiler;
        protected DateTime _lastProfileOutTime;
        protected RenderEngineFeatures _features;

        private bool _passesDirty;

        public static class Props
        {
            public static readonly DynamicProp GlResId = new(nameof(GlResId));

            public static readonly DynamicProp GlQuery = new(nameof(GlQuery));

            public static readonly DynamicProp BufferMap = new(nameof(BufferMap));

            public static readonly DynamicProp BufferRangeSlot = new(nameof(BufferRangeSlot));

            public static readonly DynamicProp[] RenderTarget = [new("RenderTargetEye0"), new("RenderTargetEye1")];

        }

        #region CONSTRUCTORS

        public OpenGLRender(GL gl)
            : this(gl, new GlRenderOptions())
        {
        }

        public OpenGLRender(GL gl, GlRenderOptions options, bool isDummy = false)
            : this(gl, options, new GlState(gl), isDummy)
        {
        }

        protected OpenGLRender(GL gl, GlRenderOptions options, GlState state, bool isDummy)
        {
            _current = this;

            _thread = Thread.CurrentThread;

            _glState = state;
            _gl = gl;
            _options = options;

            if (options.UseDefaultIntermediate)
            {
                _defaultTarget = new GlDefaultRenderTarget(gl,
                    !options.UseDepthPass && !options.ContactShadow.Use,
                    options.SampleCount);
            }
            else
                _defaultTarget = new GlDefaultDirectRenderTarget(gl);

            _target = _defaultTarget;

            _dispatcher = new QueueDispatcher();

            _profiler = new GlProfiler(_gl);

            _extensions = GetExtensions();

            _updateCtx = new GlUpdateContext
            {
                RenderEngine = this,
            };

            ConfigureDriver();



            if (isDummy)
                return;

            ConfigurePasses();

#if GLES

            foreach (var ex in _extensions)
                Debug.WriteLine(ex);
#endif

            _textureFilter = new GlTextureFilter(this);

            ConfigureCaps();



            PbrMaterial.SHADER.ToneMap = _options.ToneMap;
        }

        #endregion

        protected unsafe void ConfigureDriver()
        {
            var version = Marshal.PtrToStringAnsi((nint)_gl.GetString(StringName.Version)) ?? "";

            _features.ClipCullDistance = _extensions.Contains("GL_EXT_clip_cull_distance");

            _features.PrimitiveBoundingBox = _extensions.Contains("GL_EXT_primitive_bounding_box");

            _features.GeometryShader = _extensions.Contains("GL_EXT_geometry_shader") ||
                                       _extensions.Contains("GL_OES_geometry_shader");

            _features.TessellationShader = _extensions.Contains("GL_EXT_tessellation_shader") ||
                                           _extensions.Contains("GL_OES_tessellation_shader");

            _features.ShaderFramebufferFetch = _extensions.Contains("GL_EXT_shader_framebuffer_fetch");

            _features.Multiview2 = _extensions.Contains("GL_OVR_multiview2");

            _features.ImageExternalEssl3 = _extensions.Contains("GL_OES_EGL_image_external_essl3");

            _features.ShaderFramebufferFetchRate = _extensions.Contains("GL_QCOM_shader_framebuffer_fetch_rate");

            _features.BufferStorage = _extensions.Contains("GL_EXT_buffer_storage");

            _features.ClearTexture = _extensions.Contains("GL_EXT_clear_texture");

            _features.ClipControl = _extensions.Contains("GL_EXT_clip_control");

            _features.DisjointTimerQuery = _extensions.Contains("GL_EXT_disjoint_timer_query");

            _features.MultisampledRenderToTexture = _extensions.Contains("GL_EXT_multisampled_render_to_texture");

            _gl.GetInteger(GetPName.MaxVertexShaderStorageBlocks, out _features.MaxVertexSsboBlocks);

            _gl.GetInteger(GetPName.MaxFragmentShaderStorageBlocks, out _features.MaxFragmentSsboBlocks);

            _gl.GetInteger(GetPName.MaxTextureImageUnits, out _features.MaxTextureUnits);

            _features.GpuName = Marshal.PtrToStringAnsi((nint)_gl.GetString(StringName.Renderer)) ?? "";

            _features.IsNvidia = _features.GpuName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase);

            _features.IsAngle = _features.GpuName.Contains("ANGLE", StringComparison.OrdinalIgnoreCase);

            _features.IsWindows = OperatingSystem.IsWindows();

            _features.IsAndroid = OperatingSystem.IsAndroid();

            _features.IsGlEs = version.StartsWith("OpenGL ES", StringComparison.OrdinalIgnoreCase);

            //
            if (_features.IsWindows && !_features.IsAngle)
                _features.PrimitiveBoundingBox = false;

            _updateCtx.Bugs.NvMultiViewClipBug = _features.IsAngle &&
                                                 _features.IsWindows &&
                                                 _features.IsNvidia;

            _updateCtx.UseAngle = _features.IsAngle;

            if (_features.MaxVertexSsboBlocks == 0)
            {
                _options.UseInstanceDraw = false;
                _updateCtx.UseSharedSsbo = false;
            }

        }

        public void MakeCurrent()
        {
            _current = this;
        }

        #region STATE


        protected internal void ResetState()
        {
            _glState.Reset();
        }

        public unsafe void EnableDebug(RenderEngineDebug mode)
        {
            _gl.DebugMessageCallback((source, type, id, sev, len, msg, param) =>
           {
               if (SuspendErrors > 0)
                   return;
               try
               {
                   var span = new Span<byte>((void*)msg, len);
                   var text = Encoding.UTF8.GetString(span);

                   Debug.WriteLine($"\n\n\n");
                   Debug.WriteLine($"------ OPENGL[{id}]: {text}");
                   Debug.WriteLine($"\n\n\n");
               }
               catch
               {

               }
           }, null);

            _gl.Enable(EnableCap.DebugOutput);

            if (mode == RenderEngineDebug.Sync)
                _gl.Enable(EnableCap.DebugOutputSynchronous);

            _gl.DebugMessageControl(DebugSource.DontCare, DebugType.DontCare, DebugSeverity.DebugSeverityNotification, 0, null, false);

            _gl.DebugMessageControl(DebugSource.DebugSourceApi, DebugType.DebugTypePerformance, DebugSeverity.DontCare, 2u, [131186, 131202], false);
            _gl.DebugMessageControl(DebugSource.DebugSourceOther, DebugType.DebugTypePerformance, DebugSeverity.DontCare, 1u, [2147483647], false);
            _gl.DebugMessageControl(DebugSource.DebugSourceApi, DebugType.DebugTypeError, DebugSeverity.DontCare, 2u, [1281, 2147483647], false);

            //glDisable: Enum 0x3000 is currently not supported.
            _gl.DebugMessageControl(DebugSource.DebugSourceApi, DebugType.DebugTypeError, DebugSeverity.DontCare, 1u, [1280], false);

            //Error:glObjectLabel::<name> is not an accepted value
            //Error:glDisable::<cap> is not one of the accepted values
            _gl.DebugMessageControl(DebugSource.DebugSourceApi, DebugType.DebugTypeError, DebugSeverity.DontCare, 2u, [57, 55], false);
            //Error:glEnable::<cap> is not one of the accepted values
            _gl.DebugMessageControl(DebugSource.DebugSourceApi, DebugType.DebugTypePerformance, DebugSeverity.DontCare, 1u, [55], false);
            //"Performance:glTexSubImage2D::Submission has been flushed"
            _gl.DebugMessageControl(DebugSource.DebugSourceApi, DebugType.DebugTypePerformance, DebugSeverity.DontCare, 1u, [4], false);

            _isDebug = true;

#if !GLES
            _glState.EnableDebug = true;
#endif
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

        #endregion

        #region RENDER

        protected virtual void ConfigurePasses()
        {
            if (_options.ShadowMap.Mode != ShadowMapMode.None)
            {
                _shadowPass = new GlShadowPass(this);
                _renderPasses.Add(_shadowPass);
                _updateCtx.ShadowMapProvider = _shadowPass;
            }

            if (_options.UsePlanarReflection)
                _renderPasses.Add(new GlReflectionPassGroup(this));

            if (_options.Outline.Use)
            {
                var outline = new GlOutlinePass(this, -1, _options.Outline.IsMultiView);
                _renderPasses.Add(outline);
            }

            if (_options.UseRayCollider)
                _renderPasses.Add(new GlRayColliderPassGroup(this));

            if (_options.UseHitTest)
            {
                var hitTest = new GlHitTestPass(this);
                _renderPasses.Add(hitTest);
                Context.Implement<IViewHitTest>(hitTest);
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

            _renderPasses.Add(new GlColorPass(this));

            if (_options.ContactShadow.Use)
            {
                var contact = new GlContactShadowPass(this, -1, _options.ContactShadow.IsMultiView);
                _renderPasses.Add(contact);
            }

            _renderPasses.Add(new GlCompositonPass(this));

            if (_options.UseResolve)
                _renderPasses.Add(new GlResolvePass(this));

            _passesDirty = true;
        }

        public IEnumerable<T> Passes<T>() where T : IGlRenderPass
        {
            return _renderPasses.OfType<T>();
        }

        public T EnsurePass<T>(Func<T> factory) where T : IGlRenderPass
        {
            var res = Passes<T>().FirstOrDefault();
            if (res == null)
            {
                res = factory();
                AddPass(res, -1);
            }
            return res;
        }

        public void AddPass(IGlRenderPass pass, int position)
        {
            if (position == -1)
            {
                _renderPasses.Add(pass);
            }
            else
                _renderPasses.Insert(position, pass);

            _passesDirty = true;
        }

        protected void UpdateLights(Scene3D scene)
        {
            var lights = scene.EnsureLayer<LightLayer>();

            if (_lastLightLayerVersion == lights.Version)
                return;

            _updateCtx.Lights = [];

            var builder = HashBuilder.Instance;

            builder.Reset();

            foreach (var light in lights.Content.Visible())
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

                        //ResetState();
                    }
                }

                light.EnsureId();

                builder.Add(light.Id);
            }

            _updateCtx.LightsHash = builder.Value();

            _lastLightLayerVersion = lights.Version;
        }

        public IGlLayer AddLayer(Scene3D scene, GlLayerType type, ILayer3D? sceneLayer = null)
        {
            var layer = new GlLayer(this, scene, type, sceneLayer);
            _activeLayers.Add(layer);
            return layer;
        }

        protected void UpdatePasses()
        {
            var sorted = _renderPasses
                .Where(x => x.Priority < 0).OrderBy(x => x.Priority)
                .Concat(_renderPasses.Where(x => x.Priority == 0))
                .Concat(_renderPasses.Where(x => x.Priority > 0).OrderBy(x => x.Priority))
                .ToList();

            _renderPasses.Clear();
            _renderPasses.AddRange(sorted);

            _passesDirty = false;
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

                if (_options.UseRayCollider)
                {
                    var collider = scene.EnsureLayer<MeshColliderLayer>();
                    AddLayer(scene, GlLayerType.MeshCollider, collider);
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
            _glState.Commit();

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
            _profiler.IsEnabled = _options.UseProfiler;

            PushGroup($"Render {(target == null ? "Default" : target.GetType().Name)}");

            using var frameProf = _profiler.Profile("Frame", _updateCtx.Frame, true);

            UpdateLayers(ctx.Scene);

            UpdateLights(ctx.Scene);

            _updateCtx.MainCamera = ctx.Camera;
            _updateCtx.PassCamera = ctx.Camera;
            _updateCtx.Frame = ctx.Frame;
            _updateCtx.Time = (float)ctx.Time;
            _updateCtx.Scene = ctx.Scene;
            _updateCtx.DeltaTime = (float)ctx.DeltaTime;

            _updateCtx.ContextVersion++;

            if (_passesDirty)
                UpdatePasses();

            foreach (var pass in _renderPasses)
                pass.Configure(_updateCtx);

            foreach (var pass in _renderPasses)
            {
                _updateCtx.Pass = pass;

                if ((pass.Flags & GlRenderPassFlags.CustomCamera) != 0)
                    _updateCtx.PassCamera = _updateCtx.MainCamera.Clone();
                else
                    _updateCtx.PassCamera = _updateCtx.MainCamera;

                PushGroup($"Pass {pass.GetType().Name}");

                using var passProf = _profiler.Profile(pass.GetType().Name, _updateCtx.Frame);

                pass.Render(_updateCtx);

                PopGroup();
            }

            _dispatcher.ProcessQueue();

            using var endFrameProf = _profiler.Profile("EndFrame", _updateCtx.Frame);

            _target.End(_options.InvalidateDepth);

            if (flush)
                _gl.Flush();

            PopGroup();

#if GLES
            if (EngineApp.Current.Stats.Frame > 6)
                _glState.EnableDebug = _isDebug;
#endif

            _profiler.Collect();

            if ((DateTime.Now - _lastProfileOutTime).TotalSeconds > 10)
            {
                Log.Debug(this, _profiler.GetStatsLog());
                _lastProfileOutTime = DateTime.Now;
            }
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
                     _gl.Context.TryGetProcAddress(name, out var result);
                     return result;
                });

#endif

                if (grInterface == null)
                    throw new InvalidOperationException();

                _grContext = GRContext.CreateGl(grInterface);

                if (_grContext == null)
                    throw new InvalidOperationException();
            }

            var format = glTexture.InternalFormat;

            if (format == InternalFormat.Rgba || format == 0)
                format = InternalFormat.Rgba8;

            var gerTextInfo = new GRGlTextureInfo((uint)glTexture.Target, glTexture.Handle, (uint)format);

            var grTexture = new GRBackendTexture((int)glTexture.Width, (int)glTexture.Height, glTexture.MaxLevel > 0, gerTextInfo);

            var props = new SKSurfaceProperties(SKPixelGeometry.RgbVertical);

            var surface = SKSurface.Create(_grContext, grTexture, ImageUtils.GetSkFormat(texture.Format), props);

            ResetState();

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

        public TextureData ReadFrame(TextureFormat format = TextureFormat.Rgba8)
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

            if (typeof(T) == typeof(GL))
                return _gl as T;

            if (typeof(T) == typeof(IShadowMapProvider))
                return _shadowPass as T;

            if (typeof(T) == typeof(ITextureFilterProvider))
                return _textureFilter as T;

            if (typeof(T) == typeof(IGpuProfiler))
                return _profiler as T;

            return (T?)_renderPasses.FirstOrDefault(a => a is T);
        }

        public HashSet<string> GetExtensions()
        {
            _gl.GetInteger(GetPName.NumExtensions, out var count);

            var result = new HashSet<string>(count);

            for (uint i = 0; i < count; i++)
                result.Add(_gl.GetStringS(StringName.Extensions, i));

            return result;
        }

        public void Dispose()
        {
            _textureFilter?.Dispose();

            foreach (var pass in _renderPasses)
                pass.Dispose();
            _renderPasses.Clear();

            foreach (var layer in _activeLayers)
                layer.Dispose();

            foreach (var program in GlProgramInstance._programsByFeatures)
                program.Value.Dispose();
            GlProgramInstance._programsByFeatures.Clear();

            foreach (var texture in GlTexture._attached)
                texture.Value.Dispose();
            GlTexture._attached.Clear();

            GlProgramInstance._programsByFeatures.Clear();

            GC.SuppressFinalize(this);
        }

        public void Suspend()
        {
        }

        public void Resume()
        {
        }

        internal void Begin(IGlRenderTarget renderTarget)
        {
            if ((renderTarget.Flags & GlRenderTargetFlags.ForceSrgbEncode) != 0)
            {
                _updateCtx.IsSrgbTarget = true;
                _updateCtx.IsSrgbAutoEncode = false;
            }
            else
            {
                if (renderTarget is IGlFrameBufferProvider fbProv && fbProv.FrameBuffer.Color != null)
                    _updateCtx.IsSrgbTarget = fbProv.FrameBuffer.Color.InternalFormat.IsSrgb();
                else
                    _updateCtx.IsSrgbTarget = false;

                _updateCtx.IsSrgbAutoEncode = _glState.IsFeatureEnabled(EnableCap.FramebufferSrgb);
            }

            _updateCtx.ClipRegions = renderTarget.ClipRegions;
            _updateCtx.IsMultiView = renderTarget is GlMultiViewRenderTarget;

            _glState.SetShadingRate(Math.Max(1, _target!.ShadingRate));
        }

        public void ConfigureCaps(ShaderMaterial material)
        {
            _glState.ConfigureCaps(material);

            _glState.SetShadingRate(Math.Max(1, Math.Max(material.ShadingRate, _target!.ShadingRate)));
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

        public RenderEngineFeatures Features => _features;

        public IReadOnlySet<string> Extensions => _extensions ?? [];

        public static int SuspendErrors { get; set; }

        public static OpenGLRender? Current => _current;

        public bool IsNvidia { get; private set; }
    }
}
