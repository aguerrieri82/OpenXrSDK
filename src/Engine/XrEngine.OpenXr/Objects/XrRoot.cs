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

            Flags |= EngineObjectFlags.ChildGenerated | EngineObjectFlags.DisableNotifyChangedScene;

            Name = "XrRoot";

            RightController = AddController("/user/hand/right/input/aim/pose", "Right Hand", "Models/MetaQuestTouchPlus_Right.glb");

            LeftController = AddController("/user/hand/left/input/aim/pose", "Left Hand", "Models/MetaQuestTouchPlus_Left.glb");

            Head = AddHead();

            SceneRoot = AddSceneRoot();
        }

        public override void Update(RenderContext ctx)
        {
            if (_xrApp.IsStarted && !_isInit)
            {
                var oculus = _xrApp.Plugin<OculusXrPlugin>();

                if (oculus != null)
                {
                    Task.Run(async () =>
                    {

                        //var layout = oculus.GetSpaceRoomLayout(_xrApp.Stage);

                        var anchors = await oculus.GetAnchorsAsync(new XrAnchorFilter
                        {
                            Components = XrAnchorComponent.All,
                            //Ids = [layout.FloorUuid.ToGuid()],
                            Labels = ["FLOOR"]
                        });

                        var floor = anchors.FirstOrDefault(a => a.Labels != null && a.Labels.Contains("FLOOR"));

                        if (floor == null)
                            return;

                        _ = _scene!.App!.Dispatcher.ExecuteAsync(() =>
                        {
                            SceneRoot.AddComponent(new XrAnchorUpdate()
                            {
                                Space = new Space(floor.Space),
                                UpdateInterval = TimeSpan.FromMilliseconds(300),
                                LogChanges = true
                            });
                        });
                    });
                };

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

            Object3D? model = null;

            IXrInput? input = null;

            group.AddBehavior((_, ctx) =>
            {
                if (input == null)
                    input = _xrApp.Inputs.Values.FirstOrDefault(a => a.Path == path);

                if (input == null)
                    return;

                if (input.IsChanged && input.IsActive)
                {
                    var pose = (Pose3)input.Value;
                    group.WorldPosition = pose.Position;
                    group.WorldOrientation = pose.Orientation;
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

        public Group3D? Head { get; }

        public Group3D SceneRoot { get; }

        public Group3D? RightController { get; }

        public Group3D? LeftController { get; }
    }
}
