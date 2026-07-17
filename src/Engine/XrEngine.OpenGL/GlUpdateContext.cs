namespace XrEngine.OpenGL
{
    public class GlUpdateContext : UpdateShaderContext
    {
        public Scene3D? Scene;

        public uint ProgramInstanceId;

        public long ImageLightVersion;

        public int FrustumPlanesCount { get; internal set; }
    }
}
