using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using System.Numerics;
using XrEngine.Audio;
using XrEngine.Gltf;
using XrMath;

namespace XrEngine.OpenXr
{
    public class XrRoot : Group3D
    {
        protected XrApp _xrApp;
        private bool _isInit;

        public XrRoot()
        {
            _xrApp = XrApp.Current ?? throw new InvalidOperationException();

            Flags |= EngineObjectFlags.ChildrenGenerated | EngineObjectFlags.DisableNotifyChangedScene;

            Name = "XrRoot";

            RightController = AddController("/user/hand/right/input/aim/pose", "Right Hand", "Models/right_controller.glb");

            LeftController = AddController("/user/hand/left/input/aim/pose", "Left Hand", "Models/left_controller.glb");

            Head = AddHead();

            SceneRoot = AddSceneRoot();
        }

        public override void Update(RenderContext ctx)
        {
            if (_xrApp.IsStarted && !_isInit)
            {
                if (!_xrApp.TryPlugin<OculusXrPlugin>(out var oculus))
                    return;

                if (oculus != null)
                {
                    _ = Task.Run(async () =>
                    {
                        var anchors = await oculus.GetAnchorsAsync(new XrAnchorFilter
                        {
                            Components = XrAnchorComponent.All,
                            Labels = ["FLOOR"]
                        });

                        var floor = anchors.FirstOrDefault(a => a.Labels != null && a.Labels.Contains("FLOOR"));

                        if (floor == null)
                            return;

                        await EngineApp.MainThread;

                        SceneRoot.AddComponent(new XrAnchorUpdate()
                        {
                            Space = new Space(floor.Space),
                            UpdateInterval = TimeSpan.FromMilliseconds(300),
                            LogChanges = true
                        });
                    });
                }

                Head?.AddComponent(new XrAnchorUpdate()
                {
                    Space = _xrApp.Head
                });

                _isInit = true;
            }

            base.Update(ctx);
        }

        protected Group3D AddSceneRoot()
        {
            var group = new Group3D()
            {
                Name = "SceneRoot"
            };

            AddChild(group);

            return group;
        }

        protected Group3D AddHead()
        {
            var group = new Group3D
            {
                Name = "Head"
            };

            group.AddComponent<AudioReceiver>();

            AddChild(group);

            return group;
        }

        protected Group3D AddController(string path, string name, string modelFileName)
        {
            var group = new Group3D
            {
                Name = name,
            };

            Group3D? model = null;

            IXrInput? input = null;

            group.AddBehavior((_, ctx) =>
            {
                input ??= _xrApp.Inputs.Values.FirstOrDefault(a => a.Path == path);

                if (input == null)
                    return;

                if (input.IsChanged && input.IsActive)
                {
                    var pose = (Pose3)input.Value;
                    group.WorldPosition = pose.Position;
                    group.WorldOrientation = pose.Orientation;
                }
                /*
                if (model != null)
                    model.IsVisible = input.IsActive;*/

            });

            var assets = Context.Require<IAssetStore>();

            var fullPath = assets.GetPath(modelFileName);

            if (File.Exists(fullPath))
            {
                model = (Group3D)GltfLoader.LoadFile(fullPath);

                model.SetWorldPose(new Pose3()
                {
                    Position = new Vector3(0f, 0f, 0.049999997f),
                    Orientation = new Quaternion(0f, 1f, 0f, -4.371139E-08f)
                });

                model.Transform.SetScale(0.01f * 1.06f);
                model.Name = "Controller";

                model.Descendants<Joint3D>()
                    .First(a => a.Name!.EndsWith("oculus_controller_world"))
                    .Transform.Orientation = Quaternion.Identity;

                group.AddChild(model);

            }

            AddChild(group);

            return group;
        }

        public Vector3 ReferenceFramePos
        {
            get => XrApp.Current!.ReferenceFrame.Position;

            set => XrApp.Current!.ReferenceFrame = new Pose3
            {
                Position = value,
                Orientation = XrApp.Current!.ReferenceFrame.Orientation
            };
        }

        public Group3D? Head { get; }

        public Group3D SceneRoot { get; }

        public Group3D? RightController { get; }

        public Group3D? LeftController { get; }
    }
}
