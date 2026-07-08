using XrEngine.OpenXr;

namespace XrSamples
{
    public class GameSettings
    {

        public string? SampleName { get; set; }

        public string? Hdri { get; set; }

        public int Msaa { get; set; }

        public GraphicDriver Driver { get; set; }

        public bool IsMultiView { get; set; }

        public bool EnableDepthPass { get; set; }

        public bool UsePbrV2 { get; set; }

        public bool UseSpaceWarp { get; set; }

        public bool FrustumCulling { get; set; }

        public bool TextureCompression { get; set; }

        public float Scale { get; set; }


        public static GameSettings Graffiti()
        {
            return new GameSettings
            {
                SampleName = "Light Field",
                Msaa = 2,
                Scale = 1f,
                UsePbrV2 = true,
                Driver = GraphicDriver.OpenGL,
                IsMultiView = true,
                UseSpaceWarp = true,
                EnableDepthPass = false,
                FrustumCulling = true,
                TextureCompression = false
            };
        }

        public static GameSettings Default()
        {
            return new GameSettings()
            {
                Msaa = 1,
                Scale = 1f,
                Driver = GraphicDriver.OpenGL,
                IsMultiView = true,
                UseSpaceWarp = true,
                EnableDepthPass = false,
                UsePbrV2 = true,
                FrustumCulling = true
            };
        }
    }
}
