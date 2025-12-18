using System.Numerics;
using XrEngine;
using XrMath;

namespace XrSamples.Dnd
{
    public partial class Token : Group3D
    {
        readonly Group3D _tokenSet;

        readonly TokenPicture _picture;
        readonly TriangleMesh _box;
        readonly Object3D _mesh;
        private VttToken? _vttToken;

        public Token(Object3D mesh)
        {
            _tokenSet = new Group3D();

            _picture = new TokenPicture();
            _picture.Transform.Position = new Vector3(0, 0, 0.006f);

            _box = new TriangleMesh(Cube3D.Default, (Material)MaterialFactory.CreatePbr("#0075FF3E"));
            _box.Materials[0].Alpha = AlphaMode.Blend;

            _box.Transform.Scale = new Vector3(1, 1, 0.01f);

            _tokenSet.AddChild(_box);
            _tokenSet.AddChild(_picture);

            _mesh = mesh;

            AddChild(_tokenSet);
            AddChild(_mesh);

            this.AddComponent<TokenGrabbable>();

            var curPose = _mesh.GetWorldPose();
            _mesh.SetWorldPose(new Pose3
            {
                Orientation = curPose.Orientation,
                Position = Vector3.Zero
            });

            WorldPosition = curPose.Position;

            var y = _mesh.WorldBounds.Max.Y;
            _tokenSet.Transform.Position = new Vector3(0, y + 0.2f, 0);
        }

        public override void Update(RenderContext ctx)
        {
            var camera = ctx.Camera ?? _scene?.ActiveCamera;
            if (camera != null)
            {
                var forward = camera.Forward;
                _tokenSet.Forward = new Vector3(forward.X, 0, forward.Z).Normalize();
            }

            base.Update(ctx);
        }

        protected void UpdatePosition()
        {
            var scene = (DndScene)_scene!;
            var padding = 200;
            var mapSize = new Vector2(scene.VttScene!.Width - padding * 2.2f, scene.VttScene.Height - padding * 2.2f);
            var pos = new Vector2(int.Parse(_vttToken!.Left![..^2]), int.Parse(_vttToken.Top![..^2]));
            pos -= new Vector2(padding, padding);

            var tiles = scene.Map!.Children.OfType<Group3D>().First(a => a.Name == "Tiles");
            tiles.UpdateBounds();
            var bounds = tiles.LocalBounds;

            var localPos = pos / mapSize * new Vector2(bounds.Size.X, bounds.Size.Z);
            Transform.Position = new Vector3(localPos.X + 0.5f, Transform.Position.Y, -(bounds.Size.Z - localPos.Y - 0.5f));

            //SendPosition();
        }

        [Action]
        public void SendPosition()
        {
            var scene = (DndScene)_scene!;
            var padding = 200;
            var mapSize = new Vector2(scene.VttScene!.Width - padding * 2.2f, scene.VttScene.Height - padding * 2.2f);

            var tiles = scene.Map!.Children.OfType<Group3D>().First(a => a.Name == "Tiles");

            tiles.UpdateBounds();
            var bounds = tiles.LocalBounds;

            var position = Transform.Position;

            var localPosX = position.X - 0.5f;
            var localPosY = bounds.Size.Z - (-position.Z) - 0.5f;

            var mapCoord = new Vector2(localPosX / bounds.Size.X, localPosY / bounds.Size.Z);
            var pos = mapCoord * mapSize + new Vector2(padding, padding);

            _vttToken!.Left = $"{(int)Math.Round(pos.X)}px";
            _vttToken!.Top = $"{(int)Math.Round(pos.Y)}px";

            _ = scene.VttClient.UpdateTokenAsync(scene.VttScene.Id, _vttToken);
        }

        public VttToken? VttToken
        {
            get => _vttToken;
            set
            {
                _vttToken = value;
                if (value != null)
                {
                    _picture.Update(value);
                    UpdatePosition();
                }

            }
        }

    }
}
