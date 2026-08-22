#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using XrEngine.Helpers;
using System.Numerics;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace XrEngine.OpenGL
{
    public class GlColorPass : GlBaseRenderPass
    {
        protected DepthClipEffect? _depthClipEffect;
        private ColorCopyDownsampleEffect _colorCopyEffect;
        protected readonly ShaderMaterial _dummyMaterial;

#if GLES
        readonly Silk.NET.OpenGLES.Extensions.EXT.ExtPrimitiveBoundingBox _bounds;
#endif

        public GlColorPass(OpenGLRender renderer)
            : base(renderer)
        {
            WriteDepth = true;
#if GLES
            _bounds = new Silk.NET.OpenGLES.Extensions.EXT.ExtPrimitiveBoundingBox(renderer.GL.Context);
#endif

            _dummyMaterial = new PbrMaterial
            {
                ColorMap = TextureFactory.CreateChecker(),
                Metalness = 0,
                Roughness = 0.5f
            };
        }

        protected override bool BeginRender(GlUpdateContext ctx)
        {
            ctx.UseMotionVectors = _renderer.Options.MotionVectorMode == MotionVectorMode.Shared;

            if (ctx.MotionVectorProvider != null && ctx.MotionVectorProvider.IsActive)
                ctx.MotionVectorProvider.Begin();

            GetRenderTarget()?.Begin(ctx.PassCamera!);

            _renderer.State.SetWriteColor(true);

            if (_renderer.Options.UseDepthPass)
            {
                _gl.Clear(ClearBufferMask.ColorBufferBit);
                _gl.DepthFunc(DepthFunction.Lequal);
            }
            else
            {
                _renderer.State.SetWriteDepth(true);
                _renderer.State.SetClearDepth(1.0f);
                _renderer.State.SetClearStencil(0);
                _renderer.State.Commit();

                _gl.ClearBuffer(BufferKind.Color, 0, ctx.PassCamera!.BackgroundColor.AsSpan());

                _gl.Clear(ClearBufferMask.StencilBufferBit | ClearBufferMask.DepthBufferBit);

                if (ctx.Bugs.NvMultiViewClipBug &&
                    ctx.ClipRegions != null &&
                    ctx.ClipRegions.Length > 1 &&
                    ctx.IsMultiView)
                {
                    _depthClipEffect ??= new DepthClipEffect();
                    UseEffect(_depthClipEffect);
                    DrawVirtual(6);
                }
            }

            return true;
        }

        protected override IEnumerable<IGlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => (a.Type & GlLayerType.Color) == GlLayerType.Color ||
                                               (a.Type & GlLayerType.Static) == GlLayerType.Static ||
                                               (a.SceneLayer is DetachedLayer det && det.Usage != DetachedLayerUsage.Outline))
                                    .Where(a => a.SceneLayer == null || a.SceneLayer.IsVisible);
        }

        protected override void EndRender(GlUpdateContext ctx)
        {
            _renderer.State.SetActiveProgram(0);

            var isSharedMv = _renderer.Options.MotionVectorMode == MotionVectorMode.Shared;

            if (ctx.MotionVectorProvider != null && ctx.MotionVectorProvider.IsActive && isSharedMv)
            {
                if (ctx.PassCamera!.ActiveEye == -1 || ctx.PassCamera.ActiveEye == 1)
                {
                    ctx.MotionVectorProvider!.Swap(ctx.PassCamera,
                        SelectLayers()
                        .OfType<GlLayer>()
                        .SelectMany(a => a.Content.Contents)
                        .SelectMany(a => a.Value.Contents)
                        .SelectMany(a => a.Value.Contents)
                        .SelectMany(a => a.Value.Contents)
                        .Select(a => a.Object!)
                        .Where(a => a != null));
                }
            }

            ctx.UseMotionVectors = false;
        }

        protected virtual bool CanDraw(DrawContent draw)
        {

            if (draw.IsHidden || draw.IsClipped)
                return false;

            if (draw.Query != null)
            {
                var passed = draw.Query.GetResult();
                if (passed == 0)
                    return false;
            }

            return true;
        }

        protected void Draw(DrawContent draw)
        {
            draw.Draw!();

#if DEBUG
            var name = draw.Object!.Name;
            if (name != null)
                _gl.DebugMessageInsert(DebugSource.DebugSourceApplication, DebugType.DebugTypeMarker, 0, DebugSeverity.DebugSeverityNotification, (uint)name.Length, name);
#endif

        }

        protected virtual bool UpdateProgram(UpdateShaderContext ctx, GlProgramInstance progInst, bool forceSync = false)
        {
            return progInst.UpdateProgram(ctx, forceSync);
        }

        protected void SetBounds(UpdateShaderContext ctx)
        {

#if GLES
            if (!_renderer.Options.UsePrimitiveBoundingBox || !_renderer.Features.PrimitiveBoundingBox)
                return;

            ctx.UsePrimitiveBoundingBox = false;

            Debug.Assert(ctx.Material != null && ctx.Model != null && ctx.PassCamera != null);

            if (ctx.Material.UseSkin)
                return;

            ctx.UsePrimitiveBoundingBox = true;

            var min = new Vector4(float.PositiveInfinity);
            var max = new Vector4(float.NegativeInfinity);

            foreach (var p in ctx.Model.WorldBounds.Points)
            {
                var clip = Vector4.Transform(new Vector4(p, 1), ctx.PassCamera.ViewProjection);
                min = Vector4.Min(min, clip);
                max = Vector4.Max(max, clip);
            }

            _bounds.PrimitiveBoundingBox(
                min.X, min.Y, min.Z, min.W,
                max.X, max.Y, max.Z, max.W);
#endif
        }

        protected virtual void ConfigureCaps(ShaderMaterial material)
        {
            var glState = _renderer.State;

            _renderer.ConfigureCaps(material);

            if (!WriteDepth)
                glState.SetWriteDepth(false);

            var clipRegions = _renderer.UpdateContext.ClipRegions;

            var enableClipRegions = clipRegions != null &&
                                    clipRegions.Length > 0;

            glState.EnableFeature(EnableCap.ClipDistance1, enableClipRegions);
            glState.EnableFeature(EnableCap.ClipDistance2, enableClipRegions);
            glState.EnableFeature(EnableCap.ClipDistance3, enableClipRegions);
            glState.EnableFeature(EnableCap.ClipDistance4, enableClipRegions);
        }

        [MemberNotNull(nameof(_colorCopyEffect))]
        protected void PrepareColorCopy()
        {
            if (_colorCopyEffect == null)
            {
                var ctx = _renderer.UpdateContext;

                var downsample = (uint)_renderer.Options.RefractionDownsampleFactor;

                var colorSize = ctx.PassCamera!.ViewSize;

                _colorCopyEffect = new ColorCopyDownsampleEffect
                {
                    DownsampleFactor = (int)downsample,
                    IsMultiView = ctx.IsMultiView,
                    ShadingRate = (int)downsample,
                    DestTexture = new Texture2D
                    {
                        Depth = ctx.IsMultiView ? 2u : 1u,
                        Height = colorSize.Height / downsample,
                        Width = colorSize.Width / downsample,
                        MipLevelCount = 10,
                        MinFilter = ScaleFilter.LinearMipmapLinear,
                        MagFilter = ScaleFilter.Linear,
                        WrapS = WrapMode.ClampToEdge,
                        WrapT = WrapMode.ClampToEdge,
                        Format = TextureFormat.Rgba8,
                        NeverCompress = true,
                        Name = "Refraction Foreground"
                    }
                };
            }

            if (!_renderer.Features.ShaderFramebufferFetch)
            {
                var color = (_renderer.RenderTarget?.QueryTexture(FramebufferAttachment.ColorAttachment0)?.ToEngineTexture()) ??
                    throw new NotSupportedException();

                _colorCopyEffect.SourceTexture = (Texture2D)color;
            }
        }

        public override void RenderLayer(GlLayer layer)
        {
            GlUtils.EnsureRenderThread();

            if (layer.SceneLayer != null && !layer.SceneLayer.IsVisible)
                return;
            _renderer.PushGroup($"Layer {layer.Name ?? layer.Type.ToString()}");

            var ctx = _renderer.UpdateContext;

            var useDepthPass = _renderer.Options.UseDepthPass;

            var useOcclusion = _renderer.Options.UseOcclusionQuery;

            uint globalProgChangesCount = 0;

            if (layer.Type == GlLayerType.Refraction)
            {
                PrepareColorCopy();
                UseEffect(_colorCopyEffect);
                DrawQuad();

                _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

                _colorCopyEffect.DestTexture!.ToGlTexture().GenerateMipmap();

                ctx.VolumeForeground = _colorCopyEffect.DestTexture;
            }
            else
                ctx.VolumeForeground = null;

            foreach (var shader in layer.Content.SortedContent!)
            {
                var progGlobal = shader.Value!.ProgramGlobal;

                ctx.Shader = shader.Key;
                ctx.Stage = UpdateShaderStage.Shader;

                progGlobal!.UpdateProgram(ctx, GetRenderTarget()?.ShaderHandler);

                foreach (var material in shader.Value.SortedContent!)
                {
                    var matContent = material.Value;

                    if (material.Value.IsHidden)
                        continue;

                    ctx.Material = matContent.Material as ShaderMaterial;

                    ctx.UseInstanceDraw = matContent.UseInstanceDraw;

                    var progInst = matContent.ProgramInstance!;

                    ctx.Stage = UpdateShaderStage.Material;

                    ctx.ActiveComponents = matContent.ActiveComponents;

                    ctx.Model = matContent.SingleModel;

                    Debug.Assert(ctx.Model != null || !ctx.Material!.UseMorph);

                    var progChanged = UpdateProgram(ctx, progInst);

                    if (!progInst.IsReady)
                    {
                        progInst = GetProgramInstance(_dummyMaterial);
                        ctx.Stage = UpdateShaderStage.Shader;
                        progInst.Global.UpdateProgram(ctx, GetRenderTarget()?.ShaderHandler);
                        ctx.Stage = UpdateShaderStage.Material;
                        ctx.Material = progInst.Material;
                        progChanged = UpdateProgram(ctx, progInst);
                    }

                    var programChanged = ctx.ProgramInstanceId != progInst.Program!.Handle;

                    ctx.ProgramInstanceId = progInst.Program!.Handle;

                    progInst.Program.Use();

                    progInst.UpdateBuffers(ctx);

                    progInst.UpdateUniforms(ctx, programChanged);

                    ConfigureCaps(progInst.Material!);

                    if (progChanged)
                    {
                        globalProgChangesCount++;
                        layer.Invalidate(shader.Value);
                    }

                    foreach (var vertex in matContent.Contents)
                    {
                        var vertexContent = vertex.Value;
                        if (vertexContent.IsHidden)
                            continue;

                        if (vertexContent.Contents.All(a => a.IsClipped))
                            continue;

                        var vHandler = vertexContent.VertexHandler!;

                        vHandler.Bind();

                        ctx.Stage = UpdateShaderStage.Model;

                        if (vertexContent.Draw != null)
                        {
#if GL_VALIDATE_PROG
                            progInst.Program.Validate();
#endif
                            vertexContent.Draw();
                        }
                        else
                        {
                            foreach (var draw in vertexContent.Contents)
                            {
                                if (!CanDraw(draw))
                                    continue;

                                ctx.Model = draw.Object;

                                progInst.UpdateModel(ctx);

                                SetBounds(ctx);

#if GL_VALIDATE_PROG
                                progInst.Program.Validate();
#endif

                                Draw(draw);
                            }
                        }
                    }
                }

                ctx.Material = null;
            }

            _renderer.State.BindVertexArray(0);

            _renderer.PopGroup();

            if (globalProgChangesCount > 0)
                Log.Debug(this, "Changes: {0}", globalProgChangesCount);

        }

        public bool WriteDepth { get; set; }
    }
}
