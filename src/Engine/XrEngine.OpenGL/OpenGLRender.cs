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


namespace XrEngine.OpenGL
{
    public class GlobalContent
    {
        public IList<Light>? Lights;

        public long ImageLightVersion = -1;

        public long LightsVersion = -1;

        public long SceneVersion = -1;

        public Scene3D? Scene;

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

    public class OpenGLRender : IRenderEngine, ISurfaceProvider, IIBLPanoramaProcessor, IFrameReader
    {
        protected class GlState
        {
            public bool? WriteDepth;
            public bool? UseDepth;
            public bool? DoubleSided;
            public bool? WriteColor;
            public uint? ActiveProgram;
            public bool? Wireframe;
            public AlphaMode? Alpha;
            public Rect2I? LastView;
        }

        protected GL _gl;
        protected Dictionary<Scene3D, GlobalContent> _contents = [];
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

        public static class Props
        {
            public const string GlResId = nameof(GlResId);
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
            _target = _defaultTarget;

            _updateCtx = new UpdateShaderContext();
            _updateCtx.RenderEngine = this;

            _dispatcher = new QueueDispatcher();

            _drawAttachment0 = [DrawBufferMode.ColorAttachment0];

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

        protected GlobalContent BuildContent(Scene3D scene)
        {
            Log.Info(this, "Building content...");
        
            if (!_contents.TryGetValue(scene, out var content))
            {
                content = new GlobalContent();
                _contents[scene] = content;
            }

            content.Lights = new List<Light>();
            content.Scene = scene;
            content.SceneVersion = scene.Version;
            content.LightsVersion = -1;

            content.ShaderContents.Clear();

            var drawId = 0;

            foreach (var light in scene.VisibleDescendants<Light>())
            {
                content.Lights.Add(light);
                content.LightsVersion += light.Version;

                if (light is ImageLight imgLight)
                {
                    if (imgLight.Panorama?.Data != null && imgLight.Panorama.Version != content.ImageLightVersion)
                    {
                        var options = PanoramaProcessorOptions.Default();
                        options.SampleCount = 1024;
                        options.Resolution = 256;
                        options.Mode = IBLProcessMode.GGX | IBLProcessMode.Lambertian;
                        imgLight.Textures = ProcessPanoramaIBL(imgLight.Panorama.Data[0], options);
                        imgLight.Panorama.NotifyLoaded();
                        content.ImageLightVersion = imgLight.Panorama.Version;
                        ResetState();
                    }
                }
            }


            foreach (var obj3D in scene.VisibleDescendants<Object3D>())
            {
                if (obj3D is Light light)
                    continue;

                if (!obj3D.Feature<IVertexSource>(out var vrtSrc))
                    continue;


                foreach (var material in vrtSrc.Materials.OfType<ShaderMaterial>())
                {
                    if (material.Shader == null)
                        continue;

                    if (!content.ShaderContents.TryGetValue(material.Shader, out var shaderContent))
                    {
                        shaderContent = new ShaderContent
                        {
                            ProgramGlobal = material.Shader.GetResource(gl => new GlProgramGlobal(_gl, material.GetType()))
                        };

                        content.ShaderContents[material.Shader] = shaderContent;
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
                        ProgramInstance = new GlProgramInstance(_gl, material, shaderContent.ProgramGlobal!),
                        DrawId = drawId++,
                        Object = obj3D
                    });
                }
            }

            //_content.ShaderContentsOrder.Clear();
            //_content.ShaderContentsOrder.AddRange(_content.ShaderContents);

            Log.Debug(this, "Content Build");

            return content;
        }

        protected void ResetState()
        {
            _glState = new GlState();

            GL.DrawBuffers(_drawAttachment0.AsSpan());
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
            EnableFeature(EnableCap.FramebufferSrgb, _options.UseSRGB);
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
        }

        public void Render(Scene3D scene, Camera camera, Rect2I view, bool flush)
        {
            if (_target != null)
                Render(scene, camera, view, _target);
        }

        public void Render(Scene3D scene, Camera camera, Rect2I view, IGlRenderTarget target)
        {
            if (!_contents.TryGetValue(scene, out var content) || content.SceneVersion != scene.Version)
                content = BuildContent(scene);

            target.Begin();

            if (_glState.LastView == null || !_glState.Equals(view))
            {
                _gl.Viewport(view.X, view.Y, view.Width, view.Height);

                _glState.LastView = view;
            }

            Clear(camera.BackgroundColor);

            var targetHandler = target as IShaderHandler;

            _updateCtx.Camera = camera;
            _updateCtx.Lights = content.Lights;
            _updateCtx.LightsVersion = content.LightsVersion;

            int skipCount = 0;

            foreach (var shader in content.ShaderContents.OrderBy(a => a.Key.Priority))
            {
                var progGlobal = shader.Value!.ProgramGlobal;

                _updateCtx.Shader = shader.Key;

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

                        _updateCtx.Model = draw.Object;

                        progInst.UpdateProgram(_updateCtx);

                        _updateCtx.InstanceId = progInst.Program!.Handle;

                        bool updateGlobals = false;

                        if (_glState.ActiveProgram != progInst.Program!.Handle)
                        {
                            progInst.Program!.Use();
                            _glState.ActiveProgram = progInst.Program!.Handle;
                            updateGlobals = true;
                        }

                        progInst.UpdateUniforms(_updateCtx, updateGlobals);

                        ConfigureCaps(draw.ProgramInstance!.Material!);

                        draw.Draw!();
                    }

                    vHandler.Unbind();
                }
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
            var glTexture = texture.GetResource(a =>
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
            var glTexture = texture.GetResource(tex => tex.CreateGlTexture(_gl, false));
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

        public IDispatcher Dispatcher => _dispatcher;

        public IGlRenderTarget? RenderTarget => _target;

        public GlRenderOptions Options => _options;

        public static OpenGLRender? Current { get; internal set; }

        public static int SuspendErrors { get; set; }
    }
}
