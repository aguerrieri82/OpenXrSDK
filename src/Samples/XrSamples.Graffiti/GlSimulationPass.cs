#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
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

        protected readonly GlBuffer<PaintSimUniforms> _paintUniformsBuffer;
        protected PaintSimUniforms _paintUniforms;

        protected readonly GlBuffer<PaintProjUniforms> _sprayUniformsBuffer;
        protected PaintProjUniforms _sprayUniforms;

        protected Vector3 _prevNozzlePositon;
        protected Pose3 _prevPose;
        protected long _lastFrame;    

        public GlSimulationPass(OpenGLRender renderer)
            : base(renderer)    
        {
            _sprayFrameBuffer = new GlTextureFrameBuffer(_gl);

            _sprayProgram = new GlSimpleProgram(renderer.GL, "paint_proj.vert", "paint_proj.frag", str => Embedded.GetString<GlSimulationPass>(str));
            _sprayProgram.Build();

            _accumulateProgram = new GlComputeProgram(renderer.GL, "paint_accumulate.comp", str => Embedded.GetString<GlSimulationPass>(str));
            _accumulateProgram.Build();
       
            _dripProgram = new GlComputeProgram(renderer.GL, "paint_drip.comp", str => Embedded.GetString<GlSimulationPass>(str));
            _dripProgram.Build();

            _resolveProgram = new GlComputeProgram(renderer.GL, "paint_res.comp", str => Embedded.GetString<GlSimulationPass>(str));
            _resolveProgram.Build();

            _sprayUniformsBuffer = new GlBuffer<PaintProjUniforms>(_gl, BufferTargetARB.UniformBuffer);
            _sprayUniforms = new PaintProjUniforms();

            _wetTex = new GlTexture(_gl) { MaxLevel = 0 };
            _tempWetTex = new GlTexture(_gl) { MaxLevel = 0 };
            _dryTex = new GlTexture(_gl) { MaxLevel = 0 };

            _paintUniformsBuffer = new GlBuffer<PaintSimUniforms>(_gl, BufferTargetARB.UniformBuffer);
            _paintUniforms = new PaintSimUniforms();
        }

        public override void Render(RenderContext ctx)
        {
            if (!_isInit)
                Intialize(ctx);

            if (ctx.DeltaTime == 0)
                return;

            if (ctx.Frame == _lastFrame)
                return;

            if (_canvas!.ClearRequest)
            {
                _wetTex.Clear(Color.Transparent);
                _tempWetTex.Clear(Color.Transparent);
                _dryTex.Clear(Color.Transparent);

                _canvas.ClearRequest = false;
            }

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

            var glText = _canvas.SprayTexture.ToGlTexture();

            _sprayFrameBuffer.Configure(glText, null, 1);

            _isInit = true;

            var data = new TextureData
            {
                Format = TextureFormat.RgbaFloat16,
                Width = (uint)(_canvas.Size.X / _canvas.TexelSize),
                Height = (uint)(_canvas.Size.Y / _canvas.TexelSize),
                Depth = 1
            };

            _dryTex.Update(data.Depth, data);
            _tempWetTex.Update(data.Depth, data);
            _wetTex.Update(data.Depth, data);

            _canvas.PaintTextures = [(Texture2D)_wetTex.ToEngineTexture(), (Texture2D)_dryTex.ToEngineTexture()];
        }

        protected void RenderAccumulate(RenderContext ctx)
        {
            _accumulateProgram.Use();

            _canvas!.Update(ctx, ref _paintUniforms);
            _paintUniformsBuffer.Update(_paintUniforms);

            _paintUniformsBuffer.Bind();
            _accumulateProgram.LoadBuffer(_paintUniformsBuffer, 0);

            _accumulateProgram.SetUniform("uIncomingDensity", _canvas.SprayTexture, 0);

            _gl.BindImageTexture(1, _wetTex, 0, true, 0, BufferAccessARB.ReadWrite, InternalFormat.Rgba16f);
            _gl.BindImageTexture(2, _dryTex, 0, true, 0, BufferAccessARB.ReadWrite, InternalFormat.Rgba16f);

            _gl.DispatchCompute((_wetTex.Width + 7) / 8, (_wetTex.Height + 7) / 8, 1);

            _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

        }

        protected void RenderDrip(RenderContext ctx)
        {
            _dripProgram.Use();
            _paintUniformsBuffer.Bind();
            _accumulateProgram.LoadBuffer(_paintUniformsBuffer, 0);

            _gl.BindImageTexture(0, _wetTex, 0, true, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(1, _tempWetTex, 0, true, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);

            _gl.DispatchCompute((_wetTex.Width + 7) / 8, (_wetTex.Height + 7) / 8, 1);

            _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            var temp = _wetTex;   
            _wetTex = _tempWetTex;
            _tempWetTex = temp;
        }


        protected void RenderResolve(RenderContext ctx)
        {
            _resolveProgram.Use();
            _paintUniformsBuffer.Bind();
            _accumulateProgram.LoadBuffer(_paintUniformsBuffer, 0);

            _gl.BindImageTexture(0, _wetTex, 0, true, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(1, _dryTex, 0, true, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);

            _gl.BindImageTexture(2, _canvas!.ColorTexture.ToGlTexture(), 0, true, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(3, _canvas!.RoughnessTexture.ToGlTexture(), 0, true, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(4, _canvas!.NormalTexture.ToGlTexture(), 0, true, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);

            _gl.DispatchCompute((_wetTex.Width + 7) / 8, (_wetTex.Height + 7) / 8, 1);

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

            _sprayFrameBuffer.Unbind();
        }
    }
}
