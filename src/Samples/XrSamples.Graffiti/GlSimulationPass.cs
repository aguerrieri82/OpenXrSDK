#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using Common.Interop;
using System.Numerics;
using XrEngine;
using XrEngine.OpenGL;
using XrMath;
using XrSamples.Graffiti.Shaders;
using System.Diagnostics;

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
        protected readonly GlComputeProgram _dryProgram;
        protected readonly GlComputeProgram _dripProgram;
        protected readonly GlComputeProgram _resolveProgram;
        protected readonly GlSimpleProgram _sprayProgram;

        protected GlTexture _wetTex;
        protected GlTexture _tempWetTex;
        protected GlTexture _undoWetTex;

        protected GlTexture _dryTex;
        protected GlTexture _tempDryTex;
        protected GlTexture _undoDryTex;

        protected readonly GlBuffer<PaintSimUniforms> _paintUniformsBuffer;
        protected PaintSimUniforms _paintUniforms;

        protected readonly GlBuffer<SprayUniforms> _sprayUniformsBuffer;
        protected SprayUniforms _sprayUniforms;

        protected Vector3 _prevCanvasTarget;
        protected Pose3 _prevPose;
        protected long _lastFrame;
        protected bool _isSprayClear;
        protected Rect2I _sprayRect;
        protected Vector2 _lastCanvasSize;
        protected bool _isFirstSizeUpdate;
        protected bool _spraySessionStarted;

        protected readonly GlBuffer<PaintStateBuffer> _paintStateBuffer;
        protected PaintStateBuffer _paintState;
        protected IMemoryBuffer<byte>[] _readBuffer = new IMemoryBuffer<byte>[1];
        private bool _hasUndo;

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

            _dryProgram = new GlComputeProgram(renderer.GL, "paint_dry.comp", str => Embedded.GetString<GlSimulationPass>(str));
            _dryProgram.Build();

            _dripProgram = new GlComputeProgram(renderer.GL, "paint_drip.comp", str => Embedded.GetString<GlSimulationPass>(str));
            _dripProgram.Build();

            _resolveProgram = new GlComputeProgram(renderer.GL, "paint_res.comp", str => Embedded.GetString<GlSimulationPass>(str));
            _resolveProgram.Build();

            _sprayUniformsBuffer = new GlBuffer<SprayUniforms>(_gl, BufferTargetARB.UniformBuffer);
            _sprayUniforms = new SprayUniforms();

            _paintStateBuffer = new GlBuffer<PaintStateBuffer>(_gl, BufferTargetARB.ShaderStorageBuffer);
            _paintState = new PaintStateBuffer();

            GlTexture CreateTexture() => new(_gl)
            {
                MaxLevel = 0
            };

            _wetTex = CreateTexture();
            _tempWetTex = CreateTexture();
            _dryTex = CreateTexture();
            _tempDryTex = CreateTexture();

            _undoDryTex = CreateTexture();
            _undoWetTex = CreateTexture();

            _paintUniformsBuffer = new GlBuffer<PaintSimUniforms>(_gl, BufferTargetARB.UniformBuffer);
            _paintUniforms = new PaintSimUniforms();

#if DEBUG
            _renderer.EnableDebug(true);
#endif
        }

        public static Rect2I ComputeSprayTextureRect(
             in SprayUniforms uniforms,
             in Pose3 oldPose,
             in Pose3 curPose,
             in Vector3 canScale,
             in Quad3 canvasQuad,
             in Size2I textureSize,
             int marginPixels = 8)
        {
            var bounds = new Bounds2
            {
                Min = new Vector2(float.PositiveInfinity, float.PositiveInfinity),
                Max = new Vector2(float.NegativeInfinity, float.NegativeInfinity)
            };

            AccumulateSprayPoseBounds(
                in uniforms,
                oldPose.ToMatrix(canScale),
                in canvasQuad,
                in textureSize,
                ref bounds);

            AccumulateSprayPoseBounds(
                in uniforms,
                curPose.ToMatrix(canScale),
                in canvasQuad,
                in textureSize,
                ref bounds);

            if (!float.IsFinite(bounds.Min.X) || !float.IsFinite(bounds.Min.Y) ||
                !float.IsFinite(bounds.Max.X) || !float.IsFinite(bounds.Max.Y))
            {
                return new Rect2I(0, 0, 0, 0);
            }

            int x0 = (int)MathF.Floor(bounds.Min.X) - marginPixels;
            int y0 = (int)MathF.Floor(bounds.Min.Y) - marginPixels;
            int x1 = (int)MathF.Ceiling(bounds.Max.X) + marginPixels;
            int y1 = (int)MathF.Ceiling(bounds.Max.Y) + marginPixels;

            x0 = Math.Clamp(x0, 0, (int)textureSize.Width);
            y0 = Math.Clamp(y0, 0, (int)textureSize.Height);
            x1 = Math.Clamp(x1, 0, (int)textureSize.Width);
            y1 = Math.Clamp(y1, 0, (int)textureSize.Height);

            int width = x1 - x0;
            int height = y1 - y0;

            if (width <= 0 || height <= 0)
                return new Rect2I(0, 0, 0, 0);

            return new Rect2I(
                x0,
                y0,
                (uint)width,
                (uint)height);
        }

        private static void AccumulateSprayPoseBounds(
            in SprayUniforms uniforms,
            in Matrix4x4 canWorld,
            in Quad3 canvasQuad,
            in Size2I textureSize,
            ref Bounds2 bounds)
        {
            var sprayCenterLocal = uniforms.SprayCenterLocal;

            var sprayDirectionLocal =
                uniforms.SprayDirectionLocal.LengthSquared() > 0.000001f
                    ? Vector3.Normalize(uniforms.SprayDirectionLocal)
                    : Vector3.UnitZ;

            float radius = uniforms.SprayRadius;
            float angle = MathF.Max(uniforms.SpreadAngle, 0.0001f);

            float h = radius / MathF.Tan(angle);

            var localApex =
                sprayCenterLocal -
                sprayDirectionLocal * h;

            MathUtils.BuildBasis(
                sprayDirectionLocal,
                out var tangentLocal,
                out var bitangentLocal);

            var canvasPlane = canvasQuad.ToPlane();

            const int sampleCount = 64;

            for (int i = 0; i < sampleCount; i++)
            {
                float a = MathF.Tau * i / sampleCount;
                var c = new Vector2(MathF.Cos(a), MathF.Sin(a));

                var localCirclePoint =
                    sprayCenterLocal +
                    tangentLocal * (c.X * radius) +
                    bitangentLocal * (c.Y * radius);

                var localRayDir =
                    Vector3.Normalize(localCirclePoint - localApex);

                var ray = new Ray3
                {
                    Origin = localCirclePoint.Transform(canWorld),
                    Direction = localRayDir.ToDirection(canWorld)
                };

                if (!ray.Intersects(canvasPlane, out var worldHit))
                    continue;

                var canvasPoint = canvasQuad.LocalPointAt(worldHit);

                if (!canvasPoint.InRange(Vector2.Zero, canvasQuad.Size))
                    continue;

                var pixel = CanvasPointToPixel(
                    canvasPoint,
                    canvasQuad.Size,
                    textureSize);

                bounds.Min = Vector2.Min(bounds.Min, pixel);
                bounds.Max = Vector2.Max(bounds.Max, pixel);
            }
        }

        private static Vector2 CanvasPointToPixel(
            in Vector2 canvasPoint,
            in Vector2 canvasSize,
            in Size2I textureSize)
        {
            float u = canvasPoint.X / canvasSize.X;
            float v = canvasPoint.Y / canvasSize.Y;

            return new Vector2(
                u * textureSize.Width,
                (1.0f - v) * textureSize.Height);
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

            if (_canvas!.UndoRequest)
            {
                Undo();
                _canvas.UndoRequest = false;
            }

            GlState.Current!.SetActiveBuffer(_paintUniformsBuffer, 11);
            GlState.Current!.SetActiveBuffer(_paintStateBuffer, 12);
            GlState.Current!.SetActiveBuffer(_sprayUniformsBuffer, 13);

            RenderSpray(ctx);
            RenderAccumulate(ctx);
            RenderDry(ctx);
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
                _tempDryTex.Recreate();
                _undoWetTex.Recreate();
                _undoDryTex.Recreate();
                _canvas!.ColorTexture.ToGlTexture().Recreate();
                _canvas!.RoughnessTexture.ToGlTexture().Recreate();
                _canvas!.NormalTexture.ToGlTexture().Recreate();
                _canvas!.SprayTexture.ToGlTexture().Recreate();
            }

            _dryTex.Update(data);
            _tempWetTex.Update(data);
            _wetTex.Update(data);
            _tempDryTex.Update(data);
            _undoDryTex.Update(data);
            _undoWetTex.Update(data);

            _canvas!.ColorTexture.ToGlTexture().Update(data);
            _canvas!.RoughnessTexture.ToGlTexture().Update(data);
            _canvas!.NormalTexture.ToGlTexture().Update(data);
            _canvas!.SprayTexture.ToGlTexture().Update(data);

            _sprayFrameBuffer.Configure(_canvas!.SprayTexture.ToGlTexture(), null, 1);

            _canvas.PaintTextures = [(Texture2D)_wetTex.ToEngineTexture(), (Texture2D)_dryTex.ToEngineTexture()];

            if (!_isFirstSizeUpdate)
                ClearCanvas();

            _isFirstSizeUpdate = false;

            //_readBuffer[0] ??= MemoryBuffer.Create<byte>(data.Width * data.Height * 2);

            _hasUndo = false;
        }

        protected void ClearCanvas()
        {
            CreateUndoEntry();

            _wetTex.Clear(Color.Transparent);
            _tempWetTex.Clear(Color.Transparent);
            
            _dryTex.Clear(Color.Transparent);
            _tempDryTex.Clear(Color.Transparent);
        }

        protected void RenderAccumulate(RenderContext ctx)
        {
            _canvas!.Update(ctx, ref _paintUniforms);

            _paintUniforms.ComputeSize = new Vector2I((int)_sprayRect.Width, (int)_sprayRect.Height);
            _paintUniforms.ComputeOffset = new Vector2I(_sprayRect.X, _sprayRect.Y);
            
            _paintUniformsBuffer.Update(_paintUniforms);

            if (_sprayRect.Width == 0 || _sprayRect.Height == 0)
                return;

            _accumulateProgram.Use();

            _accumulateProgram.SetUniform("uIncomingDensity", _canvas.SprayTexture, 0);

            _gl.BindImageTexture(1, _wetTex, 0, false, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(2, _dryTex, 0, false, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(3, _wetTex, 0, false, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(4, _dryTex, 0, false, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);

            _gl.DispatchCompute((_sprayRect.Width + 7) / 8, (_sprayRect.Height + 7) / 8, 1);

            _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
        }

        protected void RenderDry(RenderContext ctx)
        {
            _dryProgram.Use();

            _gl.BindImageTexture(1, _wetTex, 0, false, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(3, _wetTex, 0, false, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);

            _gl.DispatchCompute((_wetTex.Width + 7) / 8, (_wetTex.Height + 7) / 8, 1);

            _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
        }

        protected void RenderDrip(RenderContext ctx)
        {
            _dripProgram.Use();

            _gl.BindImageTexture(0, _wetTex, 0, false, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(1, _dryTex, 0, false, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);

            _gl.BindImageTexture(2, _tempWetTex, 0, false, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(3, _tempDryTex, 0, false, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);

            _gl.DispatchCompute((_wetTex.Width + 7) / 8, (_wetTex.Height + 7) / 8, 1);

            _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            (_tempWetTex, _wetTex) = (_wetTex, _tempWetTex);
            (_tempDryTex, _dryTex) = (_dryTex, _tempDryTex);
        }


        protected void RenderResolve(RenderContext ctx)
        {
            _resolveProgram.Use();

            _gl.BindImageTexture(0, _wetTex, 0, false, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(1, _dryTex, 0, false, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);

            _gl.BindImageTexture(3, _canvas!.ColorTexture.ToGlTexture(), 0, false, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(4, _canvas!.RoughnessTexture.ToGlTexture(), 0, false, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(5, _canvas!.NormalTexture.ToGlTexture(), 0, false, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);

            _gl.DispatchCompute((_wetTex.Width + 7) / 8, (_wetTex.Height + 7) / 8, 1);

            _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
        }

        protected void Undo()
        {
            if (!_hasUndo)
                return;

            _undoDryTex.CopyTo(_dryTex);
            _undoWetTex.CopyTo(_wetTex);

            _hasUndo = false;
        }

        protected void CreateUndoEntry()
        {
            _dryTex.CopyTo(_undoDryTex);
            _wetTex.CopyTo(_undoWetTex);
            _hasUndo = true;

            /*
            Stopwatch timer = new Stopwatch();
            timer.Start();
            _dryTex.Read(TextureFormat.RgbaFloat16, 0, null, _readBuffer);
            _wetTex.Read(TextureFormat.RgbaFloat16, 0, null, _readBuffer);
            timer.Stop();
            Log.Warn(this, "READ TIME: {0}ms", timer.Elapsed.TotalMilliseconds);
            */
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

            var isSpraying = _can!.SprayAperture > 0;

            var mustDraw = isSpraying && isInCanvas;

            var useFrameBuffer = mustDraw || !_isSprayClear;

            _tracker.Update(ref _sprayUniforms);

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

            if (!_spraySessionStarted && isSpraying)
            {
                CreateUndoEntry();
                _spraySessionStarted = true;
            }
            else if (!isSpraying)
                _spraySessionStarted = false;

            if (mustDraw)
            {
                var distance = (curCanvasTarget - _prevCanvasTarget).Length();

                var sampleCount = Math.Max(1, (int)MathF.Ceiling(distance / _canvas.SpraySpacing));
                sampleCount = Math.Min(sampleCount, SprayMaxSamples);

                _sprayProgram.Use();

                _brushSource!.Bind();

                _sprayUniforms.DensityScale = _sprayUniforms.DensityScale / sampleCount;

                _paintStateBuffer.Update(new PaintStateBuffer
                {
                    HasSprayFragments = 0,
                    SprayMaxX = 0,
                    SprayMaxY = 0,
                    SprayMinY = 100000,
                    SprayMinX = 100000,
                });

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

                _paintStateBuffer.Read(MapBufferAccessMask.ReadBit, ref _paintState);

                _brushSource.Unbind();

                _isSprayClear = false;

                _sprayRect = ComputeSprayTextureRect(_sprayUniforms,
                    _prevPose, curPose, _can.Transform.Scale, canvasQuod, new Size2I(_wetTex.Width, _wetTex.Height), 8);

                //_sprayRect = new Rect2I(0, 0, _wetTex.Width, _wetTex.Height);
            }   
            else
            {
                if (isSpraying)
                    _sprayUniformsBuffer.Update(_sprayUniforms);

                _sprayRect = new Rect2I();
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
