using OpenXr.Framework;
using System.Numerics;
using Xr.Engine.Gltf;
using Xr.Math;

namespace Xr.Engine.OpenXr
{
    public class XrRoot : Group3D
    {
        readonly XrApp _xrApp;


        public XrRoot(XrApp app)
        {
            _xrApp = app;

            RightController = AddController("/user/hand/right/input/aim/pose", "Right Hand", "Models/MetaQuestTouchPlus_Right.glb");

            LeftController = AddController("/user/hand/left/input/aim/pose", "Left Hand", "Models/MetaQuestTouchPlus_Left.glb");
        }

        protected Group3D? AddController(string path, string name, string modelFileName)
        {
            var input = _xrApp.Inputs.Values.FirstOrDefault(a => a.Path == path);
            if (input == null)
                return null;

            var group = new Group3D();
            group.Name = name;
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
            });

            var assets = Platform.Current!.AssetManager!;

            var fullPath = assets.FullPath(modelFileName);

            if (File.Exists(fullPath))
            {
                var model = GltfLoader.Instance.Load(fullPath, assets);
                model.Transform.SetMatrix(Matrix4x4.Identity);
                model.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), MathF.PI);
                model.Transform.Position = new Vector3(-0.002f, 0.001f, 0.05f);
                model.Transform.SetScale(1.1f);
                model.Name = "Right Controller";
                //var laser = model.FindByName<Object3D>("laser_begin");
                //model.Transform.Position = laser.Transform.Position;
                //model.Transform.Orientation = laser.Transform.Orientation;
                group.AddChild(model);
            }

            AddChild(group);
            return group;
        }

        public Group3D? RightController { get; }

        public Group3D? LeftController { get; }

    }
}
