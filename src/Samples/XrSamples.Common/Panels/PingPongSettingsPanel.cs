using CanvasUI;
using CanvasUI.Objects;
using System.Reflection;
using System.Text.Json;
using XrEngine;


namespace XrSamples
{
    public class PhysicSettings
    {
        public float ContactDistance { get; set; }

        public float Restitution { get; set; }

        public float LengthScale { get; set; }

        public bool UseCCD { get; set; }
    }

    public class PingPongSettings : BaseAppSettings
    {

        public PingPongSettings()
        {
            Ball = new PhysicSettings();
            Racket = new PhysicSettings();
            Terrain = new PhysicSettings();

            LengthScale = 1;
            UsePcm = true;

            Ball.LengthScale = 1;
            Ball.ContactDistance = 0.2f;
            Ball.Restitution = 0.8f;
            Ball.UseCCD = true;

            Racket.LengthScale = 1;
            Racket.ContactDistance = 0.2f;
            Racket.Restitution = 0.8f;
            Racket.UseCCD = true;

            Terrain.LengthScale = 1;
            Terrain.ContactDistance = 0.2f;
            Terrain.Restitution = 0.8f;
            Terrain.UseCCD = true;
        }

        public override void Apply(Scene3D scene)
        {

        }

        public PhysicSettings Ball { get; }

        public PhysicSettings Racket { get; }

        public PhysicSettings Terrain { get; }

        public float LengthScale { get; set; }

        public bool UsePcm { get; set; }

    }

    public class PingPongSettingsPanel : UIRoot
    {
        public PingPongSettingsPanel(PingPongSettings settings, Scene3D scene)
        {
            var binder = new Binder<PingPongSettings>(settings);

            binder.PropertyChanged += (_, _, _, _) =>
            {
                settings.Apply(scene);
            };

            UiBuilder.From(this).Name("main").AsColumn()
                .Style(s => s.Padding(16).Color("#F5F5F5").BackgroundColor("#050505AF"))
            .BeginRow(s => s.ColGap(16).FlexGrow(1))
                .BeginColumn(s => s.FlexBasis(1).RowGap(16))
                    .AddText("Ball", s => s.FontSize(1.5f, Unit.Em))
                    .AddInputRange("Contact Distance", 0.01f, 1f, binder.Prop(a=> a.Ball.ContactDistance))
                    .AddInputRange("Length Scale", 0.01f, 100, binder.Prop(a => a.Ball.LengthScale))
                    .AddInputRange("Restitution", 0, 1, binder.Prop(a => a.Ball.Restitution))
                    .AddInput("Use CCD", new CheckBox(), binder.Prop(a => a.Ball.UseCCD))
                .EndChild()
                .BeginColumn(s => s.FlexBasis(1).RowGap(16))
                    .AddText("Racket", s => s.FontSize(1.5f, Unit.Em))
                    .AddInputRange("Contact Distance", 0.01f, 1f, binder.Prop(a => a.Racket.ContactDistance))
                    .AddInputRange("Length Scale", 0.01f, 100, binder.Prop(a => a.Racket.LengthScale))
                    .AddInputRange("Restitution", 0, 1, binder.Prop(a => a.Racket.Restitution))
                    .AddInput("Use CCD", new CheckBox(), binder.Prop(a => a.Racket.UseCCD))
                .EndChild()
                .BeginColumn(s => s.FlexBasis(1).RowGap(16))
                    .AddText("Terrain", s => s.FontSize(1.5f, Unit.Em))
                    .AddInputRange("Contact Distance", 0.01f, 1f, binder.Prop(a => a.Terrain.ContactDistance))
                    .AddInputRange("Length Scale", 0.01f, 100, binder.Prop(a => a.Terrain.LengthScale))
                    .AddInputRange("Restitution", 0, 1, binder.Prop(a => a.Terrain.Restitution))
                    .AddInput("Use CCD", new CheckBox(), binder.Prop(a => a.Terrain.UseCCD))
                .EndChild()
                .BeginColumn(s => s.FlexBasis(1).RowGap(16))
                    .AddText("Scene", s => s.FontSize(1.5f, Unit.Em))
                    .AddInputRange("Length Scale", 0.01f, 100, binder.Prop(a=> a.LengthScale))
                    .AddInput("Use PCM", new CheckBox(), binder.Prop(a => a.UsePcm))
                .EndChild()
            .EndChild()
            .AddText("Footer");
        }

    }
}
