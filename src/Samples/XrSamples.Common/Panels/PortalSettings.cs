using CanvasUI;
using UI.Binding;
using XrEngine;
using XrMath;

namespace XrSamples
{
    public class PortalSettings : BaseAppSettings
    {

        public PortalSettings()
        {
            Radius = 3;
            Offset = 2;
            SphereY = 1.65f;
            Border = 0.1f;
        }

        public override void Apply(Scene3D scene)
        {
            var mesh = scene.FindByName<TriangleMesh>("mesh")!;
            var mat = ((FishReflectionSphereMaterial)mesh.Materials[0])!;

            mat.SpherRadius = Radius;
            mat.Border = Border;    
            mesh.SetProp("Offset", Offset);
            mesh.SetProp("SphereY", SphereY);

            if (_filePath != null)
            {
                Save();
                Log.Info(this, "Settings SAVED");
            }
        }

        public float Radius { get; set; }

        public float Offset { get; set; }

        public float SphereY { get; set; }

        public float Border { get; set; }
    }

    public class PortalSettingsPanel : UIRoot
    {
        public PortalSettingsPanel(PortalSettings settings, Scene3D scene)
        {
            var binder = new Binder<PortalSettings>(settings);

            void Binder_PropertyChanged(PortalSettings? obj, IProperty property, object? value, object? oldValue)
            {
                obj!.Apply(scene);
            }

            binder.PropertyChanged += Binder_PropertyChanged;

            UiBuilder.From(this).Name("main").AsColumn()
            .Style(s =>
                s.Padding(16)
                .RowGap(16)
                .Color("#F5F5F5")
                .BackgroundColor("#050505AF")
             )
            .BeginColumn(s => s.RowGap(16))
                .AddInputRange("Sphere Radius", 1f, 20f, binder.Prop(a => a.Radius))
                .AddInputRange("Wall Offset", 0f, 3f, binder.Prop(a => a.Offset))
                .AddInputRange("Sphere Y", 0f, 2f, binder.Prop(a => a.SphereY))
                .AddInputRange("Border", 0f, 0.2f, binder.Prop(a => a.Border))
            .EndChild();
        }

    }
}
