namespace XrEngine.OpenGL
{
    public class ShaderContent
    {
        public GlProgramGlobal? ProgramGlobal;

        public readonly Dictionary<EngineObject, VertexContent> Contents = [];
    }

    public class VertexContent
    {
        public GlVertexSourceHandle? VertexHandler;

        public VertexComponent ActiveComponents;

        public int RenderPriority;

        public readonly List<DrawContent> Contents = [];

        public float AvgDistance;
    }

    public class DrawContent
    {
        public Object3D? Object;

        public Action? Draw;

        public int DrawId;

        public GlProgramInstance? ProgramInstance;

        public GlQuery? Query;

        public bool IsHidden;

        public float Distance;
    }


    public class RenderContent
    {
        public IList<Light>? Lights;

        public long LayerVersion;

        public long ImageLightVersion = -1;

        public string LightsHash = "";

        public readonly Dictionary<Shader, ShaderContent> ShaderContents = [];
    }
}
