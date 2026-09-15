using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using XrMath;
using System.Numerics;

namespace XrEngine.OpenXr
{
    public class OculusSceneLoader : BaseXrComponent<Group3D>
    {
        public interface INotifySceneLoaded
        {
            void NotifySceneLoaded();
        }

        protected bool _isSceneLoaded;

        public OculusSceneLoader()
            : this(DefaultSceneModelFactory.Instance)
        {
        }

        public OculusSceneLoader(ISceneModelFactory factory)
        {
            Factory = factory;
            _isAsync = true;
        }

        protected override void OnAttach()
        {
            _host.Flags |= EngineObjectFlags.DisableNotifyChangedScene | EngineObjectFlags.Generated;
            _host.Name = "SceneView";
        }

        protected override async Task UpdateWorkAsync(RenderContext ctx)
        {
            if (!_isSceneLoaded)
                await LoadSceneAsync();
        }

        protected override void DetachXr()
        {
            _isSceneLoaded = false;
        }

        public async Task LoadSceneAsync()
        {
            _host.Clear();

            var oculus = _xrApp!.Plugin<XrOculusPlugin>();

            try
            {
                var anchors = await oculus.GetSpacesAsync(new XrSpaceFilter()
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
                        await oculus.EnsureSpaceComponentAsync(info.Space, SpaceComponentTypeFB.LocatableFB);

                        model.AddComponent(new XrAnchorUpdate
                        {
                            Space = info.Space,
                            UpdateInterval = TimeSpan.FromMilliseconds(300)
                        });
                    }

                    _host.AddChild(model);
                }

                _isSceneLoaded = true;
            }
            catch (Exception ex)
            {
                Log.Warn(this, ex.ToString());
            }

            _isSceneLoaded = true;

            if (_host is INotifySceneLoaded loaded)
                loaded.NotifySceneLoaded();
        }

        public bool IsSceneLoaded => _isSceneLoaded;

        public ISceneModelFactory Factory { get; set; }
    }
}
