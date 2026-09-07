#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using XrEngine;
using XrEngine.OpenGL;
using XrMath;

namespace XrSamples
{
    public sealed class GlWaterSimulationPass : GlBaseRenderPass
    {
        private const int SimulationBufferSlot = 0;

        [InlineArray(4)]
        private struct ContactArray
        {
            private Vector4 _element0;
        }

        // Matches std140 WaterSimulation in water_sim.comp (224 bytes).
        [StructLayout(LayoutKind.Explicit, Size = 224)]
        private struct SimulationUniforms
        {
            [FieldOffset(0)]
            public int ReadLayer;

            [FieldOffset(4)]
            public int WriteLayer;

            [FieldOffset(8)]
            public int Initialize;

            [FieldOffset(12)]
            public float DeltaTime;

            [FieldOffset(16)]
            public float Time;

            [FieldOffset(20)]
            public float SurfaceWaveSpeed;

            [FieldOffset(24)]
            public float SurfaceDecay;

            [FieldOffset(28)]
            public float BackgroundStrength;

            [FieldOffset(32)]
            public float FineRippleExcitation;

            [FieldOffset(36)]
            public float FineRippleDecay;

            [FieldOffset(40)]
            public float SurfaceHeightScale;

            [FieldOffset(44)]
            public float BackgroundSpacing;

            [FieldOffset(48)]
            public float FineRippleSpacing;

            [FieldOffset(52)]
            public float FineRippleExcitationRate;

            [FieldOffset(56)]
            public float PlayerMotion;

            [FieldOffset(60)]
            public float PlayerStrength;

            [FieldOffset(64)]
            public Vector2 WaterSize;

            [FieldOffset(72)]
            public Vector2 LaplacianWeights;

            [FieldOffset(80)]
            public Vector2 PlayerUv;

            [FieldOffset(88)]
            public Vector2 PlayerRadiusUv;

            [FieldOffset(96)]
            public ContactArray Contacts;

            [FieldOffset(160)]
            public ContactArray PreviousContacts;
        }

        private readonly GlBuffer<SimulationUniforms> _uniforms;
        private SimulationUniforms _uniformData;
        private readonly WaterMaterial _material;
        private readonly Water _water;
        private readonly GlComputeProgram _program;
        private GlTexture? _state;
        private long _lastFrame;
        private int _readLayer;
        private bool _resetRequested;
        private bool _initialize = true;

        public GlWaterSimulationPass(OpenGLRender renderer, Water water)
            : base(renderer)
        {
            _water = water;
            _material = water.Material;
            water.Simulation = this;
            _lastFrame = -1;

            _program = new GlComputeProgram(renderer.GL, "Water/water_sim.comp", Embedded.GetString<GlWaterSimulationPass>);
            _program.Build();
            _uniforms = new GlBuffer<SimulationUniforms>(_gl, BufferTargetARB.UniformBuffer);
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
            var deltaTime = MathF.Min(ctx.DeltaTime, 1f / 60f);

            var cellSize = Vector2.Max(_material.WaterSize /
                new Vector2(_material.StateTexture.Width, _material.StateTexture.Height), new Vector2(0.0001f));

            var weights = new Vector2(MathF.Min(cellSize.X, cellSize.Y)) / cellSize;

            _uniformData.ReadLayer = _readLayer;
            _uniformData.WriteLayer = writeLayer;
            _uniformData.Initialize = _initialize ? 1 : 0;
            _uniformData.DeltaTime = deltaTime;
            _uniformData.Time = ctx.Time;
            _uniformData.SurfaceWaveSpeed = _water.SurfaceWaveSpeed;
            _uniformData.BackgroundStrength = _water.BackgroundStrength;
            _uniformData.FineRippleExcitation = _water.FineRippleExcitation;
            _uniformData.WaterSize = _material.WaterSize;
            _uniformData.SurfaceHeightScale = _material.SurfaceHeightScale;
            _uniformData.BackgroundSpacing = MathF.Max(_water.BackgroundSpacing, 2f * MathF.Max(cellSize.X, cellSize.Y));
            _uniformData.FineRippleSpacing = MathF.Max(_water.FineRippleSpacing, 2f * MathF.Max(cellSize.X, cellSize.Y));
            _uniformData.SurfaceDecay = MathF.Pow(_water.SurfacePersistence, deltaTime * 60f);
            _uniformData.FineRippleDecay = MathF.Pow(MathF.Min(_water.FineRipplePersistence, 0.9999f), deltaTime * 60f);
            _uniformData.LaplacianWeights = weights * weights;
            _uniformData.FineRippleExcitationRate = _water.FineRippleExcitationRate;

            var interaction = _water.Interaction;
            var state = interaction.State;
            var size = Vector2.Max(_material.WaterSize, new Vector2(0.001f));

            _uniformData.PlayerUv = state.PlayerLocalPosition / size + new Vector2(0.5f);
            _uniformData.PlayerRadiusUv = new Vector2(interaction.PlayerDisturbanceRadius) / size;
            _uniformData.PlayerMotion = state.PlayerMotion;
            _uniformData.PlayerStrength = interaction.PlayerDisturbanceStrength;

            var contacts = state.Contacts;

            for (var i = 0; i < contacts.Length; i++)
            {
                ref readonly var contact = ref contacts[i];
                _uniformData.Contacts[i] = new Vector4(contact.LocalPosition, contact.Radius);
                _uniformData.PreviousContacts[i] = new Vector4(contact.PreviousLocalPosition, contact.Speed);
            }

            _uniforms.Update(_uniformData);
            _renderer.State.LoadBuffer(_uniforms, SimulationBufferSlot);
            _program.Use();

            _gl.BindImageTexture(0, _state, 0, true, 0, BufferAccessARB.ReadOnly, InternalFormat.Rgba16f);
            _gl.BindImageTexture(1, _state, 0, true, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba16f);

            _gl.DispatchCompute((_material.StateTexture.Width + 7) / 8, (_material.StateTexture.Height + 7) / 8, 1);
            _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit | MemoryBarrierMask.TextureFetchBarrierBit);

            _readLayer = writeLayer;
            _material.CurrentLayer = _readLayer;
            _renderer.State.SetActiveProgram(0);
            _lastFrame = ctx.Frame;
            _initialize = false;
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
            _initialize = true;
            _resetRequested = false;
        }

        public override void Dispose()
        {
            _program.Dispose();
            _uniforms.Dispose();
            _water.Simulation = null;
            base.Dispose();
        }

    }
}
