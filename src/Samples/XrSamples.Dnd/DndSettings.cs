using CanvasUI;
using UI.Binding;
using XrEngine;
using XrEngine.OpenXr;
using XrMath;
using CheckBox = CanvasUI.CheckBox;

namespace XrSamples.Dnd
{
    public class DndSettings : BaseAppSettings
    {

        public DndSettings()
        {
            Zoom = 1f;
        }

        public override void Apply(Scene3D scene)
        {
            var myScene = (DndScene)scene;  

            myScene.Map!.Component<BoundsGrabbable>().IsEnabled = !DisableMove;

            //var envView = myScene.Children.OfType<EnvironmentView>().First();
            //envView.IsVisible = ShowSKy;

            if (ShowSKy)
                scene.ActiveCamera!.BackgroundColor = "#7C93DB";
            else
                scene.ActiveCamera!.BackgroundColor = Color.Transparent;


            if (_filePath != null)
            {
                Save();
                Log.Info(this, "Settings SAVED");
            }
        }

        public float Zoom { get; set; }

        public bool DisableMove { get; set; }

        public bool ShowSKy { get; set; }
    }

    public class DndSettingsPanel : UIRoot
    {
        public DndSettingsPanel(DndSettings settings, DndScene scene)
        {
            var binder = new Binder<DndSettings>(settings);

            void Binder_PropertyChanged(DndSettings? obj, IProperty property, object? value, object? oldValue)
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
                .AddInputRange("Zoom", 0.05f, 1f, binder.Prop(a => a.Zoom))
                .AddInput("Disable Move", new CheckBox(), binder.Prop(a => a.DisableMove))
                .AddInput("Show Sky", new CheckBox(), binder.Prop(a => a.ShowSKy))
                .BeginRow(s => s.RowGap(16))
                    .AddButton("Reset", scene.ResetPose, s => s.Padding(8, 16).BackgroundColor("#1565C0"))
                .EndChild()
            .EndChild();
        }

    }
}
