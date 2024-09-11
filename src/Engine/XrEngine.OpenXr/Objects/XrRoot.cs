using OpenXr.Framework;
using System.Numerics;
using XrEngine.Audio;
using XrEngine.Gltf;
using XrMath;

namespace XrEngine.OpenXr
{
    public class XrRoot : Group3D
    {
        protected XrApp? _xrApp;


        public XrRoot()
        {
            Flags |= EngineObjectFlags.ChildGenerated;

            Name = "XrRoot";

            RightController = AddController("/user/hand/right/input/aim/pose", "Right Hand", "Models/MetaQuestTouchPlus_Right.glb");

            LeftController = AddController("/user/hand/left/input/aim/pose", "Left Hand", "Models/MetaQuestTouchPlus_Left.glb");

            Head = AddHead();
        }

        protected override void Start(RenderContext ctx)
        {
            if (XrApp.Current == null)
                throw new InvalidOperationException();

            _xrApp = XrApp.Current;

            base.Start(ctx);
        }

        protected Group3D AddHead()
        {
            var group = new Group3D
            {
                Name = "Head"
            };

            group.AddBehavior((_, ctx) =>
            {
                if (!_xrApp!.IsStarted)
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
            var group = new Group3D
            {
                Name = name
            };

            Object3D? model = null;

            group.AddBehavior((_, ctx) =>
            {
                var input = _xrApp?.Inputs.Values.FirstOrDefault(a => a.Path == path);

                if (input == null)
                    return;

                if (input.IsChanged && input.IsActive)
                {
                    var pose = (Pose3)input.Value;
                    group.Transform.Position = pose.Position;
                    group.Transform.Orientation = pose.Orientation;
                }

                if (model != null)
                    model.IsVisible = input.IsActive;
            });


            var assets = Context.Require<IAssetStore>();

            var fullPath = assets.GetPath(modelFileName);

            if (File.Exists(fullPath))
            {
                model = GltfLoader.LoadFile(fullPath);
                model.Transform.SetMatrix(Matrix4x4.Identity);
                model.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), MathF.PI);
                model.Transform.Position = new Vector3(-0.002f, 0.001f, 0.05f);
                model.Transform.SetScale(1.06f);
                model.Name = "Controller";

                group.AddChild(model);
            }

            AddChild(group);

            return group;
        }

        public Group3D? Head { get; protected set; }

        public Group3D? RightController { get; protected set; }

        public Group3D? LeftController { get; protected set; }
    }
}
