using PhysX.Framework;
using System.Numerics;
using System.Xml.Linq;
using XrEngine;
using XrEngine.Components;
using XrEngine.Gltf;
using XrEngine.Helpers;
using XrEngine.OpenXr;
using XrEngine.Physics;
using XrMath;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Car")]
        public static XrEngineAppBuilder CreateCar(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();
            var scene = app.ActiveScene!;
            scene.ActiveCamera!.BackgroundColor = "#7C93DB";
            scene.Id = Guid.Parse("9692f695-f53c-40c4-900a-d17ac94302d8");

            //Physics
            var pm = scene.AddComponent(new PhysicsManager(60));
            pm.SetCollideGroup(RigidBodyGroup.Group1, CollideGroup.Never);
            pm.SetCollideGroup(RigidBodyGroup.Group2, CollideGroup.Always);

            scene.AddComponent(new InputPhysicsForce
            {
                InputName = "RightGripPose",
                HandlerName = "RightSqueezeClick",
                HapticName = "RightHaptic",
                Tollerance = 0.01f,
                Factor = 0.1f
            });

            scene.AddComponent(new InputPhysicsForce
            {
                InputName = "LeftGripPose",
                HandlerName = "LeftSqueezeClick",
                HapticName = "LeftHaptic",
                Factor = 0.1f
            });

            //Material
            var leather = (IPbrMaterial)LoadMaterial("Materials/xjekdbj_tier_2.gltf");
            leather.Color = "#FF6400FF";
            leather.DoubleSided = true;
            leather.Color *= 2f;

            var car = (Group3D)GltfLoader.LoadFile(GetAssetPath("car.glb"), GltfOptions, GetAssetPath);
            car.Name = "car";
            car.WorldPosition = new Vector3(0, 0.4f, 0);
            var bodyMeshes = new HashSet<TriangleMesh>();

            //Fix model
            foreach (var mat in car.DescendantsOrSelf().OfType<TriangleMesh>().SelectMany(a => a.Materials).Distinct())
            {
                if (mat is IPbrMaterial pbr)
                {

                    if (mat.Name!.Contains("glass"))
                    {
                        pbr.Color = "#00000020";
                        pbr.Alpha = AlphaMode.Blend;
                        pbr.AlphaCutoff = 0.2f;
                    }
                    if (mat.Name!.Contains("paint"))
                    {
                        pbr.Color = "#FF0100FF";
                        pbr.Roughness = 0.15f;
                        foreach (var host in mat.Hosts)
                            bodyMeshes.Add((TriangleMesh)host);
                    }
                }
            }

            //Optimize  
            foreach (var mesh in car.DescendantsOrSelf().OfType<TriangleMesh>())
            {
                Log.Info(typeof(SampleScenes), $"Optimizing {mesh.Name}");

                if (mesh.Name != "reflect_mirrors.003" && mesh.Name != "reflect_mirror_int.003")
                    XrEngine.MeshOptimizer.Simplify(mesh.Geometry!, 0.4f, 0.005f);

                XrEngine.MeshOptimizer.OptimizeVertexCache(mesh.Geometry!);
                XrEngine.MeshOptimizer.OptimizeOverdraw(mesh.Geometry!, 1.05f);
                XrEngine.MeshOptimizer.OptimizeVertexFetch(mesh.Geometry!);

                if (mesh.Name == "leather_armrest.007")
                {
                    mesh.Materials.Clear();
                    mesh.Materials.Add((Material)leather);
                }
            }

            car.UpdateBounds(true);

            var scale = car.FindByName<Object3D>("body.003")!.Transform.Matrix;

            //Simulation
            var model = new CarModelV2
            {
                WheelFL = car.GroupByName("wheel.Ft.L.003", "wheelbrake.Ft.L.003"),
                WheelFR = car.GroupByName("wheel.Ft.R.003", "wheelbrake.Ft.R.003"),
                WheelBL = car.GroupByName("wheel.Bk.L.003", "wheelbrake.Bk.R.003"),
                WheelBR = car.GroupByName("wheel.Bk.R.003", "wheelbrake.Bk.R.001"),
                CarBody = car.GroupByName("body.003"),
                SteeringWheel = car.GroupByName("leatherB_steering.003", "chrome_steering.003", "chrome_logo_steering.003", "texInt_steering.003"),
                CarBodyCollisionMeshes = bodyMeshes,
                UseSteeringPhysics = false,
                GearBoxPose = new Pose3()
                {
                    Position = new Vector3(-0.1f, 0.61f, 0.1f),
                    Orientation = Quaternion.Normalize(new Quaternion(1, 0f, 0f, 0.82f))
                },
                SeatLocalPose = new Pose3
                {
                    Position = new Vector3(-0.4f, 1.1f, 0.2f),
                    Orientation = Quaternion.Identity
                },
                SteeringLocalPose = new Pose3
                {
                    Position = new Vector3(-0.428f, 0.926f, -0.062f),
                    Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -19f / 180 * MathF.PI)
                },
            };

            var mirror = car.FindByName<TriangleMesh>("plasticInt_mirror_int.003")!;

            var splitter = new MeshSplitter(mirror)
            {
                SplittedName = "plasticInt_mirror_int_body-mirror",
                FullIntersection = true,
                Orientation = new Quaternion(0, 0, 0, 1),
                Bounds = new Vector3(300, 97, 100),
                Origin = new Vector3(24, -38, -42f)
            };

            splitter.ExecuteSplit();

            var mainBody = (TriangleMesh)((Group3D)((Group3D)model.CarBody).Children[0]).Children[0];

            splitter = new MeshSplitter(mainBody)
            {
                SplittedName = "mirror_left",
                FullIntersection = true,
                Orientation = new Quaternion(0, 0, 0, 1),
                Bounds = new Vector3(300, 150, 230),
                Origin = new Vector3(-1028, 470, -390)
            };
            splitter.ExecuteSplit();

            splitter = new MeshSplitter(mainBody)
            {
                SplittedName = "mirror_right",
                FullIntersection = true,
                Orientation = new Quaternion(0, 0, 0, 1),
                Bounds = new Vector3(300, 150, 230),
                Origin = new Vector3(940.2f, 470, -390)
            };
            splitter.ExecuteSplit();

            mirror = car.FindByName<TriangleMesh>("plastic_mirrors.003")!;
            splitter = new MeshSplitter(mirror)
            {
                SplittedName = "plastic_mirrors_right",
                FullIntersection = true,
                Orientation = new Quaternion(0, 0, 0, 1),
                Bounds = new Vector3(1000, 1000, 1000),
                Origin = new Vector3(1000, 0, -500)
            };
            splitter.ExecuteSplit();

            mirror = car.FindByName<TriangleMesh>("glassClear_mirrors.003")!;
            splitter.SplittedName = "glassClear_mirrors_right";
            splitter.Attach(mirror);
            splitter.ExecuteSplit();

            mirror = car.FindByName<TriangleMesh>("reflect_mirrors.003")!;
            splitter.SplittedName = "reflect_mirrors_right";
            splitter.Attach(mirror);
            splitter.ExecuteSplit();

            model.AddMirror(car.GroupByName(scale, "reflect_mirror_int.003", "plasticInt_mirror_int_body-mirror"),
                new Ray3(new Vector3(-50, 640, -172), new Vector3(0, 0, 1)));
            model.AddMirror(car.GroupByName(scale, "reflect_mirrors.003", "glassClear_mirrors.003", "plastic_mirrors.003", "mirror_left"),
                new Ray3(new Vector3(-850, 390, -300), new Vector3(1, -0.87f, 0)));
            model.AddMirror(car.GroupByName(scale, "reflect_mirrors_right", "glassClear_mirrors_right", "plastic_mirrors_right", "mirror_right"),
              new Ray3(new Vector3(800, 390, -300), new Vector3(1, 0.87f, 0)));

            car.AddComponent(model);

            var checkerMat = (Material)MaterialFactory.CreatePbr(TextureFactory.CreateChecker());
            var staticMat = new PhysicsMaterialInfo()
            {
                StaticFriction = 1f,
                DynamicFriction = 1f,
                Restitution = 0.2f
            };

            //Floor
            var floor = new TriangleMesh(new Cube3D(new Vector3(20, 0.01f, 20)), checkerMat);
            floor.Name = "floor";
            floor.Transform.SetPositionY(-0.005f);
            floor.Geometry!.ScaleUV(new Vector2(20, 20));
            floor.AddComponent(new RigidBody
            {
                Type = PhysicsActorType.Static,
                MaterialInfo = staticMat
            });

            //Ramp
            var ramp = new TriangleMesh(new Cube3D(new Vector3(20, 0.01f, 20)), checkerMat);
            ramp.Name = "ramp";
            ramp.SetWorldPoseIfChanged(new Pose3()
            {
                Position = new Vector3(0f, 2.565f, -19.36f),
                Orientation = new Quaternion(0.12981941f, 0f, 0f, 0.99153763f)
            });
            ramp.Geometry!.ScaleUV(new Vector2(20, 20));
            ramp.AddComponent(new RigidBody
            {
                Type = PhysicsActorType.Static,
                MaterialInfo = staticMat,
                Configure = rb =>
                {
        
                }
            });

            //Wall
            var wall = new TriangleMesh(new Cube3D(new Vector3(5, 3, 0.5f)), checkerMat);
            wall.Name = "wall";
            wall.Transform.Position = new Vector3(0, 1.5f, -5f);
            wall.Geometry!.ScaleUV(new Vector2(5, 3));
            wall.AddComponent(new RigidBody
            {
                Type = PhysicsActorType.Static,
                MaterialInfo = staticMat
            });

            //Add children
            scene.AddChild(floor);
            scene.AddChild(ramp);
            scene.AddChild(wall);
            scene.AddChild(car);

            //Create model
            model.Create();
            model.CarBody.Name = "car-body";

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .SetGlOptions(opt =>
                {
                    opt.UsePlanarReflection = true;
                })
                .ConfigureSampleApp(false)
                .ConfigureApp(a =>
                {
                    a.XrApp.UseLocalSpace = true;
                    model.ConfigureInput(a.Inputs!);

                    //Point light
                    var pl = scene.Descendants<PointLight>().First();
                    pl.IsVisible = true;
                    pl.Specular = new Color(0.1f, 0.1f, 0.1f, 1);
                    pl.Intensity = 1f;
                });
        }
    }
}
