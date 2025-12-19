using CanvasUI;
using Microsoft.Extensions.Logging;
using UI.Binding;
using XrEngine;
using XrEngine.OpenXr;
using XrEngine.OpenXr.Components;
using XrEngine.Physics;
using XrEngine.UI;
using CheckBox = CanvasUI.CheckBox;

namespace XrSamples
{
    public class ThrowSettings : BaseAppSettings<Object3D>
    {

        public ThrowSettings()
        {
            Amplification = 1f;
            AutoThrow = false;
            SimFps = 0;
            MinDeltaTime = 25;
            SamplesToSkip = 0;
            SampleCount = 3;
            Mode = Throwable.AvgMode.WeightedExponential;
        }

        public override void Apply(Object3D obj)
        {
            if (Context.Require<ITimeLogger>() is PlotterTimeLogger tl)
                tl.IsEnabled = !DisableLog;

            var tracker = obj.Component<Throwable>();

            tracker.Amplification = Amplification;
            tracker.AutoThrow = AutoThrow;
            tracker.MinDeltaTime = MinDeltaTime / 1000f;
            tracker.SamplesToSkip = (int)SamplesToSkip;
            tracker.MaxSamples = (int)SampleCount;

            obj.Scene!.Component<PhysicsManager>().StepSizeSecs = SimFps == 0 ? 0 : 1f / SimFps;

            if (_filePath != null)
            {
                Save();
                Log.Info(this, "Settings SAVED");
            }
        }

        public float SimFps { get; set; }

        public float Amplification { get; set; }

        public float MinDeltaTime { get; set; }

        public bool AutoThrow { get; set; }

        public bool DisableLog { get; set; }
        public Throwable.AvgMode Mode { get; set; }
        public float SampleCount { get; set; }
        public float SamplesToSkip { get; set; }
    }

    public class ThrowSettingsPanel : UIRoot
    {
        public ThrowSettingsPanel(ThrowSettings settings, Object3D obj)
        {
            var binder = new Binder<ThrowSettings>(settings);

            void Binder_PropertyChanged(ThrowSettings? settings, IProperty property, object? value, object? oldValue)
            {
                settings!.Apply(obj);
            }
            /*
            var plotter = new Plotter();
            plotter.AutoScaleY = AutoScaleYMode.None;
            plotter.PixelPerUnitY = 20f;
            */

            TextBlock? logger = null;

            binder.PropertyChanged += Binder_PropertyChanged;

            UiBuilder.From(this).Name("main").AsColumn()
            .Style(s =>
                s.Padding(16)
                .RowGap(16)
                .Color("#F5F5F5")
                .BackgroundColor("#050505AF")
             )
            .BeginColumn(s => s.RowGap(16))
                .AddInputRange("Amplification", 0f, 2f, binder.Prop(a => a.Amplification))
                .AddInputRange("Min Delta Time", 0f, 1000f, binder.Prop(a => a.MinDeltaTime))
                .AddInputRange("Sim Fps", 30f, 200f, binder.Prop(a => a.SimFps))
                .AddInputRange("Sample count", 2, 10, binder.Prop(a => a.SampleCount), 1)
                .AddInputRange("Sample to skip", 0, 10, binder.Prop(a => a.SamplesToSkip), 1)
                .BeginRow(s => s.ColGap(16))
                    .AddButton("Normal", () => binder.Value.Mode = Throwable.AvgMode.Normal)
                    .AddButton("Weight", () => binder.Value.Mode = Throwable.AvgMode.Weighted)
                    .AddButton("Weight Exp", () => binder.Value.Mode = Throwable.AvgMode.WeightedExponential)
                .EndChild()
                .BeginRow(s => s.ColGap(16))
                    .AddInput("Disable Log", new CheckBox(), binder.Prop(a => a.DisableLog))
                    .AddInput("Auto Throw", new CheckBox(), binder.Prop(a => a.AutoThrow))
                .EndChild()
            .EndChild()

            .AddText(bld => bld
                .Style(s => s
                    .Padding(16)
                    .FlexBasis(2)
                    .FlexGrow(1)
                    .LineSize(20)
                    .AlignSelf(UiAlignment.Stretch)
                    .Border(1, "#0f0"))
                .Set(a => logger = a));

            if (!XrPlatform.IsEditor)
            {
                var service = new TextBlockProgressLogger(logger!, 25);
                Context.Implement<IProgressLogger>(service);
                Context.Implement<ILogger>(service);
            }


        }

    }
}
