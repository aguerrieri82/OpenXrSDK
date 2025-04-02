using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using PhysX.Framework;
using Silk.NET.OpenXR;
using XrEngine.Objects;
using XrEngine.OpenGL;
using XrEngine.Physics;

namespace XrEngine.OpenXr
{
    public enum ControllerHand
    {
        Left,
        Right
    }

    public static class XrEngineAppExtensions
    {

        public static XrEngineAppBuilder AddPassthrough(this XrEngineAppBuilder self) => self.ConfigureApp(e =>
        {
            if (!e.XrApp.Layers.List.OfType<XrPassthroughLayer>().Any())
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

        public static XrEngineAppBuilder UseTeleport(this XrEngineAppBuilder self, ControllerHand hand, Object3D dest, ITeleportTarget? target = null)
        {
            self.UseInputs<XrOculusTouchController>(bld =>
            {
                if (hand == ControllerHand.Left)
                    bld.AddAction(a => a.Left!.ThumbstickY);
                else
                    bld.AddAction(a => a.Right!.ThumbstickY);
            });

            self.ConfigureApp(e =>
             {
                 var inputs = e.GetInputs<IXrBasicInteractionProfile>();
                 XrInteractionProfileHand curHand = hand == ControllerHand.Left ? inputs.Left! : inputs.Right!;

                 var pointer = new XrInputPointer
                 {
                     PoseInput = curHand.AimPose,
                     RightButton = curHand.SqueezeClick!,
                     LeftButton = curHand.TriggerClick!,
                 };

                 var trigger = () => curHand.ThumbstickY!.IsActive && curHand.ThumbstickY!.Value < -0.8f;

                 target ??= e.App.ActiveScene!.AddComponent<SceneTeleportTarget>();

                 var teleport = new InputTeleport()
                 {
                     Pointer = pointer,
                     IsTriggerActive = trigger,
                     Target = target
                 };

                 dest!.AddComponent(teleport);
             });
            return self;
        }

        public static XrEngineAppBuilder AddRightPointer(this XrEngineAppBuilder self) => self.ConfigureApp(e =>
        {
            var inputs = e.Inputs;

            e.App.ActiveScene!.AddComponent(new XrInputPointer
            {
                PoseInput = inputs!.Right!.AimPose,
                RightButton = inputs!.Right!.SqueezeClick!,
                LeftButton = inputs!.Right!.TriggerClick!,
                AButton = inputs!.Right!.Button!.AClick!,
                BButton = inputs!.Right!.Button!.BClick!,
                Name = "RightController"
            });
        });


        public static XrEngineAppBuilder UseRayCollider(this XrEngineAppBuilder self, string pointerName = "RightController") => self.ConfigureApp(e =>
        {
            var inputs = e.GetInputs<XrOculusTouchController>();

            var rayCol = e.App!.ActiveScene!.AddComponent(new RayPointerCollider() { PointerName = pointerName });
        });

        public static XrEngineAppBuilder UseHands(this XrEngineAppBuilder self) => self.ConfigureApp(e =>
        {
            e.App.ActiveScene!.AddChild(new OculusHandView() { HandType = HandEXT.RightExt });
            e.App.ActiveScene!.AddChild(new OculusHandView() { HandType = HandEXT.LeftExt });
        });

        public static XrEngineAppBuilder UseGrabbers(this XrEngineAppBuilder self) => self.
            UseLeftController().
            UseRightController().
            ConfigureApp(e =>
        {
            var inputs = e.GetInputs<XrOculusTouchController>();

            e.App!.ActiveScene!.AddComponent(new InputGrabber(
                inputs.Right!.GripPose!,
                null,
                inputs.Right!.SqueezeValue!,
                inputs.Right!.TriggerValue!));

            e.App!.ActiveScene!.AddComponent(new InputGrabber(
                inputs.Left!.GripPose!,
                null,
                inputs.Left!.SqueezeValue!,
                inputs.Left!.TriggerValue!));

            foreach (var hand in e.App.ActiveScene.Descendants<OculusHandView>())
                hand.AddComponent(new HandGrabber());
        });


        public static XrEngineAppBuilder UseSceneMesh(this XrEngineAppBuilder self, bool arMode, bool addPhysics = true) => self.ConfigureApp(e =>
        {
            var sceneView = new OculusSceneView();

            var factory = (DefaultSceneModelFactory)sceneView.Factory;

            Material? material = null;
            if (arMode)
                material = new ShadowOnlyMaterial();

            factory.AddMesh(material, addPhysics);


            e.App.ActiveScene!.AddChild(sceneView);
        });

        public static XrEngineAppBuilder UsePhysics(this XrEngineAppBuilder self, PhysicsOptions options) => self.ConfigureApp(e =>
        {
            e.App.ActiveScene!.AddComponent(new PhysicsManager() { Options = options });
        });


        public static XrEngineAppBuilder UseInputs<TProfile>(this XrEngineAppBuilder self) where TProfile : IXrBasicInteractionProfile, new()
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

        public static XrEngineAppBuilder UseOpenGL(this XrEngineAppBuilder self, Action<GlRenderOptions> options)
        {
            self.Options.Driver = GraphicDriver.OpenGL;
            self.Options.DriverOptions = new GlRenderOptions();
            options((GlRenderOptions)self.Options.DriverOptions);
            return self;
        }

        public static XrEngineAppBuilder SetGlOptions(this XrEngineAppBuilder self, Action<GlRenderOptions> options)
        {
            self.Options.DriverOptions ??= new GlRenderOptions();
            options((GlRenderOptions)self.Options.DriverOptions);
            return self;
        }

        public static XrEngineAppBuilder UseMultiView(this XrEngineAppBuilder self)
        {
            self.Options.RenderMode = XrRenderMode.MultiView;
            PlanarReflection.IsMultiView = true;
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

        public static XrEngineAppBuilder AddXrRoot(this XrEngineAppBuilder self)
        {
            self.ConfigureApp(app =>
            {
                app.App.ActiveScene!.AddChild(new XrRoot());
            });

            return self;
        }

        public static XrEngineAppBuilder UseSpaceWarp(this XrEngineAppBuilder self)
        {
            self.ConfigureApp(e =>
            {
                if (e.App.Renderer is not OpenGLRender openGl)
                    throw new NotSupportedException("Space warp is only supported on OpenGL");

                if (e.XrApp.RenderOptions.RenderMode == XrRenderMode.MultiView)
                {
                    openGl.AddPass(new GlMotionVectorPass(openGl, e.XrApp, 0), 0);
                    openGl.AddPass(new GlMotionVectorPass(openGl, e.XrApp, 1), 1);
                }
                else
                    openGl.AddPass(new GlMotionVectorPass(openGl, e.XrApp, -1), 0);
            });

            return self;
        }


        public static XrEngineAppBuilder UseEnvironmentDepth(this XrEngineAppBuilder self)
        {
            self.ConfigureApp(e =>
            {
                if (e.App.Renderer is not OpenGLRender openGl)
                    return;

                var passTh = e.XrApp.Layers.List.OfType<XrPassthroughLayer>().FirstOrDefault();
                if (passTh == null)
                {
                    passTh = new XrPassthroughLayer();
                    e.XrApp.Layers.List.Insert(0, passTh);
                }

                var camera = e.App.ActiveScene?.ActiveCamera;
                camera?.AddComponent(new OculusEnvDepthProvider(e.XrApp));
            });
            return self;
        }

    }
}
