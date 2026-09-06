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
            ImpactStrength = 0.3f;
            RippleScale = 1f;
            BaseWaveHeight = 0.012f;
            MicroWaveStrength = 2.25f;
            PlayerRadius = 0.16f;
            PlayerStrength = 0.45f;
            PlayerStepDistance = 0.52f;
            HighFrequencyStrength = 0.35f;
            HighFrequencyDamping = 0.9995f;
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
            _material.BaseWaveHeight = BaseWaveHeight;
            _material.MicroWaveStrength = MicroWaveStrength;
            _material.Roughness = Roughness;
            _material.Ior = Ior;
            _material.Transmission = Transmission;

            if (_simulation != null)
            {
                _simulation.WaveSpeed = WaveSpeed;
                _simulation.Damping = Damping;
                _simulation.RippleStrength = ImpactStrength;
                _simulation.PlayerRadius = PlayerRadius;
                _simulation.PlayerStrength = PlayerStrength;
                _simulation.PlayerStepDistance = PlayerStepDistance;
                _simulation.HighFrequencyStrength = HighFrequencyStrength;
                _simulation.HighFrequencyDamping = HighFrequencyDamping;
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

        public float BaseWaveHeight { get; set; }

        public float MicroWaveStrength { get; set; }

        public float PlayerRadius { get; set; }

        public float PlayerStrength { get; set; }

        public float PlayerStepDistance { get; set; }

        public float HighFrequencyStrength { get; set; }

        public float HighFrequencyDamping { get; set; }

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
                    .AddInputRange("Base wave height (m)", 0f, 0.05f, binder.Prop(a => a.BaseWaveHeight))
                    .AddInputRange("Micro-wave strength", 0f, 5f, binder.Prop(a => a.MicroWaveStrength))
                    .AddInputRange("Player radius (m)", 0.05f, 0.4f, binder.Prop(a => a.PlayerRadius))
                    .AddInputRange("Player impulse (m/s)", 0f, 2f, binder.Prop(a => a.PlayerStrength))
                    .AddInputRange("Player step distance (m)", 0.2f, 1f, binder.Prop(a => a.PlayerStepDistance))
                    .AddInputRange("High-frequency strength", 0f, 2f, binder.Prop(a => a.HighFrequencyStrength))
                    .AddInputRange("High-frequency damping", 0.98f, 0.9999f, binder.Prop(a => a.HighFrequencyDamping))
                    .AddInputRange("Roughness", 0f, 0.5f, binder.Prop(a => a.Roughness))
                    .AddInputRange("IOR", 1f, 1.7f, binder.Prop(a => a.Ior))
                    .AddInputRange("Transmission", 0f, 1f, binder.Prop(a => a.Transmission))
                    .AddButton("Reset simulation", settings.ResetSimulation)
                .EndChild()
                .EndChild();
        }
    }
}
