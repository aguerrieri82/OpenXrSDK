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

        public bool UseSpaceWarp { get; set; }

        public bool FrustumCulling { get; set; }

        public bool TextureCompression { get; set; }

        public float Scale { get; set; }

        public bool UseResolve { get; set; }

        public float DepthScale { get; set; }

        public static GameSettings Graffiti()
        {
            return new GameSettings
            {
                SampleName = "DnD",
                Msaa = 2,
                Scale = 1f,
                DepthScale = 0.5f,
                Driver = GraphicDriver.OpenGL,
                IsMultiView = true,
                UseSpaceWarp = true,
                EnableDepthPass = false,
                FrustumCulling = true,
                TextureCompression = true,
                UseResolve = false
            };
        }

        public static GameSettings Default()
        {
            return new GameSettings()
            {
                Msaa = 1,
                Scale = 1f,
                DepthScale = 0.5f,
                Driver = GraphicDriver.OpenGL,
                IsMultiView = true,
                UseSpaceWarp = true,
                EnableDepthPass = false,
                FrustumCulling = true
            };
        }
    }
}
