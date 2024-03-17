using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using XrEngine.Physics;
using XrMath;

namespace XrEngine.OpenXr
{
    public class XrEngineAppBuilder
    {
        readonly XrEngineAppOptions _options = new();
        readonly List<Action<XrEngineApp>> _configurations = [];
        EngineApp? _app;
        readonly List<Action<IXrActionBuilder>> _inputs = [];
        Type? _inputProfile;
        IXrEnginePlatform? _platform;

        public XrEngineAppBuilder()
        {
            _options.Driver = GraphicDriver.OpenGL;
            _options.ResolutionScale = 1;
            _options.SampleCount = 1;
            _options.RenderMode = XrRenderMode.SingleEye;
            _platform = Platform.Current;
        }

        public XrEngineAppBuilder UsePlatform(IXrEnginePlatform platform)
        {
            _platform = platform;
            Platform.Current = _platform;
            return this;
        }

        public XrEngineAppBuilder UsePlatform<T>() where T : IXrEnginePlatform, new()
        {
            return UsePlatform(new T());
        }

        public XrEngineAppBuilder ConfigureApp(Action<XrEngineApp> configure)
        {
            _configurations.Add(configure);
            return this;
        }

        public XrEngineAppBuilder Configure(Action<XrEngineAppBuilder> configure)
        {
            configure(this);
            return this;
        }

        public XrEngineAppBuilder SetRenderQuality(float resolutionScale, uint sampleCount)
        {
            _options.ResolutionScale = resolutionScale;
            _options.SampleCount = sampleCount;
            return this;
        }

        public XrEngineAppBuilder UseInputs<TProfile>(Action<XrActionsBuilder<TProfile>> builder) where TProfile : new()
        {
            if (_inputProfile != null && _inputProfile != typeof(TProfile))
                throw new ArgumentException("Input profile differ");

            _inputProfile = typeof(TProfile);

            _inputs.Add(a => builder((XrActionsBuilder<TProfile>)a));

            return this;
        }

        public XrEngineAppBuilder UseInputs<TProfile>() where TProfile : new()
        {
            return UseInputs<TProfile>(a => a.AddAll());
        }

        public XrEngineAppBuilder UseApp(EngineApp app)
        {
            _app = app;
            return this;
        }

        public XrEngineAppBuilder UseOpenGL()
        {
            _options.Driver = GraphicDriver.OpenGL;
            return this;
        }

        public XrEngineAppBuilder UseMultiView()
        {
            _options.RenderMode = XrRenderMode.MultiView;
            return this;
        }

        public XrEngineAppBuilder UseStereo()
        {
            _options.RenderMode = XrRenderMode.Stereo;
            return this;
        }

        public XrEngineAppBuilder UseFilament()
        {
            _options.Driver = GraphicDriver.FilamentVulkan;
            return this;
        }

        public XrEngineAppBuilder AddPassthrough() => ConfigureApp(e =>
        {
            e.XrApp.Layers.List.Insert(0, new XrPassthroughLayer());
        });

        public XrEngineAppBuilder UseLeftController()
        {
            UseInputs<XrOculusTouchController>(bld =>

            bld.AddAction(a => a.Left!.AimPose)
                .AddAction(a => a.Left!.GripPose)
                .AddAction(a => a.Left!.SqueezeClick)
                .AddAction(a => a.Left!.SqueezeValue)
                .AddAction(a => a.Left!.Button!.XClick)
                .AddAction(a => a.Left!.Button!.YClick)
                .AddAction(a => a.Left!.TriggerClick)
                .AddAction(a => a.Left!.TriggerValue));

            return this;
        }

        public XrEngineAppBuilder AddRightPointer() => ConfigureApp(e =>
        {
            var inputs = e.Inputs as XrOculusTouchController;

            e.App.ActiveScene!.AddComponent(new XrInputPointer
            {
                PoseInput = inputs!.Right!.AimPose,
                RightButton = inputs!.Right!.SqueezeClick!,
                LeftButton = inputs!.Right!.TriggerClick!,
            });
        });

        public XrEngineAppBuilder UseRightController()
        {
            UseInputs<XrOculusTouchController>(bld => bld
                .AddAction(a => a.Right!.AimPose)
                .AddAction(a => a.Right!.GripPose)
                .AddAction(a => a.Right!.SqueezeValue)
                .AddAction(a => a.Right!.SqueezeClick)
                .AddAction(a => a.Right!.Button!.ATouch)
                .AddAction(a => a.Right!.Button!.BClick)
                .AddAction(a => a.Right!.TriggerClick)
                .AddAction(a => a.Right!.TriggerValue));



            return this;
        }

        public XrEngineAppBuilder UseRayCollider() => ConfigureApp(e =>
        {
            var inputs = e.GetInputs<XrOculusTouchController>();

            var rayCol = e.App!.ActiveScene!.AddComponent(new RayCollider(inputs.Right!.AimPose!));
        });


        public XrEngineAppBuilder UseHands() => ConfigureApp(e =>
        {
            var rHand = e.XrApp.AddHand<XrHandInputMesh>(HandEXT.RightExt);
            var lHand = e.XrApp.AddHand<XrHandInputMesh>(HandEXT.LeftExt);

            e.App.ActiveScene!.AddChild(new OculusHandView(rHand!));
            e.App.ActiveScene!.AddChild(new OculusHandView(lHand!));
        });

        public XrEngineAppBuilder UseGrabbers() => UseLeftController().
                                                   UseRightController().
                                                   ConfigureApp(e =>
        {
            var inputs = e.GetInputs<XrOculusTouchController>();

            e.App!.ActiveScene!.AddComponent(new InputObjectGrabber(
                inputs.Right!.GripPose!,
                null,
                inputs.Right!.SqueezeValue!,
                inputs.Right!.TriggerValue!));

            e.App!.ActiveScene!.AddComponent(new InputObjectGrabber(
                inputs.Left!.GripPose!,
                null,
                inputs.Left!.SqueezeValue!,
                inputs.Left!.TriggerValue!));

            foreach (var hand in e.App.ActiveScene.Descendants<OculusHandView>())
                hand.AddComponent(new HandObjectGrabber());
        });

        public XrEngineAppBuilder UseScene(bool arMode) => ConfigureApp(e =>
        {
            var model = new OculusSceneModel();
            if (arMode)
                model.Material = new ColorMaterial { Color = Color.Transparent };

            e.App.ActiveScene!.AddChild(model);

        });

        public XrEngineAppBuilder UsePhysics() => ConfigureApp(e =>
        {
            e.App.ActiveScene!.AddComponent<PhysicsManager>();
        });

        public XrEngineApp Build()
        {
            _platform ??= Platform.Current;

            if (_platform == null)
                throw new ArgumentNullException("Platform not specified");

            var engine = new XrEngineApp(_options, _platform);

            engine.Create(_app ?? new EngineApp());

            IXrActionBuilder? actionBuilder = null;

            foreach (var config in _configurations)
            {
                config(engine);

                if (_inputProfile != null && actionBuilder == null)
                {
                    var builderType = typeof(XrActionsBuilder<>).MakeGenericType(_inputProfile);

                    actionBuilder = (IXrActionBuilder)Activator.CreateInstance(builderType, engine.XrApp)!;

                    engine.Inputs = actionBuilder.Result;
                }

                while (_inputs.Count > 0)
                {
                    foreach (var input in _inputs)
                        input(actionBuilder!);

                    _inputs.Clear();
                }
            }

            if (actionBuilder != null)
                engine.XrApp.AddActions(actionBuilder);

            engine.XrApp.BindEngineApp(engine.App); //TODO previous was in XrEngineApp.Create, but leads some error

            return engine;
        }
    }
}
