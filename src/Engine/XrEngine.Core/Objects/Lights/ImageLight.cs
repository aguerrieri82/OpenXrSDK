
using XrEngine.Services;

namespace XrEngine
{
    public class ImageLight : Light
    {
        bool _panoramaDirty;

        public ImageLight()
        {
            Intensity = 3;
            Textures = new PbrMaterial.IBLTextures();
        }

        public void LoadPanorama(string hdrFileName)
        {
            Panorama = AssetLoader.Instance.Load<Texture2D>(hdrFileName, new TextureReadOptions { Format = TextureFormat.RgbaFloat32 });
            _panoramaDirty = true;
        }

        public override void Update(RenderContext ctx)
        {
            if (_panoramaDirty && Panorama?.Data != null)
            {
                LoadPanorama();
                NotifyChanged(ObjectChangeType.Render);
            }

            base.Update(ctx);
        }

        public void LoadPanorama()
        {
            var processor = _scene?.App?.Renderer as IIBLPanoramaProcessor;

            if (processor != null)
            {
                var options = PanoramaProcessorOptions.Default();
                options.SampleCount = 1024;
                options.Resolution = 256;
                options.Mode = IBLProcessMode.GGX | IBLProcessMode.Lambertian;

                Textures = processor.ProcessPanoramaIBL(Panorama!.Data![0], options);

                _panoramaDirty = false;
            }
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(Rotation), Rotation);
            container.Write("Panorama", Panorama);

        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            Rotation = container.Read<float>(nameof(Rotation));
            Panorama = container.Read<Texture2D>("Panorama");
            _panoramaDirty = true;
        }

        public PbrMaterial.IBLTextures Textures { get; set; }

        public Texture2D? Panorama { get; set; }

        public float Rotation { get; internal set; }
    }
}
