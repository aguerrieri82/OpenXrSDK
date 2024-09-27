using CanvasUI;
using Microsoft.Extensions.Logging;
using PhysX;
using UI.Binding;
using XrEngine;
using XrEngine.OpenXr;
using XrEngine.Physics;
using XrMath;
using CheckBox = CanvasUI.CheckBox;

namespace XrSamples
{
    public class MaterialSettings
    {
        public float Metallic { get; set; }

        public float Roughness { get; set; }
    }

    public class PingPongSettings : BaseAppSettings
    {
        static readonly Material matShow = PbrMaterial.CreateDefault("#fff");
        static readonly Material matHide = new ColorMaterial(Color.Transparent);

        public PingPongSettings()
        {
            Ball = new PhysicSettings();
            Racket = new PhysicSettings();
            Terrain = new PhysicSettings();

            LengthToleranceScale = 1;
            EnablePCM = true;
            EnableCCD = true;
            ShowTerrain = false;

            BallMaterial = new MaterialSettings();
            BallMaterial.Roughness = 0.4f;
            BallMaterial.Metallic = 0.1f;

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

            Exposure = 0.5f;
            LightIntensity = 1f;
        }


        public void Apply(Object3D obj, PhysicSettings settings)
        {
            var body = obj.Component<RigidBody>();

            body.LengthToleranceScale = settings.LengthToleranceScale;
            body.EnableCCD = settings.EnableCCD;
            body.ContactReportThreshold = settings.ContactReportThreshold;
            body.ContactOffset = settings.ContactOffset;

            var curMat = body.Material;
            curMat.Restitution = settings.Restitution;
            body.Material = curMat;

            if (body.IsCreated)
            {
                if (body.Type != PhysX.Framework.PhysicsActorType.Static)
                    body.DynamicActor.ContactReportThreshold = settings.ContactReportThreshold;

                foreach (var shape in body.Actor.GetShapes())
                {
                    var mat = shape.GetMaterials()[0];
                    mat.Restitution = settings.Restitution;
                    shape.ContactOffset = settings.ContactOffset;
                }
            }

        }

        public override void Apply(Scene3D scene)
        {
            var pyManager = scene.FeatureDeep<PhysicsManager>()!;
            var system = pyManager.System;
            var racket = scene.FindByName<Object3D>("Racket");
            var generator = scene.FeatureDeep<BallGenerator>()!;
            var mesh = scene.FindByName<TriangleMesh>("global-mesh");

            pyManager.Options.LengthTolerance = LengthToleranceScale;
            pyManager.Options.EnablePCM = EnablePCM;
            pyManager.Options.EnableCCD = EnableCCD;

            if (mesh != null)
            {
                Apply(mesh, Terrain);

                if (ShowTerrain)
                    mesh.Materials[0] = matShow;
                else
                    mesh.Materials[0] = matHide;

                mesh.NotifyChanged(ObjectChangeType.Render);
            }


            Apply(racket!, Racket);

            generator.PhysicSettings = Ball;
            foreach (var ball in generator.Balls)
                Apply(ball, Ball);



            if (system != null)
            {
                system.Scene.SetFlag(PxSceneFlag.EnablePcm, EnablePCM);
                system.Scene.SetFlag(PxSceneFlag.EnableCcd, EnableCCD);
            }

            scene.PerspectiveCamera().Exposure = Exposure;

            var light = scene.Descendants<ImageLight>().FirstOrDefault();
            if (light != null)
            {
                light.Intensity = LightIntensity;
                light.NotifyChanged(ObjectChangeType.Render);
            }


            if (_filePath != null)
            {
                Save();
                Log.Info(this, "Settings SAVED");
            }
        }

        public MaterialSettings BallMaterial { get; set; }


        public PhysicSettings Ball { get; set; }

        public PhysicSettings Racket { get; set; }

        public PhysicSettings Terrain { get; set; }

        public float LengthToleranceScale { get; set; }

        public bool EnablePCM { get; set; }

        public bool EnableCCD { get; set; }

        public bool ShowTerrain { get; set; }

        public float Exposure { get; set; }

        public float LightIntensity { get; set; }
    }

    public class PingPongSettingsPanel : UIRoot
    {
        public PingPongSettingsPanel(PingPongSettings settings, Scene3D scene)
        {
            var binder = new Binder<PingPongSettings>(settings);

            void Binder_PropertyChanged(PingPongSettings? obj, IProperty property, object? value, object? oldValue)
            {
                if (property.Name!.Contains("BallMaterial"))
                {
                    var generator = scene.FeatureDeep<BallGenerator>()!;

                    (generator.Material as PbrMaterial)!.MetallicRoughness!.RoughnessFactor = obj!.BallMaterial!.Roughness;
                    (generator.Material as PbrMaterial)!.MetallicRoughness!.MetallicFactor = obj!.BallMaterial!.Metallic;
                    generator.Material.NotifyChanged(ObjectChangeType.Render);

                }
                if (property.Name!.Contains("Exposure"))
                {
                    scene.PerspectiveCamera().Exposure = obj!.Exposure;
                }
                if (property.Name!.Contains("Light"))
                {
                    var light = scene.Descendants<ImageLight>().First();
                    light.Intensity = obj!.LightIntensity;
                    light.NotifyChanged(ObjectChangeType.Render);
                }
            }

            binder.PropertyChanged += Binder_PropertyChanged;

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
                    .AddInputRange("Contact Threshold", 0.01f, 10, binder.Prop(a => a.Ball.ContactReportThreshold))
                    .AddInput("Use CCD", new CheckBox(), binder.Prop(a => a.Ball.EnableCCD))
                .EndChild()
                .BeginColumn(s => s.FlexBasis(1).RowGap(16))
                    .AddText("Racket", s => s.FontSize(1.5f, Unit.Em))
                    .AddInputRange("Contact Distance", 0.01f, 1f, binder.Prop(a => a.Racket.ContactOffset))
                    .AddInputRange("Length Scale", 0.01f, 100, binder.Prop(a => a.Racket.LengthToleranceScale))
                    .AddInputRange("Restitution", 0, 1, binder.Prop(a => a.Racket.Restitution))
                    .AddInputRange("Contact Threshold", 0.01f, 10, binder.Prop(a => a.Racket.ContactReportThreshold))
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
            .BeginRow(s => s.ColGap(16).FlexGrow(1))
                .AddText(bld => bld
                    .Style(s => s
                        .Padding(16)
                        .FlexBasis(2)
                        .FlexGrow(1)
                        .LineSize(20)
                        .AlignSelf(UiAlignment.Stretch)
                        .Border(1, "#0f0"))
                    .Set(a => logger = a))
                .BeginColumn(s => s.FlexBasis(1).RowGap(16))
                    .AddInputRange("Metallic", 0f, 1f, binder.Prop(a => a.BallMaterial.Metallic))
                    .AddInputRange("Roughness", 0f, 1f, binder.Prop(a => a.BallMaterial.Roughness))
                    .AddInputRange("Exposure", 0f, 1f, binder.Prop(a => a.Exposure))
                    .AddInputRange("Intensity", 0.01f, 5f, binder.Prop(a => a.LightIntensity))
                .EndChild()

            .EndChild()
            .BeginRow(s => s.JustifyContent(UiAlignment.End))
                .AddButton("Apply", () => settings.Apply(scene), s => s.Padding(8, 16).BackgroundColor("#1565C0"))
            .EndChild();

            Context.Implement<ILogger>(new TextBlockLogger(logger!, 25));
        }

    }
}
