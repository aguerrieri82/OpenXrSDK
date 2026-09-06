#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using XrEngine;
using XrEngine.OpenGL;
using XrMath;

namespace XrSamples
{
    public sealed class GlWaterSimulationPass : GlBaseRenderPass
    {
        private readonly WaterMaterial _material;
        private readonly GlComputeProgram _program;
        private GlTexture? _state;
        private long _lastFrame;
        private int _readLayer;

        public GlWaterSimulationPass(OpenGLRender renderer, WaterMaterial material)
            : base(renderer)
        {
            _material = material;
            _lastFrame = -1;
            WaveSpeed = 300f;
            Damping = 0.995f;
            RippleStrength = 0.7f;

            _program = new GlComputeProgram(renderer.GL, "Water/water_sim.comp", 
                Embedded.GetString<GlWaterSimulationPass>);
            _program.Build();
        }

        public override void Render(GlUpdateContext ctx)
        {
            if (ctx.Frame == _lastFrame || ctx.DeltaTime <= 0)
                return;

            if (_state == null)
            {
                _state = _material.StateTexture.ToGlTexture();
                _state.Clear(Color.Transparent);
            }

            var writeLayer = 1 - _readLayer;
            var deltaTime = MathF.Min((float)ctx.DeltaTime, 1f / 30f);

            _program.Use();
            _program.SetUniform("uReadLayer", _readLayer);
            _program.SetUniform("uWriteLayer", writeLayer);
            _program.SetUniform("uDeltaTime", deltaTime);
            _program.SetUniform("uTime", ctx.Time);
            _program.SetUniform("uWaveSpeed", WaveSpeed);
            _program.SetUniform("uDamping", Damping);
            _program.SetUniform("uRippleStrength", RippleStrength);

            _gl.BindImageTexture(0, _state, 0, true, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(1, _state, 0, true, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);

            _gl.DispatchCompute((_material.StateTexture.Width + 7) / 8, (_material.StateTexture.Height + 7) / 8, 1);
            _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit | MemoryBarrierMask.TextureFetchBarrierBit);

            _readLayer = writeLayer;
            _material.CurrentLayer = _readLayer;
            GlState.Current.SetActiveProgram(0);
            _lastFrame = ctx.Frame;
        }

        public override void Dispose()
        {
            _program.Dispose();
            base.Dispose();
        }

        public float WaveSpeed { get; set; }

        public float Damping { get; set; }

        public float RippleStrength { get; set; }
    }
}
