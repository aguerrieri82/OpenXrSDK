#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

        public ShaderUpdate? LastUpdate { get; set; }
        public int DrawId { get; set; }
    }

    public class OpenGLRender : IRenderEngine
    {
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
            _defaultTarget  = new GlDefaultRenderTarget(gl);
            _target = _defaultTarget;
            _options.ProgramFactory ??= new GlDefaultProgramFactory();
            _updateCtx = new UpdateShaderContext();
            _updateCtx.RenderEngine = this;

        }

        protected GlProgram GetProgram(ShaderMaterial material)
        {
            return material.Shader!.GetResource(a =>
                    _options.ProgramFactory!.CreateProgram(_gl, material));
        }

        public void Clear(Color color)
        {
            _gl.ClearColor(color.R, color.G, color.B, color.A);
            _gl.ClearDepth(1.0f);
            _gl.ClearStencil(0);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
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
                        DrawId = drawId++,
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

            // _gl.Enable(EnableCap.DebugOutput);
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

        protected ShaderUpdate UpdateProgram(GlProgram prog, string progId, params IShaderHandler?[] handlers)
        {
            var updateBuilder = new ShaderUpdateBuilder(_updateCtx);

            foreach (var handler in handlers)
                handler?.UpdateShader(updateBuilder);

            updateBuilder.ComputeHash(progId);

            var update = updateBuilder.Result;

            prog.BeginEdit();

            if (update.Features != null)
            {
                foreach (var feature in update.Features)
                    prog.AddFeature(feature);
            }

            if (update.Extensions != null)
            {
                foreach (var ext in update.Extensions)
                    prog.AddExtension(ext);
            }

            prog.Commit(update.FeaturesHash!);

            return update;
        }

        public void Render(Scene scene, Camera camera, Rect2I view, IGlRenderTarget target)
        {
            _view = view;

            target.Begin();

            Clear(camera.BackgroundColor);

            _gl.FrontFace(FrontFaceDirection.Ccw);
            _gl.CullFace(TriangleFace.Back);

            _gl.Viewport(view.X, view.Y, view.Width, view.Height);

            _gl.LineWidth(1);

            var targetHandler = target as IShaderHandler;

            if (_content == null || _content.Scene != scene || _content.Version != scene.Version)
                BuildContent(scene);

            _updateCtx.Camera = camera;
            _updateCtx.Lights = _content.Lights;
            _updateCtx.LightsVersion = _content.Version;

            /*
                       int drawId = 1;

                       _gl.Enable(EnableCap.StencilTest);

                       _gl.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);

                       if (_writeStencil == null)
                       {
                           _writeStencil = new GlSimpleProgram(_gl,
                               Embedded.GetString<Shader>("standard.vert"),
                               Embedded.GetString<Shader>("empty.frag"),
                               Embedded.GetString<Shader>);

                           _writeStencilUpdate = UpdateProgram(_writeStencil, "write-stencil", targetHandler, StandardVertexShaderHandler.Instance);
                       }

                       _writeStencil.Use(_writeStencilUpdate!.FeaturesHash!);

                       foreach (var updateUniform in _writeStencilUpdate.Actions!)
                           updateUniform(_updateCtx, _writeStencil);

                       foreach (var shader in _content.ShaderContents)
                       {
                           foreach (var vertex in shader.Value.Contents)
                           {
                               var vHandler = vertex.Value.VertexHandler!;

                               vHandler.Bind();

                               foreach (var draw in vertex.Value.Contents)
                               {
                                   ConfigureCaps(draw.Material!);

                                   _writeStencil.SetUniform("uModel", draw.Object!.WorldMatrix);

                                   _gl.StencilFunc(StencilFunction.Always, drawId, 0xFF);
                                   draw.DrawId = drawId;
                                   draw.Draw!();
                                   drawId++;
                               }
                           }
                       }

                       _gl.Clear(ClearBufferMask.DepthBufferBit);

                       _gl.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
            */

            int skipCount = 0;

            foreach (var shader in _content.ShaderContents.OrderBy(a => a.Key.Priority))
            {
                var prog = shader.Value!.Program!;

                foreach (var vertex in shader.Value.Contents)
                {
                    var vHandler = vertex.Value.VertexHandler!;

                    if (vertex.Key is TriangleMesh mesh)
                    {
                        //vertex.Value.Contents[0].DrawId
                        if (!camera.CanSee(mesh.WorldBounds))
                        {
                            skipCount++;
                            //continue;
                        }
                    }

                    if (vertex.Key.Version != vertex.Value.Version)
                    {
                        vHandler.Update();
                        vertex.Value.Version = vertex.Key.Version;
                    }

                    vHandler.Bind();

                    _updateCtx.ActiveComponents = VertexComponent.None;

                    foreach (var attr in vHandler.Layout!.Attributes!)
                        _updateCtx.ActiveComponents |= attr.Component;

                    foreach (var draw in vertex.Value.Contents)
                    {
                        ConfigureCaps(draw.Material!);

                        _updateCtx.Model = draw.Object;

                        var update = draw.LastUpdate;

                        if (update == null || draw.Material!.NeedUpdateShader(_updateCtx, update))
                        {
                            update = UpdateProgram(prog,
                                draw.Material!.GetType().FullName!,
                                draw.Material,
                                targetHandler);

                            update.LightsVersion = _updateCtx.LightsVersion;
                            update.MaterialVersion = draw.Material!.Version;

                            draw.LastUpdate = update;
                        }

                        prog.Use(update.FeaturesHash!);

                        if (update.Actions != null)
                        {
                            foreach (var updateUniform in update.Actions)
                                updateUniform(_updateCtx, prog);
                        }

                        // _gl.StencilFunc(StencilFunction.Equal, draw.DrawId, 0xFF);

                        draw.Draw!();
                    }

                    vHandler.Unbind();
                }

                prog.Unbind();
            }

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
