#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using XrEngine;
using XrEngine.OpenGL;
using XrMath;
using XrSamples.Graffiti.Shaders;

namespace XrSamples.Graffiti
{

    public class GlSimulationPass : GlBaseRenderPass
    {
        protected readonly GlTextureFrameBuffer _sprayFrameBuffer;

        protected SprayBrush? _brush;
        protected GlVertexSourceHandle? _brushSource;

        protected PaintCanvas? _canvas;
        protected Can? _can;
        protected SprayTracker? _tracker;

        protected readonly GlComputeProgram _accumulateProgram;
        protected readonly GlComputeProgram _dripProgram;
        protected readonly GlComputeProgram _resolveProgram;
        protected readonly GlSimpleProgram _sprayProgram;

        protected GlTexture _wetTex;
        protected GlTexture _tempWetTex;
        protected readonly GlTexture _dryTex;
        protected readonly GlTexture _drySurfTex;

        protected readonly GlBuffer<PaintSimUniforms> _paintUniformsBuffer;
        protected PaintSimUniforms _paintUniforms;

        protected readonly GlBuffer<PaintProjUniforms> _sprayUniformsBuffer;
        protected PaintProjUniforms _sprayUniforms;

        protected Vector3 _prevCanvasTarget;
        protected Pose3 _prevPose;
        protected long _lastFrame;
        private bool _isSprayClear;
        private Vector2 _lastCanvasSize;
        private bool _isFirstSizeUpdate;

        public GlSimulationPass(OpenGLRender renderer)
            : base(renderer)
        {
            UseInstance = true;
            SprayMaxSamples = 100;

            _sprayFrameBuffer = new GlTextureFrameBuffer(_gl);

            _sprayProgram = new GlSimpleProgram(renderer.GL, "paint_proj.vert", "paint_proj.frag", str => Embedded.GetString<GlSimulationPass>(str));

            if (UseInstance)
                _sprayProgram.AddFeature("USE_INSTANCE");

            _sprayProgram.Build();

            _accumulateProgram = new GlComputeProgram(renderer.GL, "paint_accumulate.comp", str => Embedded.GetString<GlSimulationPass>(str));
            _accumulateProgram.Build();

            _dripProgram = new GlComputeProgram(renderer.GL, "paint_drip.comp", str => Embedded.GetString<GlSimulationPass>(str));
            _dripProgram.Build();

            _resolveProgram = new GlComputeProgram(renderer.GL, "paint_res.comp", str => Embedded.GetString<GlSimulationPass>(str));
            _resolveProgram.Build();

            _sprayUniformsBuffer = new GlBuffer<PaintProjUniforms>(_gl, BufferTargetARB.UniformBuffer);
            _sprayUniforms = new PaintProjUniforms();

            GlTexture CreateTexture() => new(_gl)
            {
                MaxLevel = 0
            };

            _wetTex = CreateTexture();
            _tempWetTex = CreateTexture();
            _dryTex = CreateTexture();
            _drySurfTex = CreateTexture();

            _paintUniformsBuffer = new GlBuffer<PaintSimUniforms>(_gl, BufferTargetARB.UniformBuffer);
            _paintUniforms = new PaintSimUniforms();

#if DEBUG
            _renderer.EnableDebug(true);
#endif
        }

        public override void Render(RenderContext ctx)
        {
            if (!_isInit)
                Intialize(ctx);

            if (ctx.DeltaTime == 0)
                return;

            if (ctx.Frame == _lastFrame)
                return;

            if (_lastCanvasSize != _canvas!.Size)
            {
                UpdateCanvasSize();
                _lastCanvasSize = _canvas.Size;
            }

            if (_canvas!.ClearRequest)
            {
                ClearCanvas();
                _canvas.ClearRequest = false;
            }

            GlState.Current!.SetActiveBuffer(_sprayUniformsBuffer, 10);
            GlState.Current!.SetActiveBuffer(_paintUniformsBuffer, 11);

            RenderSpray(ctx);
            RenderAccumulate(ctx);
            RenderDrip(ctx);
            RenderResolve(ctx);

            GlState.Current!.SetActiveProgram(0);

            _lastFrame = ctx.Frame;
        }


        protected void Intialize(RenderContext ctx)
        {
            _isFirstSizeUpdate = true;

            _canvas = ctx.Scene!.Descendants<PaintCanvas>().First();
            _brush = ctx.Scene!.Descendants<SprayBrush>().First();
            _can = ctx.Scene!.Descendants<Can>().First();
            _tracker = _can.Component<SprayTracker>();

            void InitTexture(Texture2D tex)
            {
                tex.WrapS = WrapMode.ClampToEdge;
                tex.WrapT = WrapMode.ClampToEdge;
                tex.MipLevelCount = 1;
                tex.MinFilter = ScaleFilter.Linear;
                tex.MagFilter = ScaleFilter.Linear;
            }

            InitTexture(_canvas!.ColorTexture);
            InitTexture(_canvas!.RoughnessTexture);
            InitTexture(_canvas!.NormalTexture);
            InitTexture(_canvas!.SprayTexture);

            _brushSource = _brush.GetGlResource(a => GlVertexSourceHandle.Create(_gl, _brush));
            _brushSource.Update();

            _isInit = true;
        }

        protected void UpdateCanvasSize()
        {

            var data = new TextureData
            {
                Format = TextureFormat.RgbaFloat16,
                Width = (uint)(_canvas!.Size.X / _canvas.TexelSize),
                Height = (uint)(_canvas.Size.Y / _canvas.TexelSize),
                Depth = 1
            };

            if (!_isFirstSizeUpdate)
            {
                _dryTex.Recreate();
                _tempWetTex.Recreate();
                _wetTex.Recreate();
                _drySurfTex.Recreate();
                _canvas!.ColorTexture.ToGlTexture().Recreate();
                _canvas!.RoughnessTexture.ToGlTexture().Recreate();
                _canvas!.NormalTexture.ToGlTexture().Recreate();
                _canvas!.SprayTexture.ToGlTexture().Recreate();
            }

            _dryTex.Update(1, data);
            _tempWetTex.Update(1, data);
            _wetTex.Update(1, data);
            _drySurfTex.Update(1, data);

            _canvas!.ColorTexture.ToGlTexture().Update(1, data);
            _canvas!.RoughnessTexture.ToGlTexture().Update(1, data);
            _canvas!.NormalTexture.ToGlTexture().Update(1, data);
            _canvas!.SprayTexture.ToGlTexture().Update(1, data);

            _sprayFrameBuffer.Configure(_canvas!.SprayTexture.ToGlTexture(), null, 1);

            _canvas.PaintTextures = [(Texture2D)_wetTex.ToEngineTexture(), (Texture2D)_dryTex.ToEngineTexture()];

            ClearCanvas();

            _isFirstSizeUpdate = false;
        }

        protected void ClearCanvas()
        {
            _wetTex.Clear(Color.Transparent);
            _tempWetTex.Clear(Color.Transparent);
            _dryTex.Clear(Color.Transparent);
            _drySurfTex.Clear(Color.Transparent);
        }

        protected void RenderAccumulate(RenderContext ctx)
        {
            _accumulateProgram.Use();

            _canvas!.Update(ctx, ref _paintUniforms);
            _paintUniformsBuffer.Update(_paintUniforms);

            _accumulateProgram.SetUniform("uIncomingDensity", _canvas.SprayTexture, 0);

            _gl.BindImageTexture(1, _wetTex, 0, false, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(2, _dryTex, 0, false, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(3, _drySurfTex, 0, false, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);

            _gl.BindImageTexture(4, _wetTex, 0, false, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(5, _dryTex, 0, false, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(6, _drySurfTex, 0, false, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);

            _gl.DispatchCompute((_wetTex.Width + 7) / 8, (_wetTex.Height + 7) / 8, 1);

            _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

        }

        protected void RenderDrip(RenderContext ctx)
        {
            _dripProgram.Use();

            _gl.BindImageTexture(0, _wetTex, 0, false, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(1, _tempWetTex, 0, false, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);

            _gl.DispatchCompute((_wetTex.Width + 7) / 8, (_wetTex.Height + 7) / 8, 1);

            _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            (_tempWetTex, _wetTex) = (_wetTex, _tempWetTex);
        }


        protected void RenderResolve(RenderContext ctx)
        {
            _resolveProgram.Use();

            _gl.BindImageTexture(0, _wetTex, 0, false, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(1, _dryTex, 0, false, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(2, _drySurfTex, 0, false, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);

            _gl.BindImageTexture(3, _canvas!.ColorTexture.ToGlTexture(), 0, false, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(4, _canvas!.RoughnessTexture.ToGlTexture(), 0, false, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(5, _canvas!.NormalTexture.ToGlTexture(), 0, false, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);

            _gl.DispatchCompute((_wetTex.Width + 7) / 8, (_wetTex.Height + 7) / 8, 1);

            _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
        }

        protected void RenderSpray(RenderContext ctx)
        {
            var ray = new Ray3
            {
                Origin = _tracker!.SprayCenter.Transform(_can!.WorldMatrix),
                Direction = Vector3.TransformNormal(_tracker.SprayDirection, _can.WorldMatrix)
            };

            var canvasQuod = new Quad3
            {
                Size = _canvas!.Size,
                Pose = _canvas.GetWorldPose()
            };

            var isInCanvas = ray.Intersects(canvasQuod, out var curCanvasTarget);

            var curPose = _can.GetWorldPose();

            var mustDraw = _can!.SprayAperture > 0 && isInCanvas;

            var useFrameBuffer = mustDraw || !_isSprayClear;


            if (useFrameBuffer)
            {
                _sprayFrameBuffer.Bind();

                GlState.Current!.SetView(new Rect2I(0, 0, _sprayFrameBuffer.Color!.Width, _sprayFrameBuffer.Color.Height));

                GlState.Current!.SetWriteDepth(false);
                GlState.Current!.SetUseDepth(false);
                GlState.Current!.SetWriteColor(true);
                GlState.Current!.SetAlphaMode(AlphaMode.Add);

                _gl.Clear(ClearBufferMask.ColorBufferBit);

                _isSprayClear = true;
            }

            if (mustDraw)
            {
                var distance = (curCanvasTarget - _prevCanvasTarget).Length();

                var sampleCount = Math.Max(1, (int)MathF.Ceiling(distance / _canvas.SpraySpacing));
                sampleCount = Math.Min(sampleCount, SprayMaxSamples);

                _sprayProgram.Use();
                _tracker.Update(ref _sprayUniforms);

                _brushSource!.Bind();

                _sprayUniforms.DensityScale = _sprayUniforms.DensityScale / sampleCount;

                if (!UseInstance)
                {
                    for (var i = 0; i < sampleCount; ++i)
                    {
                        var factor =
                            sampleCount == 1
                                ? 1.0f
                                : (i + 1.0f) / sampleCount;

                        var stepPose = _prevPose.Lerp(curPose, factor);

                        _sprayUniforms.CanWorld = stepPose.ToMatrix(_can.Transform.Scale);
                        _sprayUniformsBuffer.Update(_sprayUniforms);

                        _brushSource.Draw();
                    }
                }
                else
                {
                    _sprayUniforms.PrevPosition = _prevPose.Position;
                    _sprayUniforms.PrevRotation = _prevPose.Orientation;

                    _sprayUniforms.CurPosition = curPose.Position;
                    _sprayUniforms.CurRotation = curPose.Orientation;
                    _sprayUniforms.StepCount = sampleCount;
                    _sprayUniforms.CanScale = _can.Transform.Scale;

                    _sprayUniformsBuffer.Update(_sprayUniforms);

                    _brushSource.DrawInstances(sampleCount);
                }

                _brushSource.Unbind();

                _isSprayClear = false;
            }

            _prevCanvasTarget = curCanvasTarget;
            _prevPose = curPose;

            if (useFrameBuffer)
            {
                _gl.MemoryBarrier(MemoryBarrierMask.TextureFetchBarrierBit);

                _sprayFrameBuffer.Unbind();
            }
        }


        public bool UseInstance { get; set; }

        public int SprayMaxSamples { get; set; }
    }
}
