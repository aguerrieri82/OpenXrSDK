﻿namespace XrEngine
{
    public class Shader : EngineObject
    {
        public string? VertexSourceName { get; set; }

        public string? FragmentSourceName { get; set; }

        public string? GeometrySourceName { get; set; }

        public string? TessControlSourceName { get; set; }

        public string? TessEvalSourceName { get; set; }

        public Func<string, string>? Resolver { get; set; }

        public bool IsLit { get; set; }

        public bool IsEffect { get; set; }

        public bool VaryByModel { get; set; }

        public int Priority { get; set; }

        public DrawPrimitive? ForcePrimitive { get; set; }

        public string[]? SourcePaths { get; set; }
    }
}
