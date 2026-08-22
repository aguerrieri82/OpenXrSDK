using System.Numerics;
using XrEngine;
using XrEngine.Animation;
using XrEngine.Bullet;
using XrEngine.Gltf;
using XrEngine.OpenXr;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Skin")]
        public static XrEngineAppBuilder CreateSkin(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var mesh = (Group3D)GltfLoader.LoadFile(GetAssetPath("Models/CesiumMan.glb"), GltfOptions, GetAssetPath);
            mesh.Name = "mesh";

            scene.AddChild(mesh);

            mesh.Animate()
                .Name("Jump")
                .Target(a => a.WorldPosition)
                .Relative()
                .FromFunction(ComputeFunctions.Jump(
                    0,
                    Vector3.Normalize(new Vector3(0, 2, 1)),
                    intensity: 5))
                .Add();

            var control = mesh.Animate("Jump", new JumpOptions
            {

            });

            void ConfigureIk(Joint3D root)
            {
                static JointDof Dof(float min, float max, float rest = 0)
                {
                    const float DegToRad = MathF.PI / 180f;

                    return new JointDof
                    {
                        Enabled = true,
                        Min = min * DegToRad,
                        Max = max * DegToRad,
                        Rest = rest * DegToRad
                    };
                }

                static void Set3(Joint3D joint, float x, float y, float z)
                {
                    joint.DofX = Dof(-x, x);
                    joint.DofY = Dof(-y, y);
                    joint.DofZ = Dof(-z, z);
                }

                static void Set1(Joint3D joint, char axis, float min, float max)
                {
                    switch (axis)
                    {
                        case 'X':
                            joint.DofX = Dof(min, max);
                            break;
                        case 'Y':
                            joint.DofY = Dof(min, max);
                            break;
                        case 'Z':
                            joint.DofZ = Dof(min, max);
                            break;
                    }
                }

                void Visit(Joint3D joint)
                {
                    joint.DofX = default;
                    joint.DofY = default;
                    joint.DofZ = default;
                    joint.IsEffector = false;

                    var name = joint.Name ?? "";

                    if (name.Contains("torso_joint"))
                    {
                        Set3(joint, 25, 35, 25);
                    }
                    else if (name.Contains("neck_joint"))
                    {
                        Set3(joint, 40, 60, 40);
                        joint.IsEffector = name == "Skeleton_neck_joint_2";
                    }
                    else if (name.Contains("arm_joint_L__4") ||
                             name == "Skeleton_arm_joint_R")
                    {
                        Set3(joint, 120, 120, 120);
                    }
                    else if (name.Contains("arm_joint_L__3") ||
                             name.Contains("arm_joint_R__2"))
                    {
                        Set1(joint, 'X', 0, 150);
                    }
                    else if (name.Contains("arm_joint_L__2") ||
                             name.Contains("arm_joint_R__3"))
                    {
                        Set3(joint, 60, 60, 60);
                        joint.IsEffector = true;
                    }
                    else if (name.Contains("leg_joint_L_1") ||
                             name.Contains("leg_joint_R_1"))
                    {
                        Set3(joint, 100, 60, 60);
                    }
                    else if (name.Contains("leg_joint_L_2") ||
                             name.Contains("leg_joint_R_2"))
                    {
                        Set1(joint, 'X', 0, 150);
                    }
                    else if (name.Contains("leg_joint_L_3") ||
                             name.Contains("leg_joint_R_3"))
                    {
                        Set3(joint, 45, 30, 30);
                    }
                    else if (name.Contains("leg_joint_L_5") ||
                             name.Contains("leg_joint_R_5"))
                    {
                        joint.IsEffector = true;

                    }

                    foreach (var child in joint.Children.OfType<Joint3D>())
                        Visit(child);
                }

                Visit(root);
            }

            var ikRoot = mesh.FindByName<Joint3D>("Skeleton_torso_joint_1")!;
            var updater = ikRoot.AddComponent(new IkSkeletonUpdater());
            ConfigureIk(ikRoot);

            updater.Build();

            foreach (var effector in updater.Solver!.Effectors)
            {
                var target = mesh.AddChild(new TriangleMesh(new Sphere3D(0.05f, 10), new PbrMaterial() { Color = "#ff0000" })
                {
                    Name = effector.Name
                });

                target.WorldPosition = mesh.FindByName<Joint3D>(effector.Name!)!.WorldPosition;

                updater.SetTarget(effector, target);
            }

            return builder
                .UseApp(app)
                .UseEnvironmentHDR("res://asset/Envs/StudioTomoco.hdr")
                .ConfigureSampleApp();
        }
    }
}
