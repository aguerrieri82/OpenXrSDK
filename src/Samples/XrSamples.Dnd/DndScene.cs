
using OpenXr.Framework;
using System.Numerics;
using XrEngine;
using XrEngine.Audio;
using XrEngine.OpenXr;
using XrEngine.Physics;
using XrMath;

namespace XrSamples.Dnd
{
    public class DndScene : Scene3D, IAboveVttListener
    {
        Group3D? _map;
        VttScene? _vttScene;
        readonly TriangleMesh _player;
        readonly AboveVttClient _client;

        public DndScene()
        {
            _client = new AboveVttClient(this);

            AddChild(new PlaneGrid(6f, 12f, 2f));

            PerspectiveCamera camera = new PerspectiveCamera
            {
                Far = 100f,
                Near = 0.01f,
                BackgroundColor = new Color(0, 0, 0, 0),
                Exposure = 1
            };

            camera.LookAt(new Vector3(1, 1.7f, 1), new Vector3(0, 0, 0), new Vector3(0, 1, 0));

            ActiveCamera = camera;

            this.AddComponent<InputController>();
            this.AddComponent<AudioSystem>();
            this.AddComponent<DebugGizmos>();

            _player = new TriangleMesh(Cube3D.Default, (Material)MaterialFactory.CreatePbr("#ff0000"));
            _player.Transform.SetScale(0.3f, 1.0f, 0.3f);
            _player.Transform.LocalPivot = new Vector3(0, -0.5f, 0);
            _player.Materials[0].IsEnabled = false;

            _player.AddComponent(new XrPlayer
            {
                Height = 0f
            });

            _player.Name = "Player";

            AddChild(_player);
        }


        public async Task LoadAsync(string campaignId)
        {
            await _client.ConnectAsync(campaignId);

            VttCurrentSceneResponse scenes = await _client.GetCurrentSceneAsync();

            _vttScene = (await _client.GetSceneAsync(scenes.DmScene)).Data;
        }

        public Token? AddToken(string name, Guid vttTokenId)
        {
            Object3D? mesh = _map!.Children.FirstOrDefault(a => a.Name == name);
            if (mesh == null)
                return null;

            return AddToken(mesh, vttTokenId);
        }

        public Token AddToken(Object3D mesh, Guid vttTokenId)
        {
            Token token = new Token(mesh);

            _map!.AddChild(token);

            token.VttToken = _vttScene?.Tokens?.First(a => a.Id == vttTokenId);

            OnTokenUpdate(token.VttToken!);

            return token;
        }

        public Group3D LoadMap(string assetDir)
        {
            string path = Context.Require<IAssetStore>().GetPath(Path.Join(assetDir, "draws.json"));

            DndImporter imp = new DndImporter
            {
                SimpleMaterials = false
            };

            _map = imp.Import(Path.GetDirectoryName(path)!);

            _map.AddComponent<BoundsGrabbable>();

            AddChild(_map);

            _map.Transform.SetPosition(0, -imp.MapY, 0);

            Vector3 size = _map.WorldBounds.Size;
            TriangleMesh floor = new TriangleMesh(Quad3D.Default);
            floor.Name = "Floor";
            floor.Transform.Scale = new Vector3(size.X, size.Z, 0.01f);
            floor.Transform.Position = new Vector3(_map.WorldBounds.Center.X, imp.MapY, _map.WorldBounds.Center.Z);
            floor.Transform.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2);

            floor.AddComponent<TeleportTarget>();
            floor.AddComponent(new RigidBody
            {
                Type = PhysX.Framework.PhysicsActorType.Static,
                IsEnabled = false
            });

            PointLight light = new PointLight();
            light.Transform.Position = new Vector3(0, 1.8f, 0);
            light.Intensity = 10f;
            light.CastShadows = false;
            light.Range = 50;
            light.WorldPosition = new Vector3(15.109999f, 4.0699997f, -5.85f);

            _map.AddChild(floor);
            _map.AddChild(light);

            return _map;
        }

        public override void Dispose()
        {
            _ = _client.DisconnectAsync();

            base.Dispose();
        }

        public void ResetPose()
        {
            _map!.Transform.Reset();

            _map.Transform.SetPosition(0, 0, 0);

            if (XrApp.Current != null)
                XrApp.Current.ReferenceFrame = Pose3.Identity;

            _player.WorldPosition = Vector3.Zero;

            //Player.Component<XrPlayer>().Teleport(Vector3.Zero);
        }

        public void OnTokenUpdate(VttToken token)
        {
            Token? gameToken = _map?.Children.OfType<Token>().Where(a => a.VttToken?.Id == token.Id).FirstOrDefault();
            if (gameToken != null)
                gameToken.VttToken = token;

        }

        public VttScene? VttScene => _vttScene;

        public DndSettings Settings { get; } = new();

        public Group3D? Map => _map;

        public Object3D Player => _player;

        public InputController InputController => this.Component<InputController>();

        public AboveVttClient VttClient => _client;
    }
}
