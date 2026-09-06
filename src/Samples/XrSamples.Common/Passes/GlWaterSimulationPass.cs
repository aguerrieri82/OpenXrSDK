#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using XrEngine;
using XrEngine.OpenGL;
using XrMath;

namespace XrSamples
{
    public sealed class GlWaterSimulationPass : GlBaseRenderPass
    {
        private readonly WaterMaterial _material;
        private readonly TriangleMesh _water;
        private readonly Object3D _player;
        private readonly GlComputeProgram _program;
        private GlTexture? _state;
        private long _lastFrame;
        private int _readLayer;
        private Vector2 _lastPlayerPosition;
        private bool _hasPlayerPosition;
        private bool _resetRequested;

        public GlWaterSimulationPass(OpenGLRender renderer, WaterMaterial material, TriangleMesh water, Object3D player)
            : base(renderer)
        {
            _material = material;
            _water = water;
            _player = player;
            _lastFrame = -1;
            WaveSpeed = 300f;
            Damping = 0.995f;
            RippleStrength = 0.7f;
            PlayerDisturbanceRadius = 0.16f;
            PlayerDisturbanceStrength = 7f;
            WakeDetailGeneration = 0.35f;
            WakeDetailPersistence = 0.9995f;

            _program = new GlComputeProgram(renderer.GL, "Water/water_sim.comp", Embedded.GetString<GlWaterSimulationPass>);
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

            if (_resetRequested)
                ResetState();

            var writeLayer = 1 - _readLayer;
            var deltaTime = MathF.Min((float)ctx.DeltaTime, 1f / 60f);

            _program.Use();
            _program.SetUniform("uReadLayer", _readLayer);
            _program.SetUniform("uWriteLayer", writeLayer);
            _program.SetUniform("uDeltaTime", deltaTime);
            _program.SetUniform("uTime", ctx.Time);
            _program.SetUniform("uWaveSpeed", WaveSpeed);
            _program.SetUniform("uDamping", Damping);
            _program.SetUniform("uRippleStrength", RippleStrength);
            _program.SetUniform("uWakeDetailGeneration", WakeDetailGeneration);
            _program.SetUniform("uWakeDetailPersistence", WakeDetailPersistence);
            SetPlayerUniforms(deltaTime);

            _gl.BindImageTexture(0, _state, 0, true, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(1, _state, 0, true, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);

            _gl.DispatchCompute((_material.StateTexture.Width + 7) / 8, (_material.StateTexture.Height + 7) / 8, 1);
            _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit | MemoryBarrierMask.TextureFetchBarrierBit);

            _readLayer = writeLayer;
            _material.CurrentLayer = _readLayer;
            GlState.Current.SetActiveProgram(0);
            _lastFrame = ctx.Frame;
        }

        private void SetPlayerUniforms(float deltaTime)
        {
            var local = _water.ToLocal(_player.WorldPosition);
            var position = new Vector2(local.X, local.Y);
            var size = Vector2.Max(_material.WaterSize, new Vector2(0.001f));
            var uv = position / size + new Vector2(0.5f);
            var delta = position - _lastPlayerPosition;
            var distance = delta.Length();
            var isInside = uv.X >= 0 && uv.X <= 1 && uv.Y >= 0 && uv.Y <= 1;
            var isTeleport = _hasPlayerPosition && distance >= 0.5f;
            var isWalking = _hasPlayerPosition && !isTeleport && distance / deltaTime >= 0.12f;
            var motion = isWalking ? Math.Clamp(distance / deltaTime / 1.2f, 0f, 1f) : 0f;

            _program.SetUniform("uPlayerDisturbanceUv", uv);
            _program.SetUniform("uPlayerDisturbanceRadiusUv", new Vector2(PlayerDisturbanceRadius) / size);
            _program.SetUniform("uPlayerDisturbanceMotion", isInside ? motion : 0f);
            _program.SetUniform("uPlayerDisturbanceStrength", PlayerDisturbanceStrength);

            _lastPlayerPosition = position;
            _hasPlayerPosition = true;
        }

        public void RequestReset()
        {
            _resetRequested = true;
        }

        private void ResetState()
        {
            _state!.Clear(Color.Transparent);
            _readLayer = 0;
            _material.CurrentLayer = 0;
            _hasPlayerPosition = false;
            _resetRequested = false;
        }

        public override void Dispose()
        {
            _program.Dispose();
            base.Dispose();
        }

        public float WaveSpeed { get; set; }

        public float Damping { get; set; }

        public float RippleStrength { get; set; }

        public float PlayerDisturbanceRadius { get; set; }

        public float PlayerDisturbanceStrength { get; set; }

        public float WakeDetailGeneration { get; set; }

        public float WakeDetailPersistence { get; set; }
    }
}
