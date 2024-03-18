using CanvasUI;
using PhysX;
using XrEngine;
using XrEngine.Physics;
using XrMath;

namespace XrSamples
{

    public class PingPongSettings : BaseAppSettings
    {

        public PingPongSettings()
        {
            Ball = new PhysicSettings();
            Racket = new PhysicSettings();
            Terrain = new PhysicSettings();

            LengthToleranceScale = 1;
            EnablePCM = true;
            ShowTerrain = false;

            Ball.LengthToleranceScale = 1;
            Ball.ContactOffset = 0.01f;
            Ball.Restitution = 0.7f;
            Ball.EnableCCD = true;
            Ball.ContactReportThreshold = 1;

            Racket.LengthToleranceScale = 1;
            Racket.ContactOffset = 0.01f;
            Racket.Restitution = 0.7f;
            Racket.EnableCCD = true;
            Racket.ContactReportThreshold = 1;

            Terrain.LengthToleranceScale = 1;
            Terrain.ContactOffset = 0.2f;
            Terrain.Restitution = 0.7f;
            Terrain.EnableCCD = true;
            Terrain.ContactReportThreshold = 1;

        }


        public void Apply(Object3D obj, PhysicSettings settings)
        {
            var body = obj.Component<RigidBody>();
            body.LengthToleranceScale = settings.LengthToleranceScale;
            body.EnableCCD = settings.EnableCCD;

            if (body.Type != PhysX.Framework.PhysicsActorType.Static)
                body.DynamicActor.ContactReportThreshold = settings.ContactReportThreshold;

            foreach (var shape in body.Actor.GetShapes())
            {
                var mat = shape.GetMaterials()[0];
                mat.Restitution = settings.Restitution;
                shape.ContactOffset = settings.ContactOffset;
            }
        }

        public override void Apply(Scene3D scene)
        {
            var system = scene.FindFeature<PhysicsManager>()!.System;
            var racket = scene.FindByName<Object3D>("Racket");
            var generator = scene.FindFeature<BallGenerator>()!;
            var mesh = scene.FindByName<TriangleMesh>("global-mesh");

            if (mesh != null)
            {
                Apply(mesh, Terrain);

                if (ShowTerrain)
                    mesh.Materials[0] = PbrMaterial.CreateDefault("#fff");
                else
                    mesh.Materials[0] = new ColorMaterial(Color.Transparent);

                mesh.Version++;
            }


            Apply(racket!, Racket);

            generator.PhysicSettings = Ball;
            foreach (var ball in generator.Balls)
                Apply(ball, Ball);

            system.Scene.SetFlag(PxSceneFlag.EnablePcm, EnablePCM);
            system.Scene.SetFlag(PxSceneFlag.EnableCcd, EnableCCD);

            if (_filePath != null)
                Save();
        }

        public PhysicSettings Ball { get; set; }

        public PhysicSettings Racket { get; set; }

        public PhysicSettings Terrain { get; set; }

        public float LengthToleranceScale { get; set; }

        public bool EnablePCM { get; set; }

        public bool EnableCCD { get; set; }

        public bool ShowTerrain { get; set; }
    }

    public class PingPongSettingsPanel : UIRoot
    {
        public PingPongSettingsPanel(PingPongSettings settings, Scene3D scene)
        {
            var binder = new Binder<PingPongSettings>(settings);

            binder.PropertyChanged += (_, _, _, _) =>
            {

            };

            var generator = scene.FindFeature<BallGenerator>()!;

            generator.NewBallCreated += ball =>
            {
                settings.Apply(ball, settings.Ball);
            };

            TextBlock? logger = null;

            UiBuilder.From(this).Name("main").AsColumn()
                .Style(s => 
                    s.Padding(16)
                    .RowGap(16)
                    .Color("#F5F5F5")
                    .BackgroundColor("#050505AF"))
            .BeginRow(s => s.ColGap(16))
                .BeginColumn(s => s.FlexBasis(1).RowGap(16))
                    .AddText("Ball", s => s.FontSize(1.5f, Unit.Em))
                    .AddInputRange("Contact Distance", 0.01f, 1f, binder.Prop(a => a.Ball.ContactOffset))
                    .AddInputRange("Restitution", 0, 1, binder.Prop(a => a.Ball.Restitution))
                    .AddInputRange("Contact", 0.01f, 10, binder.Prop(a => a.Ball.ContactReportThreshold))
                    .AddInput("Use CCD", new CheckBox(), binder.Prop(a => a.Ball.EnableCCD))
                .EndChild()
                .BeginColumn(s => s.FlexBasis(1).RowGap(16))
                    .AddText("Racket", s => s.FontSize(1.5f, Unit.Em))
                    .AddInputRange("Contact Distance", 0.01f, 1f, binder.Prop(a => a.Racket.ContactOffset))
                    .AddInputRange("Length Scale", 0.01f, 100, binder.Prop(a => a.Racket.LengthToleranceScale))
                    .AddInputRange("Restitution", 0, 1, binder.Prop(a => a.Racket.Restitution))
                    .AddInputRange("Contact", 0.01f, 10, binder.Prop(a => a.Racket.ContactReportThreshold))
                    .AddInput("Use CCD", new CheckBox(), binder.Prop(a => a.Racket.EnableCCD))
                .EndChild()
                .BeginColumn(s => s.FlexBasis(1).RowGap(16))
                    .AddText("Terrain", s => s.FontSize(1.5f, Unit.Em))
                    .AddInputRange("Contact Distance", 0.01f, 1f, binder.Prop(a => a.Terrain.ContactOffset))
                    .AddInputRange("Length Scale", 0.01f, 100, binder.Prop(a => a.Terrain.LengthToleranceScale))
                    .AddInputRange("Restitution", 0, 1, binder.Prop(a => a.Terrain.Restitution))
                    .AddInput("Use CCD", new CheckBox(), binder.Prop(a => a.Terrain.EnableCCD))
                .EndChild()
                .BeginColumn(s => s.FlexBasis(1).RowGap(16))
                    .AddText("Scene", s => s.FontSize(1.5f, Unit.Em))
                    .AddInputRange("Length Scale", 0.01f, 100, binder.Prop(a => a.LengthToleranceScale))
                    .AddInput("Use PCM", new CheckBox(), binder.Prop(a => a.EnablePCM))
                    .AddInput("Use CCD", new CheckBox(), binder.Prop(a => a.EnableCCD))
                    .AddInput("Show Terrain", new CheckBox(), binder.Prop(a => a.ShowTerrain))
                .EndChild()
            .EndChild()
            .AddText(bld => bld
                .Style(s=> s
                    .Padding(16)
                    .FlexGrow(1)
                    .LineSize(20)
                    .AlignSelf(UiAlignment.Stretch)
                    .Border(1,"#0f0"))
                .Set(a=> logger = a))
            .BeginRow(s => s.JustifyContent(UiAlignment.End))
                .AddButton("Apply", () => settings.Apply(scene), s => s.Padding(8, 16).BackgroundColor("#1565C0"))
            .EndChild();

            XrPlatform.Current!.Logger = new TextBlockLogger(logger!, 25);
        }

    }
}
