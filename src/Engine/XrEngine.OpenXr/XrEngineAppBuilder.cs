using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using XrEngine.Physics;

namespace XrEngine.OpenXr
{
    public class XrEngineAppBuilder 
    {
        XrEngineAppOptions _options = new();
        List<Action<XrEngineApp>> _configurations = [];
        EngineApp? _app;
        List<Action<IXrActionBuilder>> _inputs = [];
        Type? _inputProfile;
        IXrPlatform? _platform;

        public XrEngineAppBuilder()
        {
            _options.Driver = GraphicDriver.OpenGL;
            _options.ResolutionScale = 1;
            _options.SampleCount = 1;
            _options.RenderMode = XrRenderMode.SingleEye;
            _platform = Platform.Current;
        }


        public XrEngineAppBuilder UsePlatform(IXrPlatform platform)
        {
            _platform = platform;
            return this;
        }

        public XrEngineAppBuilder UsePlatform<T>() where T : IXrPlatform, new()
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

        public XrEngineAppBuilder UseInputs<TProfile>(Action<XrActionsBuilder<TProfile>> builder) where TProfile: new()
        {
            if (_inputProfile != null && _inputProfile != typeof(TProfile))
                throw new ArgumentException("Input profile differ");

            _inputProfile = typeof(TProfile);
            
            _inputs.Add(a => builder((XrActionsBuilder<TProfile>)a));

            return this;
        }

        public XrEngineAppBuilder UseInputs<TProfile>() where TProfile : new() => ConfigureApp(e =>
        {
            if (_inputProfile != null && _inputProfile != typeof(TProfile))
                throw new ArgumentException("Input profile differ");

            _inputProfile = typeof(TProfile);
            e.XrApp.WithInteractionProfile<TProfile>(bld => bld.AddAll());
        });


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

        public XrEngineAppBuilder UseFilament()
        {
            _options.Driver = GraphicDriver.FilamentVulkan;
            return this;
        }

        public XrEngineAppBuilder AddPassthrough() => ConfigureApp(e =>
        {
            e.XrApp.Layers.Add<XrPassthroughLayer>();
        });

        public XrEngineAppBuilder UseControllers()
        {
            return this;
        }

        public XrEngineAppBuilder UseRayCollider() => ConfigureApp(e =>
        {
            var rayCol = e.App!.ActiveScene!.AddComponent(new RayCollider((XrPoseInput)e.XrApp.Inputs["RightAimPose"]));
            rayCol.RayView.IsVisible = false;
        });


        public XrEngineAppBuilder UseHands() => ConfigureApp(e =>
        {
            var rHand = e.XrApp.AddHand<XrHandInputMesh>(HandEXT.RightExt);
            var lHand = e.XrApp.AddHand<XrHandInputMesh>(HandEXT.LeftExt);

            e.App.ActiveScene!.AddChild(new OculusHandView(rHand!));
            e.App.ActiveScene!.AddChild(new OculusHandView(lHand!));
        });

        public XrEngineAppBuilder UseGrabbers() => UseInputs<XrOculusTouchController>().
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

        public XrEngineAppBuilder UseScene() => ConfigureApp(e =>
        {
            e.App.ActiveScene!.AddChild(new OculusSceneModel());

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
            
            foreach (var config in _configurations)
                config(engine);

            if (_inputProfile != null)
            {
                engine.Inputs = engine.XrApp.WithInteractionProfile(_inputProfile, bld =>
                {
                    foreach (var input in _inputs)
                        input(bld);
                });
            }

            return engine;
        }
    }
}
