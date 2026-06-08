namespace XrEngine.OpenGL
{
    public class GlUpdateContext : UpdateShaderContext
    {
        public uint ProgramInstanceId;

        public long ImageLightVersion;

        public int FrustumPlanesCount { get; internal set; }
    }
}
