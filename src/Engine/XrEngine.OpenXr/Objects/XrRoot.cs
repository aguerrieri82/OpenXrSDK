using OpenXr.Framework;
using System.Numerics;
using XrEngine.Audio;
using XrEngine.Gltf;
using XrMath;

namespace XrEngine.OpenXr
{
    public class XrRoot : Group3D
    {
        readonly XrApp _xrApp;

        public XrRoot(XrApp app)
        {
            _xrApp = app;

            Name = "XrRoot";

            RightHand = AddController("/user/hand/right/input/aim/pose", "Right Hand", "Models/MetaQuestTouchPlus_Right.glb");

            LeftHand = AddController("/user/hand/left/input/aim/pose", "Left Hand", "Models/MetaQuestTouchPlus_Left.glb");

            Head = AddHead();
        }

        protected Group3D AddHead()
        {
            var group = new Group3D
            {
                Name = "Head"
            };

            group.AddBehavior((_, ctx) =>
            {
                if (!_xrApp.IsStarted)
                    return;

                var head = _xrApp.LocateSpace(_xrApp.Head, _xrApp.Stage, _xrApp.LastFrameTime);

                if (head.IsValid)
                {
                    group.Transform.Position = head.Pose.Position;
                    group.Transform.Orientation = head.Pose.Orientation;
                }
            });

            group.AddComponent<AudioReceiver>();

            AddChild(group);

            return group;
        }

        protected Group3D AddController(string path, string name, string modelFileName)
        {
            var input = _xrApp.Inputs.Values.FirstOrDefault(a => a.Path == path);

            var group = new Group3D
            {
                Name = name
            };

            if (input != null)
            {
                Object3D? model = null;

                group.AddBehavior((_, ctx) =>
                {
                    input = _xrApp.Inputs.Values.FirstOrDefault(a => a.Path == path)!;

                    if (input.IsChanged && input.IsActive)
                    {
                        var pose = (Pose3)input.Value;
                        group.Transform.Position = pose.Position;
                        group.Transform.Orientation = pose.Orientation;
                        group.UpdateWorldMatrix(true, false);
                    }

                    if (model != null)
                        model.IsVisible = input.IsActive;
                });

                var assets = Platform.Current!.AssetManager!;

                var fullPath = assets.GetFsPath(modelFileName);

                if (File.Exists(fullPath))
                {
                    model = GltfLoader.Instance.Load(fullPath, assets);
                    model.Transform.SetMatrix(Matrix4x4.Identity);
                    model.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), MathF.PI);
                    model.Transform.Position = new Vector3(-0.002f, 0.001f, 0.05f);
                    model.Name = "Controller";
                    group.AddChild(model);
                }

                AddChild(group);
            }

            return group;
        }

        public Group3D Head { get; }

        public Group3D RightHand { get; }

        public Group3D LeftHand { get; }
    }
}
