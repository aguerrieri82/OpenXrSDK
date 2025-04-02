using System.Numerics;
using XrEngine;
using XrEngine.Audio;
using XrEngine.OpenXr;
using XrEngine.Physics;
using XrMath;

namespace XrSamples.Dnd
{
    public class DndScene : Scene3D
    {
        Group3D? _map;
        TriangleMesh _player;

        public DndScene()
        {
            AddChild(new PlaneGrid(6f, 12f, 2f));

            var camera = new PerspectiveCamera
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
            _player.Transform.SetScale(0.3f, 1.7f, 0.3f);
            _player.Transform.LocalPivot = new Vector3(0, -0.5f, 0);

            _player.AddComponent(new XrPlayer
            {
                Height = 0f
            });
            _player.Name = "Player";
            AddChild(_player);

        }



        public Group3D LoadMap(string assetDir)
        {
            var path = Context.Require<IAssetStore>().GetPath(Path.Join(assetDir, "draws.json"));

            var imp = new DndImporter();
            imp.SimpleMaterials = false;

            _map = imp.Import(Path.GetDirectoryName(path)!);

            foreach (var item in _map.Children.OfType<TriangleMesh>())
            {
                //item.AddComponent(new GeometryLod());
            }

            _map.AddComponent<BoundsGrabbable>();

            /*
            _map.Transform.SetPosition(-9, 1, 9);
            _map.Transform.SetPosition(-0.91999996f, 0, 0.35000038f);
            _map.Transform.SetScale(1f);
            */

            AddChild(_map);

            _map.UpdateBounds();

            var size = _map.WorldBounds.Size;
            var floor = new TriangleMesh(Quad3D.Default, new ColorMaterial());
            floor.Name = "Floor";
            floor.Transform.Scale = new Vector3(size.X, size.Z, 0.01f);
            floor.Transform.Position = new Vector3(_map.WorldBounds.Center.X, 0, _map.WorldBounds.Center.Z);
            floor.Transform.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2); 

            floor.AddComponent<TeleportTarget>();
            floor.AddComponent(new RigidBody
            {
                Type = PhysX.Framework.PhysicsActorType.Static,
                IsEnabled = false
            });

            _map.AddChild(floor);

            return _map;
        }

        public void AddToken(string name)
        {

            var token = Map.Children.FirstOrDefault(a => a.Name == name);
            if (token == null)
                return;
            token.AddComponent<BoundsGrabbable>();
        }

        public void ResetPose()
        {
            Map!.Transform.Reset();
            Player.Component<XrPlayer>().Teleport(Vector3.Zero);   
        }

        public DndSettings Settings { get; } = new();

        public Group3D? Map => _map;

        public Object3D Player => _player;

        public InputController InputController => this.Component<InputController>();    
    }
}
