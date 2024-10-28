using SkiaSharp;
using System.Numerics;
using XrEngine;
using XrMath;

namespace XrEditor.Services
{
    public class RenderPreviewCreator
    {
        readonly IRenderEngine _engine;
        readonly Texture2D _texture;
        readonly EngineApp _app;
        readonly WireframeMaterial _wireframe;
        readonly Scene3D _scene;
        readonly TriangleMesh _mesh;
        readonly ImageLight _light;
        readonly TextureMaterial _textureMaterial;

        public RenderPreviewCreator(IRenderEngine engine)
        {
            _engine = engine;

            _texture = new Texture2D()
            {
                Width = 200,
                Height = 200,
                SampleCount = 1,
                MipLevelCount = 1,
                Format = TextureFormat.Rgba32,
                WrapS = WrapMode.ClampToEdge,
                WrapT = WrapMode.ClampToEdge,
                MagFilter = ScaleFilter.Linear,
                MinFilter = ScaleFilter.Linear
            };


            _scene = new Scene3D();
            var camera = new PerspectiveCamera()
            {
                Near = 0.01f,
                Far = 5f,
            };

            camera.SetFov(45, _texture.Width, _texture.Height);

            _scene.ActiveCamera = camera;
            _scene.Name = "Preview";

            while (_scene.Layers.Layers.Count > 0)
                _scene.Layers.Remove(_scene.Layers.Layers[0]);

            _light = new ImageLight();
            _light.LoadPanorama("res://asset/pisa.hdr");
            _light.Intensity = 2.5f;
            _light.NotifyChanged(ObjectChangeType.Render);  

            _app = new EngineApp();
            _app.Renderer = _engine;
            _app.OpenScene(_scene);

            _textureMaterial = new TextureMaterial() { UseDepth = false, WriteDepth = false, DoubleSided = false };

            _wireframe = new WireframeMaterial() { Color = new Color(1, 1, 1, 1), UseDepth = false, WriteDepth = false, DoubleSided = true };
            _mesh = new TriangleMesh();
            _mesh.Materials.Add(_wireframe);
        }

        public SKBitmap? CreateMaterial(Material material)
        {
            return CreateMesh(IsoSphere3D.Default, material);
        }

        public SKBitmap? CreateGeometry(Geometry3D geometry)
        {
            return CreateMesh(geometry, _wireframe);
        }

        public SKBitmap? CreateTexture(Texture2D texture)
        {
            if (texture.Width == 0 || texture.Height == 0)
            {
                var src = texture.Component<AssetSource>();
                if (src?.Asset != null)
                    src.Asset.Update(texture);
            }

            _textureMaterial.Texture = texture;
            _textureMaterial.NotifyChanged(ObjectChangeType.Render);

            _mesh.Materials.Clear();
            _mesh.Materials.Add(_textureMaterial);
            _mesh.Geometry = Quad3D.Default;

            _scene.PerspectiveCamera().LookAt(new Vector3(0, 0, 1.3f), Vector3.Zero, Vector3.UnitY);

            _app.ActiveScene!.Clear();
            _app.ActiveScene!.AddChild(_mesh);

            return CreateImage();
        }

        protected SKBitmap? CreateMesh(Geometry3D geometry, Material material)
        {
            _mesh.Geometry = geometry;
            _mesh.Materials.Clear();
            _mesh.Materials.Add(material);
            _mesh.NotifyChanged(ObjectChangeType.Render);

            var diagonal = geometry.Bounds.Size.Length();
            var distance = diagonal / (2 * MathF.Tan((45f / 180f * MathF.PI) / 2));
            var pos = geometry.Bounds.Center + distance * new Vector3(1, 1, 1).Normalize();
            _scene.PerspectiveCamera().LookAt(pos, geometry.Bounds.Center, Vector3.UnitY);

            _app.ActiveScene!.Clear();
            _app.ActiveScene!.AddChild(_mesh);
            _app.ActiveScene!.AddChild(_light);

            return CreateImage();
        }


        protected unsafe SKBitmap? CreateImage()
        {
            _engine.SetRenderTarget(_texture);

            _app.RenderFrame(new Rect2I()
            {
                Width = _texture.Width,
                Height = _texture.Height
            });

            var data = ((IFrameReader)_app.Renderer!).ReadFrame();

            _engine.SetRenderTarget(null);

            return ImageUtils.ToBitmap(data, true);
        }

        public IRenderEngine Engine => _engine;
    }
}
