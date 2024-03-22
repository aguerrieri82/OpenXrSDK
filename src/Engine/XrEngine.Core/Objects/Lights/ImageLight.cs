using XrEngine.Materials;
using XrMath;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace XrEngine
{
    public class ImageLight : Light
    {
        public ImageLight()
        {
            Intensity = 1;
        }

        public void LoadPanorama(string hdrFileName)
        {
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

                LoadPanorama(data);
            }
        }


        public void LoadPanorama(TextureData data)
        {
            var processor = _scene?.App?.Renderer as IIBLPanoramaProcessor;

            if (processor != null)
            {
                var options = PanoramaProcessorOptions.Default();
                options.SampleCount = 1024;
                options.Mode = IBLProcessMode.GGX | IBLProcessMode.Lambertian;

                Textures = processor.ProcessPanoramaIBL(data, options);
            }
            else
                Textures = new PbrMaterial.IBLTextures();

            Textures.Panorama = new Texture2D([data]);
        }

        public PbrMaterial.IBLTextures? Textures { get; set; }

        public float Rotation { get; internal set; }
    }
}
