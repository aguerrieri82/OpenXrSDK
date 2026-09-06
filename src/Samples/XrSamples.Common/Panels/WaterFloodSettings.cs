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
            ImpactStrength = 0.7f;
            SurfaceDisplacement = 4f;
            Roughness = 0.08f;
            Ior = 1.5f;
            Transmission = 1f;
            RestartVersion = 0;
        }

        public void AttachSimulation(GlWaterSimulationPass simulation)
        {
            _simulation = simulation;
            Apply();
        }

        public void Apply()
        {
            _material.HeightScale = SurfaceDisplacement;
            _material.Roughness = Roughness;
            _material.Ior = Ior;
            _material.Transmission = Transmission;

            if (_simulation == null)
                return;

            _simulation.WaveSpeed = WaveSpeed;
            _simulation.Damping = Damping;
            _simulation.RippleStrength = ImpactStrength;
        }

        public void RestartFlood()
        {
            RestartVersion++;
        }

        public float MaximumDepth { get; set; }

        public float RiseSpeed { get; set; }

        public bool PauseFlood { get; set; }

        public float WaveSpeed { get; set; }

        public float Damping { get; set; }

        public float ImpactStrength { get; set; }

        public float SurfaceDisplacement { get; set; }

        public float Roughness { get; set; }

        public float Ior { get; set; }

        public float Transmission { get; set; }

        public int RestartVersion { get; private set; }
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
                .BeginColumn(s => s.RowGap(10))
                    .AddInputRange("Maximum depth (m)", 0.05f, 1.5f, binder.Prop(a => a.MaximumDepth))
                    .AddInputRange("Rise speed (m/s)", 0f, 0.1f, binder.Prop(a => a.RiseSpeed))
                    .AddInputRange("Wave speed", 20f, 600f, binder.Prop(a => a.WaveSpeed))
                    .AddInputRange("Damping", 0.96f, 0.9999f, binder.Prop(a => a.Damping))
                    .AddInputRange("Impact strength", 0f, 2f, binder.Prop(a => a.ImpactStrength))
                    .AddInputRange("Surface displacement", 0f, 4f, binder.Prop(a => a.SurfaceDisplacement))
                    .AddInputRange("Roughness", 0.01f, 0.5f, binder.Prop(a => a.Roughness))
                    .AddInputRange("IOR", 1f, 1.7f, binder.Prop(a => a.Ior))
                    .AddInputRange("Transmission", 0f, 1f, binder.Prop(a => a.Transmission))
                    .AddInput("Pause flood", new CheckBox(), binder.Prop(a => a.PauseFlood))
                    .AddButton("Restart flood", settings.RestartFlood)
                .EndChild();
        }
    }
}
