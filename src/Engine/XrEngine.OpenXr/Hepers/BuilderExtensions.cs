using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using PhysX.Framework;
using Silk.NET.OpenXR;
using XrEngine.Objects;
using XrEngine.OpenGL;
using XrEngine.Physics;
using XrEngine.UI.Web;
using XrMath;

namespace XrEngine.OpenXr
{
    public enum ControllerHand
    {
        Left,
        Right
    }

    public class WebUIOptions
    {
        public TriangleMesh? DestMesh { get; set; }

        public string? DevServer { get; set; }

        public object[]? Bridges { get; set; }

        public string? AssetsPath { get; set; }
    }

    public static class BuilderExtensions
    {

        public static XrEngineAppBuilder AddWebUI(this XrEngineAppBuilder self,
            WebUIOptions options,
            Action<IWebBrowser>? configure = null) => self.ConfigureApp(e =>
        {
            var factory = Context.Require<IWebBrowserFactory>();

            if (options.DestMesh == null)
            {
                var scene = e.App.ActiveScene;

                options.DestMesh = scene!.AddChild(
                    new UIWebPanel(
                        e.Inputs!.Right.Button.AClick,
                        (PerspectiveCamera)scene.ActiveCamera!));
            }

            var browser = factory.CreateBrowser(new WebBrowserOptions
            {
                DestMesh = options.DestMesh,
                UseLocalUI = true,
                LocalAssetsPath = options.AssetsPath
            });

            if (options.Bridges != null)
            {
                var bridge = new WebBrowserBridge(browser);

                foreach (var obj in options.Bridges)
                    bridge.Register(obj);

                Context.Implement(bridge);
            }

            if (!string.IsNullOrWhiteSpace(options.DevServer) && XrPlatform.IsEditor)
                browser.NavigateAsync(options.DevServer);
            else
                browser.NavigateAsync("ui://main/");

            configure?.Invoke(browser);
        });

        public static XrEngineAppBuilder AddPassthrough(this XrEngineAppBuilder self, bool enabled = true) => self.ConfigureApp(e =>
        {
            if (!XrDevice.IsMetaQuest)
                return;
            if (!e.XrApp.Layers.List.OfType<XrPassthroughLayer>().Any())
                e.XrApp.Layers.List.Insert(0, new XrPassthroughLayer() { IsEnabled = enabled });
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

        public static XrEngineAppBuilder UseFloorTeleport(this XrEngineAppBuilder self, Scene3D scene)
        {
            var player = new TriangleMesh(Cube3D.Default, (Material)MaterialFactory.CreatePbr("#ff0000"))
            {
                IsVisible = false,
                Name = "Player"
            };

            player.Transform.SetScale(0.3f, 1.7f, 0.3f);
            player.AddComponent(new XrPlayer
            {
                Height = 0f
            });

            scene.AddChild(player);

            return self.UseTeleport(ControllerHand.Right, player, new FloorTeleportTarget());
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

                 Func<bool> trigger = () => curHand.ThumbstickY!.IsActive && curHand.ThumbstickY!.Value < -0.5f;

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

        public static XrEngineAppBuilder UseRayCollider(
            this XrEngineAppBuilder self,
            string pointerName = "RightController",
            bool parallel = false) => self.ConfigureApp(e =>
        {
            var inputs = e.GetInputs<XrOculusTouchController>();

            var rayCol = e.App!.ActiveScene!.AddComponent(new RayPointerCollider()
            {
                PointerName = pointerName,
                ParallelColliders = parallel,
            });
        });

        public static XrEngineAppBuilder UseHands(this XrEngineAppBuilder self) => self.ConfigureApp(e =>
        {
            if (!XrDevice.IsMetaQuest)
                return;
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

        public static XrEngineAppBuilder SetRenderQuality(this XrEngineAppBuilder self, float resolutionScale, uint sampleCount, bool useIntermediate = false)
        {
            self.Options.ResolutionScale = resolutionScale;
            self.Options.SampleCount = sampleCount;
            //self.Options.UseIntermediate = useIntermediate;

            if (self.Options.DriverOptions is GlRenderOptions glOpt)
                glOpt.UseResolve = useIntermediate;

            return self;
        }

        public static XrEngineAppBuilder EnableDebug(this XrEngineAppBuilder self, bool sync = false)
        {
            return self.ConfigureApp(e =>
            {
                e.App.Renderer.EnableDebug(sync ? RenderEngineDebug.Sync : RenderEngineDebug.None);
            });
        }

        public static XrEngineAppBuilder EnableDebugNotRelease(this XrEngineAppBuilder self, bool sync = false)
        {
            return self.ConfigureApp(e =>
            {
#if DEBUG
                e.App.Renderer.EnableDebug(sync ? RenderEngineDebug.Sync : RenderEngineDebug.None);
#endif
            });
        }

        public static XrEngineAppBuilder AddProfileOverlay(this XrEngineAppBuilder self)
        {
            return self.ConfigureApp(e => e.App.ActiveScene!.AddComponent<ProfileOverlay>());
        }

        public static XrEngineAppBuilder UseAngle(this XrEngineAppBuilder self)
        {
            self.Options.Driver = GraphicDriver.Angle;
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

        public static XrEngineAppBuilder SetXrOptions(this XrEngineAppBuilder self, Action<XrRenderOptions> options)
        {
            return self.ConfigureApp(e => options(e.XrApp.RenderOptions));
        }

        public static XrEngineAppBuilder SetAppOptions(this XrEngineAppBuilder self, Action<XrEngineAppOptions> options)
        {
            options(self.Options);
            return self;
        }

        public static XrEngineAppBuilder UseProjDepth(this XrEngineAppBuilder self, XrProjDepthMode mode, float scale = 0.5f)
        {
            self.Options.ProjDepthScale = scale;
            self.Options.ProjDepthMode = mode;
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

        public static XrEngineAppBuilder UseSpaceWarp(this XrEngineAppBuilder self, MotionVectorMode mode = MotionVectorMode.Shared)
        {
            return self.SetGlOptions(opt =>
            {
                opt.MotionVectorMode = mode;
            });
        }

        public static XrEngineAppBuilder UseEnvironmentMesh(this XrEngineAppBuilder self, uint size = 300u, bool occlude = true, bool receiveShadow = true)
        {
            return self.UseEnvironmentDepth()
                    .ConfigureApp(e =>
                    {
                        var mesh = new EnvDepthMesh(new Size2I(size, size));

                        mesh.Material.UseDepth = occlude;
                        mesh.Material.WriteDepth = occlude;
                        mesh.Material.ReceiveShadows = receiveShadow;

                        e.App.ActiveScene!.AddChild(mesh);
                    });

        }

        public static XrEngineAppBuilder UseEnvironmentShadow(this XrEngineAppBuilder self, Color shadowColor, float maxDistance = 4f)
        {
            self.ConfigureApp(e =>
            {
                if (e.App.Renderer is not OpenGLRender openGl)
                    return;

                openGl.Options.ShadowMap.UseVirtualReceiver = true;
                openGl.Options.ShadowMap.FrustumMaxDistance = maxDistance;
                //openGl.Options.ShadowMap.UpdateInterval = 0.1f;

                openGl.AddPass(new GlEnvDepthShadowPass(openGl)
                {
                    ShadowColor = shadowColor
                }, -1);

                var scene = e.App.ActiveScene!;

                scene.AddBehavior((_, ctx) =>
                {
                    var aclick = e.Inputs!.Right!.Button!.AClick!;
                    if (aclick.IsChanged && aclick.IsActive && aclick.Value)
                    {
                        var provider = scene.ActiveCamera!.Feature<IEnvDepthProvider>();
                        if (provider != null)
                            provider.Freeze = !provider.Freeze;
                    }
                });
            });
            return self;
        }

        public static XrEngineAppBuilder UseEnvironmentDepth(this XrEngineAppBuilder self)
        {
            if (XrPlatform.IsEditor)
            {
                Log.Error(self, "Environment Depth not ADDED in editor");
                return self;
            }

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

                if (camera != null && !camera.TryComponent<OculusEnvDepthProvider>(out var _))
                    camera.AddComponent(new OculusEnvDepthProvider(e.XrApp));
            });

            return self;
        }

        /*

        public static IWebBrowser AddWebView(this XrEngineAppBuilder builder)
        {

        }
        */

    }
}
