using CanvasUI;
using OpenXr.Framework.Oculus;
using PhysX.Framework;
using RoomDesigner.Game;
using System.Numerics;
using XrEngine;
using XrEngine.Audio;
using XrEngine.Components;
using XrEngine.Gltf;
using XrEngine.Objects;
using XrEngine.OpenXr;
using XrEngine.Physics;
using XrEngine.UI;
using XrMath;
using System.Xml.Linq;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        static readonly GltfLoaderOptions GltfOptions = new()
        {
            ConvertColorTextureSRgb = true,
        };

        static string GetAssetPath(string name)
        {
            return Context.Require<IAssetStore>().GetPath(name);
        }

        static EngineApp CreateBaseScene()
        {
            var app = new EngineApp();

            var scene = new Scene3D();

            scene.AddComponent<AudioSystem>();

            scene.AddComponent<DebugGizmos>();

            scene.AddComponent<ShadowController>();

            scene.AddComponent<ResolveController>();

            scene.AddChild(new SunLight()
            {
                Name = "sun-light",
                Intensity = 1.0f,
                Direction = new Vector3(-0.1f, -0.9f, -0.15f).Normalize(),
                IsVisible = true
            });

            var pl1 = scene.AddChild(new PointLight());
            pl1.Transform.Position = new Vector3(0, 2, 0);
            pl1.Intensity = 0.3f;

            var pl2 = scene.AddChild(new PointLight());
            pl2.Name = "point-light-2";
            pl2.Transform.Position = new Vector3(0, -2, 0);
            pl2.Intensity = 0.3f;

            scene.AddChild(new PlaneGrid(6f, 12f, 2f));

            var camera = new PerspectiveCamera
            {
                Far = 100f,
                Near = 0.01f,
                BackgroundColor = new Color(0, 0, 0, 0),
                Exposure = 1
            };

            camera.LookAt(new Vector3(1, 1.7f, 1), new Vector3(0, 0, 0), new Vector3(0, 1, 0));

            scene.ActiveCamera = camera;

            app.OpenScene(scene);

            return app;
        }

        public static XrEngineAppBuilder UseEnvironmentHDR(this XrEngineAppBuilder builder, string assetPath, bool showEnv = false) 
        {
            return builder
            .ConfigureApp(e =>
            {
                var scene = e.App.ActiveScene!;

                scene.PerspectiveCamera.Exposure = 1.0f;

                var envView = scene.AddChild<EnvironmentView>();
                envView.IsVisible = showEnv;

                var light = scene.AddChild<ImageLight>();
                light.Intensity = 1f;

                foreach (var l in scene.Descendants<Light>())
                {
                    if (l != light)
                        l.IsVisible = false;
                }

                light.LoadPanorama(assetPath);
            });
        }

        public static XrEngineAppBuilder UseShadows(this XrEngineAppBuilder builder)
        {
            return builder
                .SetGlOptions(opt =>
                {
                    opt.ShadowMap.UseShadowSampler = true;
                    opt.ShadowMap.UseVirtualReceiver = true;
                    opt.ShadowMap.FrustumMaxDistance = 4f;
                    opt.ShadowMap.Mode = ShadowMapMode.PCF;
                    opt.ShadowMap.Size = 1024;
                })
                .UseInputs<XrOculusTouchController>(a => a
                           .AddAction(b => b.Right!.Thumbstick))
                .ConfigureApp(e =>
                {
                    var scene = e.App.ActiveScene!;

                    var sun = scene.Descendants<SunLight>().FirstOrDefault();

                    if (sun == null)
                        return;

                    var azimuth = 0.0f;
                    var tilt = 0.0f;

                    const float azimuthSpeed = 0.055f;
                    const float tiltSpeed = 0.035f;
                    const float maxTilt = MathF.PI * 0.49f;

                    sun.IsVisible = true;
                    sun.CastShadows = true;

                    var view = scene.AddChild(new SunLightView(sun)
                    {
                        UseRoof = false
                    });

                    view.WorldPosition = new Vector3(0, 2f, 0);

                    scene.AddBehavior((_, ctx) =>
                    {
                        var thumb = e.Inputs!.Right!.Thumbstick;

                        if (thumb!.IsActive && thumb.IsChanged)
                        {
                            var v = thumb.Value;

                            if (MathF.Abs(v.X) > MathF.Abs(v.Y))
                            {
                                azimuth += v.X * azimuthSpeed;
                            }
                            else
                            {
                                tilt += v.Y * tiltSpeed;
                                tilt = Math.Clamp(tilt, 0.0f, maxTilt);
                            }

                            var sinTilt = MathF.Sin(tilt);
                            var cosTilt = MathF.Cos(tilt);

                            sun.Direction = Vector3.Normalize(new Vector3(
                                MathF.Sin(azimuth) * sinTilt,
                               -cosTilt,
                                MathF.Cos(azimuth) * sinTilt
                            ));
                        }

                    });
                });
        }

        public static XrEngineAppBuilder UseDefaultHDR(this XrEngineAppBuilder builder)
        {
            if (DefaultHDR == null)
                DefaultHDR = "res://asset/Envs/pisa.hdr";
            return builder.UseEnvironmentHDR(DefaultHDR, DefaultShowHDR);
        }

        public static XrEngineAppBuilder UseClickMoveFront(this XrEngineAppBuilder builder, Object3D obj, float distance = 0.5f)
        {
            return builder.ConfigureApp(e =>
            {
                var inputs = e.GetInputs<XrOculusTouchController>();

                obj.AddBehavior((_, _) =>
                {
                    var click = inputs.Right.Button.AClick;
                    if (click.IsChanged && click.Value)
                    {
                        var scene = obj.Scene!;
                        obj.WorldPosition = scene.ActiveCamera!.WorldPosition + scene.ActiveCamera.Forward * distance;
                        obj.WorldOrientation = scene.ActiveCamera!.WorldOrientation;
                    }
                });
            });
        }

        public static XrEngineAppBuilder RemovePlaneGrid(this XrEngineAppBuilder builder) => builder.ConfigureApp(e =>
        {
            var grid = e.App.ActiveScene!.Descendants<PlaneGrid>().FirstOrDefault();
            if (grid != null)
                grid.IsVisible = false;
        });

        public static XrEngineAppBuilder AddPanel(this XrEngineAppBuilder builder, UIRoot uiRoot, bool forceOverlay = false, bool noOverlay = false)
        {
            var panel = new Window3D
            {
                Name = "UI Panel",
                Size = new Size2(0.8f, 0.5f),
                DpiScale = 1.6f,
                Content = uiRoot,
                WorldPosition = new Vector3(0, 1, 0),
            };

            return builder
                .UseClickMoveFront(panel, 0.5f)
                .ConfigureApp(e =>
                {
                    e.App.ActiveScene!.AddChild(panel);

                    if (!noOverlay && (XrPlatform.IsAndroid || forceOverlay))
                        panel.CreateOverlay(e.XrApp);
                });
        }

        public static XrEngineAppBuilder AddFloorShadow(this XrEngineAppBuilder builder, float size = 4, bool showShadowMap = false)
        {
            var floor = new TriangleMesh(new Cube3D(new Vector3(size, 0.01f, size)));
            floor.Name = "Floor";
            floor.Materials.Add(new ShadowOnlyMaterial
            {
                Name = "FloorMaterial",
                ShadowColor = new Color(1f, 0.1f, 0.1f, 0.7f),
            });

            floor.AddComponent<BoxCollider>();
            floor.AddComponent(new RigidBody()
            {
                Type = PhysicsActorType.Static,
            });

            floor.Transform.SetPositionY(-0.01f / 2.0f);

            TriangleMesh? depth = null;

            if (showShadowMap)
            {
                var depthView = new DepthViewMaterial();
                var texView = new TextureMaterial();

                depth = new TriangleMesh(Quad3D.Default, depthView);
                depth.Materials.Add(texView);

                depth.Transform.SetPositionY(1);

                depth.Name = "Depth";

                depth.AddBehavior((_, _) =>
                {
                    var sp = depth.Scene!.App!.Renderer.Feature<IShadowMapProvider>()!;

                    if (sp.Options.Mode == ShadowMapMode.VSM)
                    {
                        texView.IsEnabled = true;
                        depthView.IsEnabled = false;
                        if (texView.Texture == null)
                        {
                            texView.Texture = sp.ShadowMap;
                            depthView.NotifyChanged(ChangeType.Render);
                        }
                    }
                    else
                    {
                        texView.IsEnabled = false;
                        depthView.IsEnabled = true;

                        if (depthView.Texture == null)
                        {
                            depthView.Texture = sp.ShadowMap;
                            depthView.NotifyChanged(ChangeType.Render);
                        }

                        if (depthView.Camera == null)
                        {
                            depthView.Camera = sp.LightCamera;
                            depthView.NotifyChanged(ChangeType.Render);
                        }
                    }
                });
            }

            builder.ConfigureApp(e =>
            {
                e.App.ActiveScene!.AddChild(floor);
                if (depth != null)
                    e.App.ActiveScene!.AddChild(depth);

                var light = e.App.ActiveScene!.Descendants<DirectionalLight>().FirstOrDefault();
                if (light != null)
                {
                    light.CastShadows = true;
                    light.IsVisible = true;
                }
            });

            return builder;
        }

        public static XrEngineAppBuilder AddPanel<T>(this XrEngineAppBuilder builder) where T : UIRoot, new()
        {
            return builder.AddPanel(new T());
        }

        public static XrEngineAppBuilder ConfigureSampleApp(this XrEngineAppBuilder builder, bool usePt = true)
        {
            builder.AddXrRoot()
                   .UseHands()
                   .UseLeftController()
                   .UseRightController()
                   .AddRightPointer()
                   .UseInputs<XrOculusTouchController>(a => a
                       .AddAction(b => b.Right!.Thumbstick)
                       .AddAction(b => b.Right!.Haptic)
                       .AddAction(b => b.Left!.Haptic))
                   .UseRayCollider()
                   .UseGrabbers();

            if (IsEditor)
            {
                //usePt = false;
                Log.Error(builder, "Passtrhout not ADDED in editor");
            }

            if (usePt)
                builder.AddPassthrough();

            return builder;
        }

        static Material LoadMaterial(string url)
        {
            var gltf = (TriangleMesh)GltfLoader.LoadFile(GetAssetPath(url), GltfOptions);
            return gltf.Materials[0];
        }

        public static bool IsEditor => Context.Require<IXrEnginePlatform>().Name == "Editor";

        public static string? DefaultHDR { get; set; }

        public static bool DefaultShowHDR { get; set; }
    }
}
