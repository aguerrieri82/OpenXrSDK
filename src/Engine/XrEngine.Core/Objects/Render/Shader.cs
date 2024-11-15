namespace XrEngine
{
    public class Shader : EngineObject
    {
        public string? VertexSourceName { get; set; }

        public string? FragmentSourceName { get; set; }

        public string? GeometrySourceName { get; set; }

        public Func<string, string>? Resolver { get; set; }

        public bool IsLit { get; set; }

        public bool IsEffect { get; set; }

        public int Priority { get; set; }

        public DrawPrimitive? ForcePrimitive { get; set; }
    }
}
