namespace Xr.Engine
{
    public class Shader : EngineObject
    {
        public string? VertexSourceName { get; set; }

        public string? FragmentSourceName { get; set; }

        public Func<string, string>? Resolver { get; set; }

        public bool IsLit { get; set; }

        public int Priority { get; set; }
    }
}
