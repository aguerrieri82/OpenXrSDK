
using XrEngine.Services;

namespace XrEngine
{
    public class ImageLight : Light
    {

        public ImageLight()
        {
            Intensity = 3;
            Textures = new PbrMaterial.IBLTextures();
        }

        public void LoadPanorama(string hdrFileName)
        {
            Panorama = AssetLoader.Instance.Load<Texture2D>(hdrFileName, new TextureReadOptions { Format = TextureFormat.RgbaFloat32 });
            Panorama.Version = DateTime.Now.Ticks;

            NotifyChanged(ObjectChangeType.Render);
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
            if (Panorama != null)
                Panorama.Version = DateTime.Now.Ticks;
        }

        public PbrMaterial.IBLTextures Textures { get; set; }

        public Texture2D? Panorama { get; set; }

        public float Rotation { get; internal set; }
    }
}
