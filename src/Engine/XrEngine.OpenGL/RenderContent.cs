namespace XrEngine.OpenGL
{
    public class ShaderContent
    {
        public GlProgramGlobal? ProgramGlobal;

        public readonly Dictionary<EngineObject, VertexContent> Contents = [];

        public IList<VertexContent> ContentsSorted = [];
    }

    public class VertexContent
    {
        public readonly List<DrawContent> Contents = [];

        public GlVertexSourceHandle? VertexHandler;

        public VertexComponent ActiveComponents;

        public int RenderPriority;

        public float AvgDistance;

        public bool IsHidden;
    }

    public class DrawContent
    {
        public int Id;

        public Object3D? Object;

        public Action? Draw;

        public int DrawId;

        public GlProgramInstance? ProgramInstance;

        public GlQuery? Query;

        public bool IsHidden;

        public bool IsClipped;

        public float Distance;

        public long InstanceVersion;

        public bool InstanceChanged;

        public DepthObjectData DepthData;
    }


    public class RenderContent
    {
        public long LayerVersion;

        public readonly Dictionary<Shader, ShaderContent> ShaderContents = [];

        public IList<KeyValuePair<Shader, ShaderContent>> ShaderContentsSorted = [];
    }
}
