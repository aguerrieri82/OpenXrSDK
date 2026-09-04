using XrEngine;
using XrEngine.OpenGL;
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

        public MotionVectorMode MotionVectorMode { get; set; }

        public bool FrustumCulling { get; set; }

        public bool TextureCompression { get; set; }

        public float Scale { get; set; }

        public float DepthScale { get; set; }

        public XrProjDepthMode ProjDepthMode { get; set; }

        public bool UseFxAA { get; set; }

        public bool UseSimmetricFov { get; set; }

        public bool UseDynamicResolution { get; set; }

        public bool UseRayCollider { get; set; }

        public bool UsePrimitiveBoundingBox { get; set; }

        public ToneMapMode ToneMap { get; set; }

        public bool UseSharedSsbo { get; set; }

        public bool UseMeshCompression { get; set; }

        public bool UseProfileOverlay { get; set; }

        public bool UseAsyncShaderCompile { get; set; }

        public static GameSettings Graffiti()
        {
            return new GameSettings
            {
               SampleName = "Car",
                Msaa = 1,
                Scale = 1f,
                DepthScale = 0.25f,
                Driver = GraphicDriver.OpenGL,
                IsMultiView = true,
                MotionVectorMode = MotionVectorMode.None,
                EnableDepthPass = false,
                FrustumCulling = true,
                TextureCompression = true,
                ProjDepthMode = XrProjDepthMode.DepthCopyImage,
                UseFxAA = false,
                UseSimmetricFov = false,
                UseDynamicResolution = false,
                UseRayCollider = false,
                UsePrimitiveBoundingBox = false,
                ToneMap = ToneMapMode.Aces,
                UseSharedSsbo = false,
                UseMeshCompression = true,
                UseProfileOverlay = false,
                UseAsyncShaderCompile = true
            };
        }

        public static GameSettings Default()
        {
            return new GameSettings()
            {
                Msaa = 1,
                Scale = 1f,
                DepthScale = 0.5f,
                Driver = GraphicDriver.Angle,
                IsMultiView = false,
                MotionVectorMode = MotionVectorMode.None,
                EnableDepthPass = false,
                FrustumCulling = true,
                ProjDepthMode = XrProjDepthMode.DepthCopyImage,
                UseFxAA = false,
                UseSimmetricFov = false,
                UseDynamicResolution = false,
                UseRayCollider = false,
                UsePrimitiveBoundingBox = false,
                ToneMap = ToneMapMode.Aces,
                UseSharedSsbo = false,
                UseMeshCompression = true,
                UseProfileOverlay = false,
                UseAsyncShaderCompile = true
            };
        }
    }
}