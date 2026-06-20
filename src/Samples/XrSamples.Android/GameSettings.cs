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


        public static GameSettings Graffiti()
        {
            return new GameSettings
            { 
                SampleName = "Depth Snapeshot",
                Msaa = 1,
                UsePbrV2 = true,
                Driver = GraphicDriver.OpenGL,
                IsMultiView = true,
                UseSpaceWarp = false,
                EnableDepthPass = false,
                FrustumCulling = false,
                TextureCompression = true
            };
        }

        public static GameSettings Default()
        {
            return new GameSettings()
            {
                Msaa = 1,
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
