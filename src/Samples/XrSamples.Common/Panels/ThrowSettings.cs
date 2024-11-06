using CanvasUI;
using CanvasUI.Components;
using UI.Binding;
using XrEngine;
using XrEngine.OpenXr;
using CheckBox = CanvasUI.CheckBox;

namespace XrSamples
{
    public class ThrowSettings : BaseAppSettings
    {

        public ThrowSettings()
        {
            Sensitivity = 0.2f;
            ManualMode = false;

        }

        public override void Apply(Scene3D scene)
        {
            SpeedTracker.SmoothFactor = Sensitivity;

            if (_filePath != null)
            {
                Save();
                Log.Info(this, "Settings SAVED");
            }
        }

        public float Sensitivity { get; set; }

        public bool ManualMode { get; set; }
    }

    public class ThrowSettingsPanel : UIRoot
    {
        public ThrowSettingsPanel(ThrowSettings settings, Scene3D scene)
        {
            var binder = new Binder<ThrowSettings>(settings);

            void Binder_PropertyChanged(ThrowSettings? obj, IProperty property, object? value, object? oldValue)
            {
                obj!.Apply(scene);
            }

            var plotter = new Plotter();

            binder.PropertyChanged += Binder_PropertyChanged;

            UiBuilder.From(this).Name("main").AsColumn()
            .Style(s =>
                s.Padding(16)
                .RowGap(16)
                .Color("#F5F5F5")
                .BackgroundColor("#050505AF")
             )
            .BeginColumn(s => s.RowGap(16))
                .AddInputRange("Sensitivity", 0f, 1f, binder.Prop(a => a.Sensitivity))
                .AddInput("Manual", new CheckBox(), binder.Prop(a => a.ManualMode))
            .EndChild()

            .AddChild(plotter, bld => bld
                .Style(s => s.FlexGrow(1)));


            if (!XrPlatform.IsEditor)
                Context.Implement<ITimeLogger>(new PlotterTimeLogger(plotter));
        }

    }
}
