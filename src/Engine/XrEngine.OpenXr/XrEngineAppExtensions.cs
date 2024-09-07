using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using PhysX.Framework;
using Silk.NET.OpenXR;
using XrEngine.Physics;
using XrMath;

namespace XrEngine.OpenXr
{
    public static class XrEngineAppExtensions
    {

        public static XrEngineAppBuilder AddPassthrough(this XrEngineAppBuilder self) => self.ConfigureApp(e =>
        {
            e.XrApp.Layers.List.Insert(0, new XrPassthroughLayer());
        });

        public static XrEngineAppBuilder UseLeftController(this XrEngineAppBuilder self)
        {
            self.UseInputs<XrOculusTouchController>(bld =>

            bld.AddAction(a => a.Left!.AimPose)
                .AddAction(a => a.Left!.GripPose)
                .AddAction(a => a.Left!.SqueezeClick)
                .AddAction(a => a.Left!.SqueezeValue)
                .AddAction(a => a.Left!.Button!.XClick)
                .AddAction(a => a.Left!.Button!.YClick)
                .AddAction(a => a.Left!.TriggerClick)
                .AddAction(a => a.Left!.TriggerValue));

            return self;
        }

        public static XrEngineAppBuilder UseRightController(this XrEngineAppBuilder self)
        {
            self.UseInputs<XrOculusTouchController>(bld => bld
                .AddAction(a => a.Right!.AimPose)
                .AddAction(a => a.Right!.GripPose)
                .AddAction(a => a.Right!.SqueezeValue)
                .AddAction(a => a.Right!.SqueezeClick)
                .AddAction(a => a.Right!.Button!.AClick)
                .AddAction(a => a.Right!.Button!.BClick)
                .AddAction(a => a.Right!.TriggerClick)
                .AddAction(a => a.Right!.TriggerValue));

            return self;
        }

        public static XrEngineAppBuilder AddRightPointer(this XrEngineAppBuilder self) => self.ConfigureApp(e =>
        {
            var inputs = e.Inputs as XrOculusTouchController;

            e.App.ActiveScene!.AddComponent(new XrInputPointer
            {
                PoseInput = inputs!.Right!.AimPose,
                RightButton = inputs!.Right!.SqueezeClick!,
                LeftButton = inputs!.Right!.TriggerClick!,
            });
        });


        public static XrEngineAppBuilder UseRayCollider(this XrEngineAppBuilder self) => self.ConfigureApp(e =>
        {
            var inputs = e.GetInputs<XrOculusTouchController>();

            var rayCol = e.App!.ActiveScene!.AddComponent(new XrRayCollider() { InputName = "RightAimPose" });
        });

        public static XrEngineAppBuilder UseHands(this XrEngineAppBuilder self) => self.ConfigureApp(e =>
        {
            e.App.ActiveScene!.AddChild(new OculusHandView() { HandType = HandEXT.RightExt });
            e.App.ActiveScene!.AddChild(new OculusHandView() { HandType = HandEXT.LeftExt });
        });

        public static XrEngineAppBuilder UseGrabbers(this XrEngineAppBuilder self) => self.UseLeftController().
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


        public static XrEngineAppBuilder UseSceneModel(this XrEngineAppBuilder self, bool arMode, bool addPhysics = true) => self.ConfigureApp(e =>
        {
            var model = new OculusSceneModel();
            model.AddPhysics = addPhysics;

            if (arMode)
                model.Material = new ColorMaterial { Color = new Color(1, 1, 1), ShadowIntensity = 0.7f, IsShadowOnly = true };

            e.App.ActiveScene!.AddChild(model);
        });

        public static XrEngineAppBuilder UsePhysics(this XrEngineAppBuilder self, PhysicsOptions options) => self.ConfigureApp(e =>
        {
            e.App.ActiveScene!.AddComponent(new PhysicsManager() { Options = options });
        });


        public static XrEngineAppBuilder UseInputs<TProfile>(this XrEngineAppBuilder self) where TProfile : new()
        {
            return self.UseInputs<TProfile>(a => a.AddAll());
        }

        public static XrEngineAppBuilder Configure(this XrEngineAppBuilder self, Action<XrEngineAppBuilder> configure)
        {
            configure(self);
            return self;
        }


        public static XrEngineAppBuilder UsePlatform<T>(this XrEngineAppBuilder self) where T : IXrEnginePlatform, new()
        {
            return self.UsePlatform(new T());
        }

        public static XrEngineAppBuilder SetRenderQuality(this XrEngineAppBuilder self, float resolutionScale, uint sampleCount)
        {
            self.Options.ResolutionScale = resolutionScale;
            self.Options.SampleCount = sampleCount;
            return self;
        }

        public static XrEngineAppBuilder UseOpenGL(this XrEngineAppBuilder self)
        {
            self.Options.Driver = GraphicDriver.OpenGL;
            return self;
        }

        public static XrEngineAppBuilder UseMultiView(this XrEngineAppBuilder self)
        {
            self.Options.RenderMode = XrRenderMode.MultiView;
            return self;
        }

        public static XrEngineAppBuilder UseStereo(this XrEngineAppBuilder self)
        {
            self.Options.RenderMode = XrRenderMode.Stereo;
            return self;
        }

        public static XrEngineAppBuilder UseFilamentOpenGL(this XrEngineAppBuilder self)
        {
            self.Options.Driver = GraphicDriver.FilamentOpenGL;
            return self;
        }

        public static XrEngineAppBuilder UseFilamentVulkan(this XrEngineAppBuilder self)
        {
            self.Options.Driver = GraphicDriver.FilamentVulkan;
            return self;
        }
    }
}
