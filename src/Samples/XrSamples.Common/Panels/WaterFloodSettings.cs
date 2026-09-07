using CanvasUI;
using UI.Binding;
using XrEngine;
using CheckBox = CanvasUI.CheckBox;

namespace XrSamples
{
    public sealed class WaterFloodSettings : BaseAppSettings<Water>
    {
        public WaterFloodSettings()
        {
            MaximumDepth = 0.55f;
            RiseSpeed = 0.012f;
            PauseFlood = false;
            SurfaceWaveSpeed = 300f;
            SurfacePersistence = 0.995f;
            BackgroundStrength = 0f;
            SurfaceHeightScale = 1f;
            BackgroundSpacing = 0.065f;
            FineRippleSpacing = 0.008f;
            FineRippleExcitationRate = 9.966f;
            PlayerDisturbanceRadius = 0.16f;
            PlayerDisturbanceStrength = 5f;
            WalkingThreshold = 0.15f;
            FineRippleNormalStrength = 0.277f;
            FineRippleExcitation = 0.143f;
            FineRipplePersistence = 0.98f;
            Roughness = 0.08f;
            Ior = 1.5f;
            Transmission = 1f;
        }

        public override void Apply(Water water)
        {
            var material = water.Material;
            var interaction = water.Interaction;

            material.SurfaceHeightScale = SurfaceHeightScale;
            material.FineRippleNormalStrength = FineRippleNormalStrength;
            material.Roughness = Roughness;
            material.Ior = Ior;
            material.Transmission = Transmission;

            water.SurfaceWaveSpeed = SurfaceWaveSpeed;
            water.SurfacePersistence = SurfacePersistence;
            water.BackgroundStrength = BackgroundStrength;
            water.BackgroundSpacing = BackgroundSpacing;
            water.FineRippleSpacing = FineRippleSpacing;
            water.FineRippleExcitationRate = FineRippleExcitationRate;
            water.FineRippleExcitation = FineRippleExcitation;
            water.FineRipplePersistence = FineRipplePersistence;

            interaction.PlayerDisturbanceRadius = PlayerDisturbanceRadius;
            interaction.PlayerDisturbanceStrength = PlayerDisturbanceStrength;
            interaction.WalkingThreshold = WalkingThreshold;

            material.Invalidate();
        }

        public float MaximumDepth { get; set; }

        public float RiseSpeed { get; set; }

        public bool PauseFlood { get; set; }

        public float SurfaceWaveSpeed { get; set; }

        public float SurfacePersistence { get; set; }

        public float BackgroundStrength { get; set; }

        public float SurfaceHeightScale { get; set; }

        public float BackgroundSpacing { get; set; }

        public float FineRippleSpacing { get; set; }

        public float FineRippleExcitationRate { get; set; }

        public float PlayerDisturbanceRadius { get; set; }

        public float PlayerDisturbanceStrength { get; set; }

        public float WalkingThreshold { get; set; }

        public float FineRippleNormalStrength { get; set; }

        public float FineRippleExcitation { get; set; }

        public float FineRipplePersistence { get; set; }

        public float Roughness { get; set; }

        public float Ior { get; set; }

        public float Transmission { get; set; }

    }

    public sealed class WaterFloodSettingsPanel : UIRoot
    {
        public WaterFloodSettingsPanel(WaterFloodSettings settings, Water water)
        {
            var binder = new Binder<WaterFloodSettings>(settings);
            binder.PropertyChanged += (_, _, _, _) => settings.Apply(water);

            UiBuilder.From(this).Name("Water flood settings").AsColumn()
                .Style(s => s
                    .Padding(16)
                    .RowGap(10)
                    .Color("#F5F5F5")
                    .BackgroundColor("#050505D8"))
                .BeginRow(s => s.ColGap(16).FlexGrow(1))
                .BeginColumn(s => s.FlexBasis(1).RowGap(10))
                    .AddText("Flood and background", s => s.FontSize(1.25f, Unit.Em))
                    .AddInputRange("Maximum depth (m)", 0.05f, 1.5f, binder.Prop(a => a.MaximumDepth))
                    .AddInputRange("Rise speed (m/s)", 0f, 0.1f, binder.Prop(a => a.RiseSpeed))
                    .AddInputRange("Surface wave speed", 20f, 600f, binder.Prop(a => a.SurfaceWaveSpeed))
                    .AddInputRange("Surface wave persistence", 0.96f, 0.9999f, binder.Prop(a => a.SurfacePersistence))
                    .AddInputRange("Background strength", 0f, 2f, binder.Prop(a => a.BackgroundStrength))
                    .AddInputRange("Surface height scale", 0f, 2f, binder.Prop(a => a.SurfaceHeightScale))
                    .AddInputRange("Background spacing (m)", 0.025f, 0.25f, binder.Prop(a => a.BackgroundSpacing))
                    .AddInput("Pause flood", new CheckBox(), binder.Prop(a => a.PauseFlood))
                .EndChild()
                .BeginColumn(s => s.FlexBasis(1).RowGap(10))
                    .AddText("Fine ripples", s => s.FontSize(1.25f, Unit.Em))
                    .AddInputRange("Spacing (m)", 0.008f, 0.08f, binder.Prop(a => a.FineRippleSpacing))
                    .AddInputRange("Excitation rate (Hz)", 2f, 20f, binder.Prop(a => a.FineRippleExcitationRate))
                    .AddInputRange("Normal strength", 0f, 5f, binder.Prop(a => a.FineRippleNormalStrength))
                    .AddInputRange("Excitation", 0f, 2f, binder.Prop(a => a.FineRippleExcitation))
                    .AddInputRange("Persistence", 0.98f, 1f, binder.Prop(a => a.FineRipplePersistence))
                    .AddText("Interaction and appearance", s => s
                            .Margin(top: 16)
                            .FontSize(1.25f, Unit.Em))
                    .AddInputRange("Walking threshold (m)", 0.02f, 0.4f, binder.Prop(a => a.WalkingThreshold))
                    .AddInputRange("Player disturbance radius (m)", 0.05f, 0.4f, binder.Prop(a => a.PlayerDisturbanceRadius))
                    .AddInputRange("Player disturbance strength", 0f, 30f, binder.Prop(a => a.PlayerDisturbanceStrength))
                    .AddInputRange("Roughness", 0f, 0.5f, binder.Prop(a => a.Roughness))
                    .AddInputRange("IOR", 1f, 1.7f, binder.Prop(a => a.Ior))
                    .AddInputRange("Transmission", 0f, 1f, binder.Prop(a => a.Transmission))
                .EndChild()
                .EndChild()
                .BeginRow(s => s.JustifyContent(UiAlignment.End).ColGap(10))
                    .AddButton("Save parameters", settings.Save, s => s.Padding(8, 16).BackgroundColor("#1565C0"))
                    .AddButton("Reset simulation", water.ResetSimulation, s => s.Padding(8, 16).BackgroundColor("#1565C0"))
                .EndChild();
        }
    }
}
