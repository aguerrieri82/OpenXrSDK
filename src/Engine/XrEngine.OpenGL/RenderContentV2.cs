namespace XrEngine.OpenGL
{
    public class ShaderContentV2
    {
        public GlProgramGlobal? ProgramGlobal;

        public readonly Dictionary<Material, MaterialContentV2> Contents = [];

        public KeyValuePair<Material, MaterialContentV2>[]? SortedContent = [];
    }

    public class MaterialContentV2
    {
        public readonly Dictionary<EngineObject, VertexContentV2> Contents = [];

        public GlProgramInstance? ProgramInstance;

        public bool IsHidden;

        public bool UseInstanceDraw;
    }



    public class VertexContentV2
    {
        public IBuffer? InstanceBuffer;

        public readonly List<DrawContent> Contents = [];

        public GlVertexSourceHandle? VertexHandler;

        public VertexComponent ActiveComponents;

        public bool IsHidden;

        public long ContentVersion;

        public Action? Draw;
    }

    public class RenderContentV2
    {
        public long LayerVersion;

        public readonly Dictionary<Shader, ShaderContentV2> Contents = [];
    }
}
