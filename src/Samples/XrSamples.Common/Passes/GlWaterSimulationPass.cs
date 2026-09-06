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
        private float _playerDistance;
        private float _playerIdleTime;
        private bool _hasPlayerPosition;
        private bool _playerWasMoving;
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
            PlayerRadius = 0.16f;
            PlayerStrength = 0.45f;
            PlayerStepDistance = 0.52f;
            HighFrequencyStrength = 0.35f;
            HighFrequencyDamping = 0.9995f;

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
            _program.SetUniform("uHighFrequencyStrength", HighFrequencyStrength);
            _program.SetUniform("uHighFrequencyDamping", HighFrequencyDamping);
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
            var direction = isWalking ? delta / distance : Vector2.Zero;
            var impulse = 0f;

            if (isWalking)
            {
                _playerIdleTime = 0;
                _playerDistance += distance;

                var triggerDistance = _playerWasMoving ? PlayerStepDistance : 0.08f;

                if (_playerDistance >= triggerDistance)
                {
                    impulse = 1f;
                    _playerDistance = 0;
                    _playerWasMoving = true;
                }
            }
            else
            {
                _playerIdleTime += deltaTime;

                if (_playerIdleTime >= 0.2f || isTeleport)
                {
                    _playerDistance = 0;
                    _playerWasMoving = false;
                }
            }

            _program.SetUniform("uPlayerUv", uv);
            _program.SetUniform("uPlayerDirection", direction);
            _program.SetUniform("uPlayerRadiusUv", new Vector2(PlayerRadius) / size);
            _program.SetUniform("uPlayerImpulse", isInside ? impulse : 0f);
            _program.SetUniform("uPlayerStrength", PlayerStrength);

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
            _playerDistance = 0;
            _playerIdleTime = 0;
            _hasPlayerPosition = false;
            _playerWasMoving = false;
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

        public float PlayerRadius { get; set; }

        public float PlayerStrength { get; set; }

        public float PlayerStepDistance { get; set; }

        public float HighFrequencyStrength { get; set; }

        public float HighFrequencyDamping { get; set; }
    }
}
