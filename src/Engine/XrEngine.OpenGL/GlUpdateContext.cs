namespace XrEngine.OpenGL
{
    public class GlUpdateContext : UpdateShaderContext
    {
        public uint ProgramInstanceId;

        public bool IsGlEs { get; internal set; }
    }
}
