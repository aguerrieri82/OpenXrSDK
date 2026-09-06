using CanvasUI;
using UI.Binding;
using CheckBox = CanvasUI.CheckBox;

namespace XrSamples
{
    public sealed class WaterFloodSettings
    {
        private readonly WaterMaterial _material;
        private GlWaterSimulationPass? _simulation;

        public WaterFloodSettings(WaterMaterial material)
        {
            _material = material;
            MaximumDepth = 0.55f;
            RiseSpeed = 0.0125f;
            PauseFlood = false;
            WaveSpeed = 300f;
            Damping = 0.995f;
            ImpactStrength = 0f;
            RippleScale = 1f;
            AmbientWaveHeight = 0.012f;
            AmbientWaveScale = 1f;
            PlayerDisturbanceRadius = 0.16f;
            PlayerDisturbanceStrength = 5f;
            WakeDetailStrength = 2.25f;
            WakeDetailGeneration = 0.35f;
            WakeDetailPersistence = 0.9995f;
            Roughness = 0.08f;
            Ior = 1.5f;
            Transmission = 1f;
        }

        public void AttachSimulation(GlWaterSimulationPass simulation)
        {
            _simulation = simulation;
            Apply();
        }

        public void Apply()
        {
            _material.HeightScale = RippleScale;
            _material.AmbientWaveHeight = AmbientWaveHeight;
            _material.AmbientWaveScale = AmbientWaveScale;
            _material.WakeDetailStrength = WakeDetailStrength;
            _material.Roughness = Roughness;
            _material.Ior = Ior;
            _material.Transmission = Transmission;

            if (_simulation != null)
            {
                _simulation.WaveSpeed = WaveSpeed;
                _simulation.Damping = Damping;
                _simulation.RippleStrength = ImpactStrength;
                _simulation.PlayerDisturbanceRadius = PlayerDisturbanceRadius;
                _simulation.PlayerDisturbanceStrength = PlayerDisturbanceStrength;
                _simulation.WakeDetailGeneration = WakeDetailGeneration;
                _simulation.WakeDetailPersistence = WakeDetailPersistence;
            }

            _material.Invalidate();
        }

        public void ResetSimulation()
        {
            _simulation?.RequestReset();
        }

        public float MaximumDepth { get; set; }

        public float RiseSpeed { get; set; }

        public bool PauseFlood { get; set; }

        public float WaveSpeed { get; set; }

        public float Damping { get; set; }

        public float ImpactStrength { get; set; }

        public float RippleScale { get; set; }

        public float AmbientWaveHeight { get; set; }

        public float AmbientWaveScale { get; set; }

        public float PlayerDisturbanceRadius { get; set; }

        public float PlayerDisturbanceStrength { get; set; }

        public float WakeDetailStrength { get; set; }

        public float WakeDetailGeneration { get; set; }

        public float WakeDetailPersistence { get; set; }

        public float Roughness { get; set; }

        public float Ior { get; set; }

        public float Transmission { get; set; }

    }

    public sealed class WaterFloodSettingsPanel : UIRoot
    {
        public WaterFloodSettingsPanel(WaterFloodSettings settings)
        {
            var binder = new Binder<WaterFloodSettings>(settings);
            binder.PropertyChanged += (_, _, _, _) => settings.Apply();

            UiBuilder.From(this).Name("Water flood settings").AsColumn()
                .Style(s => s
                    .Padding(16)
                    .RowGap(10)
                    .Color("#F5F5F5")
                    .BackgroundColor("#050505D8"))
                .BeginRow(s => s.ColGap(16).FlexGrow(1))
                .BeginColumn(s => s.FlexBasis(1).RowGap(10))
                    .AddText("Simulation", s => s.FontSize(1.25f, Unit.Em))
                    .AddInputRange("Maximum depth (m)", 0.05f, 1.5f, binder.Prop(a => a.MaximumDepth))
                    .AddInputRange("Rise speed (m/s)", 0f, 0.1f, binder.Prop(a => a.RiseSpeed))
                    .AddInputRange("Wave speed", 20f, 600f, binder.Prop(a => a.WaveSpeed))
                    .AddInputRange("Damping", 0.96f, 0.9999f, binder.Prop(a => a.Damping))
                    .AddInputRange("Impact strength", 0f, 2f, binder.Prop(a => a.ImpactStrength))
                    .AddInputRange("Ripple scale", 0f, 2f, binder.Prop(a => a.RippleScale))
                    .AddInput("Pause flood", new CheckBox(), binder.Prop(a => a.PauseFlood))
                .EndChild()
                .BeginColumn(s => s.FlexBasis(1).RowGap(10))
                    .AddText("Surface and player", s => s.FontSize(1.25f, Unit.Em))
                    .AddInputRange("Ambient wave height (m)", 0f, 0.05f, binder.Prop(a => a.AmbientWaveHeight))
                    .AddInputRange("Ambient wave scale", 0.5f, 2f, binder.Prop(a => a.AmbientWaveScale))
                    .AddInputRange("Player disturbance radius (m)", 0.05f, 0.4f, binder.Prop(a => a.PlayerDisturbanceRadius))
                    .AddInputRange("Player disturbance strength", 0f, 30f, binder.Prop(a => a.PlayerDisturbanceStrength))
                    .AddInputRange("Wake detail strength", 0f, 5f, binder.Prop(a => a.WakeDetailStrength))
                    .AddInputRange("Wake detail generation", 0f, 2f, binder.Prop(a => a.WakeDetailGeneration))
                    .AddInputRange("Wake detail persistence", 0.98f, 1f, binder.Prop(a => a.WakeDetailPersistence))
                    .AddInputRange("Roughness", 0f, 0.5f, binder.Prop(a => a.Roughness))
                    .AddInputRange("IOR", 1f, 1.7f, binder.Prop(a => a.Ior))
                    .AddInputRange("Transmission", 0f, 1f, binder.Prop(a => a.Transmission))
                    .AddButton("Reset simulation", settings.ResetSimulation)
                .EndChild()
                .EndChild();
        }
    }
}
