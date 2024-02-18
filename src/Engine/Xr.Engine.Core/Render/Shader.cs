namespace OpenXr.Engine
{
    public class Shader : EngineObject
    {
        public string? VertexSource { get; set; }

        public string? FragmentSource { get; set; }

        public Func<string, string>? IncludeResolver { get; set; }

        public bool IsLit { get; set; }
    }
}
