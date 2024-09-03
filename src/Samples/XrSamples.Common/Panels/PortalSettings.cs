using CanvasUI;
using Microsoft.Extensions.Logging;
using PhysX;
using UI.Binding;
using XrEngine;
using XrEngine.OpenXr;
using XrEngine.Physics;
using XrMath;

namespace XrSamples
{


    public class PortalSettings : BaseAppSettings
    {
        static readonly Material matShow = PbrMaterial.CreateDefault("#fff");
        static readonly Material matHide = new ColorMaterial(Color.Transparent);

        public PortalSettings()
        {
            Radius = 3;
        }



        public override void Apply(Scene3D scene)
        {
            var mesh = scene.FindByName<TriangleMesh>("mesh")!;
            var mat = ((FishReflectionSphereMaterial)mesh.Materials[0])!;
            mat.SpherRadius = Radius;    

            if (_filePath != null)
            {
                Save();
                Log.Info(this, "Settings SAVED");
            }
        }

        public float Radius { get; set; }
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
                    .BackgroundColor("#050505AF"))
            .BeginRow(s => s.ColGap(16))
                 .AddInputRange("Contact Distance", 1f, 20f, binder.Prop(a => a.Radius))
            .EndChild();

        }

    }
}
