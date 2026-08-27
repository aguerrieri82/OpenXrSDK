namespace XrEngine.OpenGL
{
    public class DrawContent
    {
        public int Id;

        public Object3D? Object;

        public Action? Draw;

        public int DrawId;

        public GlProgramInstance? ProgramInstance;

        public GlQuery<uint>? Query;

        public bool IsHidden;

        public bool IsClipped;

        public float Distance;

        public long InstanceVersion;

        public bool InstanceChanged;

        public DepthObjectData DepthData;
    }

    public struct ShaderMaterialKey
    {
        public Guid MateriaId;

        public VertexComponent ActiveComponent;

        public long SingleDrawId;

        public readonly override int GetHashCode()
        {
            return MateriaId.GetHashCode() ^ ActiveComponent.GetHashCode() ^ SingleDrawId.GetHashCode();
        }
    }

    public class ShaderContent
    {
        public GlProgramGlobal? ProgramGlobal;

        public readonly Dictionary<ShaderMaterialKey, MaterialContent> Contents = [];

        public KeyValuePair<ShaderMaterialKey, MaterialContent>[]? SortedContent = [];

        public bool IsDirty;

        public int MaxPriority;

        public long SingleDrawCount;
    }

    public class MaterialContent
    {
        public readonly Dictionary<EngineObject, VertexContent> Contents = [];

        public Material? Material;

        public GlProgramInstance? ProgramInstance;

        public bool IsHidden;

        public bool UseInstanceDraw;

        public VertexComponent ActiveComponents;

        public Object3D? SingleModel;
    }

    public class VertexContent
    {
        public IBuffer? InstanceBuffer;

        public readonly List<DrawContent> Contents = [];

        public GlVertexSourceHandle? VertexHandler;

        public VertexComponent ActiveComponents;

        public bool IsHidden;

        public long ContentVersion;

        public Action? Draw;
    }

    public class RenderContent
    {
        public long LayerVersion;

        public readonly Dictionary<Shader, ShaderContent> Contents = [];

        public KeyValuePair<Shader, ShaderContent>[]? SortedContent = [];
    }
}
