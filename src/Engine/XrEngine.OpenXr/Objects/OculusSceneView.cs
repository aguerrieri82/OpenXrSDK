using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using System.Numerics;
using XrMath;

namespace XrEngine.OpenXr
{

    public class OculusSceneView : Group3D
    {
        protected bool _isSceneLoaded;
        protected bool _isSceneLoading;
        protected XrApp? _app;

        public OculusSceneView()
        {
            Flags |= EngineObjectFlags.DisableNotifyChangedScene | EngineObjectFlags.Generated;
            Name = "SceneView";
            Factory = DefaultSceneModelFactory.Instance;
        }

        public override void Update(RenderContext ctx)
        {
            if (!_isSceneLoaded && !_isSceneLoading)
            {
                if (_app == null && XrApp.Current != null)
                    _app = XrApp.Current;

                if (_app != null && _app.IsStarted)
                    _ = LoadSceneAsync();
            }

            base.Update(ctx);
        }

        public Object3D? AddChild(SceneModelInfo model)
        {
            var obj = Factory.CreateModel(model);
            if (obj != null)
                AddChild(obj);
            return obj;
        }

        protected async Task LoadSceneAsync()
        {
            _isSceneLoading = true;

            var oculus = _app!.Plugin<OculusXrPlugin>();

            try
            {
                var anchors = await oculus.GetAnchorsAsync(new XrAnchorFilter()
                {
                    Components = XrAnchorComponent.Label | XrAnchorComponent.Bounds
                });

                foreach (var anchor in anchors!.Where(a => a.Labels != null))
                {
                    if (anchor.Space == 0)
                        continue;

                    var isMesh = anchor.Labels!.Contains("GLOBAL_MESH");

                    var info = new SceneModelInfo()
                    {
                        Labels = anchor.Labels,
                        AnchorId = anchor.Id,
                        Space = new Space(anchor.Space),
                        Pose = Pose3.Identity,
                        Size = anchor.Bounds2D != null ? new Vector2(anchor.Bounds2D!.Value.Width, anchor.Bounds2D.Value.Height) : Vector2.Zero
                    };

                    if (isMesh)
                    {
                        var sceneMesh = oculus.GetSpaceTriangleMesh(info.Space);

                        var geo = new Geometry3D
                        {
                            Indices = sceneMesh.Indices!,
                            ActiveComponents = VertexComponent.Position,
                            Vertices = sceneMesh.Vertices!.Select(a => new VertexData
                            {
                                Pos = new Vector3(a.X, a.Y, a.Z)
                            }).ToArray()
                        };

                        geo.Rebuild();
                        geo.ComputeNormals();

                        info.Type = SceneModelType.Mesh;
                        info.Geometry = geo;
                    }

                    if (anchor.Labels!.Contains("WALL_FACE"))
                        info.Type = SceneModelType.Wall;
                    else if (anchor.Labels!.Contains("FLOOR"))
                        info.Type = SceneModelType.Floor;
                    else if (anchor.Labels!.Contains("CEILING"))
                        info.Type = SceneModelType.Ceiling;
                    else if (anchor.Labels!.Contains("WINDOW_FRAME"))
                        info.Type = SceneModelType.Window;
                    else if (anchor.Labels!.Contains("DOOR_FRAME"))
                        info.Type = SceneModelType.Door;

                    var model = Factory.CreateModel(info);

                    if (model == null)
                        continue;

                    var isLocatable = oculus.EnumerateSpaceSupportedComponentsFB(info.Space).Contains(SpaceComponentTypeFB.LocatableFB);

                    if (isLocatable)
                    {
                        if (!oculus.GetSpaceComponentEnabled(info.Space, SpaceComponentTypeFB.LocatableFB))
                            await oculus.SetSpaceComponentStatusAsync(info.Space, SpaceComponentTypeFB.LocatableFB, true);

                        model.AddComponent(new XrAnchorUpdate
                        {
                            Space = info.Space,
                            UpdateInterval =  TimeSpan.FromMilliseconds(300)
                        });
                    }

                    AddChild(model);
                }

                _isSceneLoaded = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                _isSceneLoading = false;
            }

            SceneReady?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? SceneReady;

        public ISceneModelFactory Factory { get; set; }
    }
}
