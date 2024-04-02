
namespace XrEngine
{
    public class ImageLight : Light
    {
        string? _hdrFileName;
        TextureData? _hdrData;

        public ImageLight()
        {
            Intensity = 3;
        }

        public void LoadPanorama(string hdrFileName)
        {
            if (_hdrFileName == hdrFileName)
                return;

            using (var stream = File.OpenRead(hdrFileName))
            {
                var ext = Path.GetExtension(hdrFileName);
                
                TextureData data;

                if (ext.ToLower() == ".hdr")
                    data = HdrReader.Instance.Read(stream)[0];
                else if (ext.ToLower() == ".pvr")
                    data = PvrTranscoder.Instance.Read(stream)[0];
                else
                    data = ImageReader.Instance.Read(stream, new TextureReadOptions { Format = TextureFormat.RgbaFloat32 })[0];

                _hdrData = data;
            }

            _hdrFileName = hdrFileName;


        }

        public override void Update(RenderContext ctx)
        {
            if (_hdrData != null)
            {
                LoadPanorama(_hdrData);
                NotifyChanged(ObjectChangeType.Render);
                _hdrData = null;
            }
            base.Update(ctx);
        }

        public void LoadPanorama(TextureData data)
        {
            var processor = _scene?.App?.Renderer as IIBLPanoramaProcessor;

            if (processor != null)
            {
                var options = PanoramaProcessorOptions.Default();
                options.SampleCount = 1024;
                options.Resolution = 256;
                options.Mode = IBLProcessMode.GGX | IBLProcessMode.Lambertian;

                Textures = processor.ProcessPanoramaIBL(data, options);
            }
            else
                Textures = new PbrMaterial.IBLTextures();

            Textures.Panorama = new Texture2D([data]);
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(Rotation), Rotation);
            container.Write("HdrFileName", _hdrFileName);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            Rotation = container.Read<float>(nameof(Rotation));
            if (container.Contains("HdrFileName"))
                LoadPanorama(container.Read<string>("HdrFileName"));
        }

        public PbrMaterial.IBLTextures? Textures { get; set; }

        public float Rotation { get; internal set; }
    }
}
