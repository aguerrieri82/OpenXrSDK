using CanvasUI;
using CanvasUI.Components;
using UI.Binding;
using XrEngine;
using XrEngine.OpenXr;
using XrEngine.Physics;
using CheckBox = CanvasUI.CheckBox;

namespace XrSamples
{
    public class ThrowSettings : BaseAppSettings
    {

        public ThrowSettings()
        {
            Sensitivity = 0.2f;
            AutoThrow = false;
            SimFps = 40;

        }

        public override void Apply(Scene3D scene)
        {
            ((PlotterTimeLogger)Context.Require<ITimeLogger>()).IsEnabled = !DisableLog;

            SpeedTracker.SmoothFactor = Sensitivity;
            SpeedTracker.AutoThrow = AutoThrow;
            SpeedTracker.MinDeltaTime = MinDeltaTime / 1000f;

            scene.Component<PhysicsManager>().StepSizeSecs = 1f / SimFps;   

            if (_filePath != null)
            {
                Save();
                Log.Info(this, "Settings SAVED");
            }
        }

        public float SimFps { get; set; }   

        public float Sensitivity { get; set; }

        public float MinDeltaTime { get; set; }

        public bool AutoThrow { get; set; }

        public bool DisableLog { get; set; }
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
                .AddInputRange("Min Delta Time", 0f, 1000f, binder.Prop(a => a.MinDeltaTime))
                .AddInputRange("Sim Fps", 30f, 200f, binder.Prop(a => a.SimFps))
                .BeginRow(s => s.ColGap(16))
                    .AddInput("Disable Log", new CheckBox(), binder.Prop(a => a.DisableLog))
                    .AddInput("Auto Throw", new CheckBox(), binder.Prop(a => a.AutoThrow))
                .EndChild()
            .EndChild()

            .AddChild(plotter, bld => bld
                .Style(s => s.FlexGrow(1)));

            if (!XrPlatform.IsEditor)
                Context.Implement<ITimeLogger>(new PlotterTimeLogger(plotter));

            plotter.AutoScaleY = AutoScaleYMode.None;
            plotter.PixelPerUnitY = 20f;
        }

    }
}
