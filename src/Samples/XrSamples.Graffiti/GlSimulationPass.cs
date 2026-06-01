#if GLES
using Amazon.Runtime.Telemetry.Tracing;
using Silk.NET.OpenGLES;

#else
using Silk.NET.OpenGL;
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using XrEngine;
using XrEngine.OpenGL;
using XrMath;
using XrSamples.Graffiti.Shaders;
using static System.Net.Mime.MediaTypeNames;
using static XrEngine.Bullet.BulletLib;

namespace XrSamples.Graffiti
{
    public class SavedFrame
    {
        public TextureData Data;
        public long Frame; 
    }

    public class GlSimulationPass : GlBaseRenderPass
    {
        protected readonly GlTextureFrameBuffer _sprayFrameBuffer;
        protected PaintCanvas? _canvas;
        protected SprayBrush? _brush;
        protected readonly GlSimpleProgram _sprayProgram;
        protected GlBuffer<PaintProjUniforms> _sprayUniformsBuffer;
        protected PaintProjUniforms _sprayUniforms;
        protected Can? _can;
        protected SprayTracker? _tracker;
        protected GlVertexSourceHandle? _brushSource;

        protected readonly GlComputeProgram _accumulateProgram;
        protected readonly GlComputeProgram _dripProgram;
        protected readonly GlComputeProgram _resolveProgram;

        protected readonly GlTexture _paintA;
        protected readonly GlTexture _paintB;

        protected PaintSimulationBlock _paintUniforms;
        protected GlBuffer<PaintSimulationBlock> _paintUniformsBuffer;
        private Vector3 _prevNozzlePositon;
        private Pose3 _prevPose;
        private long _lastFrame;    

        protected IList<SavedFrame> _frames = [];

        public GlSimulationPass(OpenGLRender renderer)
            : base(renderer)    
        {
            _sprayFrameBuffer = new GlTextureFrameBuffer(_gl);

            _sprayProgram = new GlSimpleProgram(renderer.GL, "paint_proj.vert", "paint_proj.frag", str => Embedded.GetString<GlSimulationPass>(str));
            _sprayProgram.Build();

            var layers = "MAX_PAINT_LAYERS 8"; 

            _accumulateProgram = new GlComputeProgram(renderer.GL, "paint_accumulate.comp", str => Embedded.GetString<GlSimulationPass>(str));
            _accumulateProgram.AddFeature(layers);
            _accumulateProgram.Build();

            _dripProgram = new GlComputeProgram(renderer.GL, "paint_drip.comp", str => Embedded.GetString<GlSimulationPass>(str));
            _dripProgram.AddFeature(layers);
            _dripProgram.Build();

            _resolveProgram = new GlComputeProgram(renderer.GL, "paint_res.comp", str => Embedded.GetString<GlSimulationPass>(str));
            _resolveProgram.AddFeature(layers);
            _resolveProgram.Build();

            _sprayUniformsBuffer = new GlBuffer<PaintProjUniforms>(_gl, BufferTargetARB.UniformBuffer);
            _sprayUniforms = new PaintProjUniforms();

            _paintA = new GlTexture(_gl) {  Target = TextureTarget.Texture2DArray, MaxLevel = 0 };
            _paintB = new GlTexture(_gl) { Target = TextureTarget.Texture2DArray, MaxLevel = 0 };

            _paintUniformsBuffer = new GlBuffer<PaintSimulationBlock>(_gl, BufferTargetARB.UniformBuffer);
            _paintUniforms = new PaintSimulationBlock();
        }

        public override void Render(RenderContext ctx)
        {
            if (!_isInit)
                Intialize(ctx);

            if (ctx.DeltaTime == 0)
                return;

            if (ctx.Frame == _lastFrame)
                return;

            RenderSpray(ctx);
            RenderAccumulate(ctx);
            RenderDrip(ctx);
            RenderResolve(ctx);

            GlState.Current!.SetActiveProgram(0);

            _lastFrame = ctx.Frame; 
        }

        protected void Intialize(RenderContext ctx)
        {
            _canvas = ctx.Scene!.Descendants<PaintCanvas>().First();
            _brush = ctx.Scene!.Descendants<SprayBrush>().First();
            _can = ctx.Scene!.Descendants<Can>().First();
            _tracker = _can.Component<SprayTracker>();

            _brushSource = _brush.GetGlResource(a => GlVertexSourceHandle.Create(_gl, _brush));
            _brushSource.Update();

            var texture = _canvas.SprayTexture;
            var glText = texture.ToGlTexture();

            _sprayFrameBuffer.Configure(glText, null, 1);

            _isInit = true;

            var data = new TextureData
            {
                Format = TextureFormat.RgbaFloat16,
                Width = (uint)(_canvas.Size.X / _canvas.TexelSize),
                Height = (uint)(_canvas.Size.Y / _canvas.TexelSize),
                Depth = (uint)_canvas.Layers.Count
            };

            _paintA.Update((uint)_canvas.Layers.Count, data);
            _paintB.Update((uint)_canvas.Layers.Count, data);
        }



        protected void RenderAccumulate(RenderContext ctx)
        {
            _accumulateProgram.Use();

            _canvas!.Update(ctx, ref _paintUniforms);
            _paintUniformsBuffer.Update(_paintUniforms);

            _paintUniformsBuffer.Bind();
            _accumulateProgram.LoadBuffer(_paintUniformsBuffer, 3);

            _accumulateProgram.SetUniform("uIncomingSpray", _canvas.SprayTexture, 0);

            _gl.BindImageTexture(1, _paintA, 0, true, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(2, _paintB, 0, true, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);

            _gl.DispatchCompute((_paintA.Width + 7) / 8, (_paintA.Height + 7) / 8, 1);

            _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

        }

        protected void RenderDrip(RenderContext ctx)
        {
            _dripProgram.Use();
            _paintUniformsBuffer.Bind();
            _accumulateProgram.LoadBuffer(_paintUniformsBuffer, 3);

            _gl.BindImageTexture(1, _paintB, 0, true, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(2, _paintA, 0, true, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);

            _gl.DispatchCompute((_paintA.Width + 7) / 8, (_paintA.Height + 7) / 8, 1);

            _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

        }


        protected void RenderResolve(RenderContext ctx)
        {
            _resolveProgram.Use();
            _paintUniformsBuffer.Bind();
            _accumulateProgram.LoadBuffer(_paintUniformsBuffer, 3);

            _gl.BindImageTexture(0, _paintA, 0, true, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(1, _canvas!.ColorTexture.ToGlTexture(), 0, true, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(2, _canvas!.RoughnessTexture.ToGlTexture(), 0, true, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(3, _canvas!.NormalTexture.ToGlTexture(), 0, true, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);

            _gl.DispatchCompute((_paintA.Width + 7) / 8, (_paintA.Height + 7) / 8, 1);

            _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

        }

        protected void RenderSpray(RenderContext ctx)
        {
            _sprayFrameBuffer.Bind();

            GlState.Current!.SetView(new Rect2I(0, 0, _sprayFrameBuffer.Color!.Width, _sprayFrameBuffer.Color.Height));

            GlState.Current!.SetWriteDepth(false);
            GlState.Current!.SetUseDepth(false);
            GlState.Current!.SetWriteColor(true);
            GlState.Current!.SetAlphaMode(AlphaMode.Add);

            _gl.Clear(ClearBufferMask.ColorBufferBit);

            var curNozzlePosition = _tracker!.SprayCenter.Transform(_can!.WorldMatrix);
            var curPose = _can.GetWorldPose();

            if (_can!.SprayAperture > 0)
            {
                float distance = (curNozzlePosition - _prevNozzlePositon).Length();

                const float spacing = 0.01f;

                int sampleCount = Math.Max(1, (int)MathF.Ceiling(distance / spacing));
                sampleCount = Math.Min(sampleCount, 100);
               // sampleCount = 1;

                _sprayProgram.Use();
                _tracker.Update(ref _sprayUniforms);

                _sprayUniformsBuffer.Bind();
                _sprayProgram.LoadBuffer(_sprayUniformsBuffer);

                _brushSource!.Bind();

                _sprayUniforms.DensityScale = _sprayUniforms.DensityScale / sampleCount;

                if (sampleCount > 1)
                    Debug.WriteLine(sampleCount);

                for (int i = 0; i < sampleCount; ++i)
                {
                    float factor =
                        sampleCount == 1
                            ? 1.0f
                            : (i + 1.0f) / sampleCount;

                    var stepPose = _prevPose.Lerp(curPose, factor);

                    _sprayUniforms.HostLocalToWorld = stepPose.ToMatrix(_can.Transform.Scale);

                    _sprayUniformsBuffer.Update(_sprayUniforms);
                    _sprayProgram.LoadBuffer(_sprayUniformsBuffer);
                    _brushSource.Draw();
                }


                _brushSource.Unbind();
            }

            _prevNozzlePositon = curNozzlePosition;
            _prevPose = curPose;

            _gl.MemoryBarrier(MemoryBarrierMask.TextureFetchBarrierBit);

            /*
            if (_can!.SprayAperture > 0)
            {
                var data = _sprayFrameBuffer.ReadColor(TextureFormat.Rgba32);

                _frames.Add(new SavedFrame { Data = data, Frame = ctx.Frame,  });

                if (_frames.Count > 20)
                {
                    foreach (var frame in _frames)
                    {
                        using var bmp = ImageUtils.ToBitmap(frame.Data, false);
                        if (bmp == null)
                            return;
                        using var enc = bmp.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                        var path = "d:\\Frames\\" + frame.Frame + ".png";
                        if (File.Exists(path))
                            File.Delete(path);
                        using var file = File.OpenWrite(path);
                        enc.SaveTo(file);
                    }
                    _frames.Clear();
                }

            }
          */
            _sprayFrameBuffer.Unbind();


        }
    }
}
