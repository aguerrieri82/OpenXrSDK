namespace XrEngine.OpenGL
{

    public struct ShaderMaterialKey
    {

        public Guid MateriaId;

        public VertexComponent ActiveComponent;

        public readonly override int GetHashCode()
        {
            return MateriaId.GetHashCode() ^ ActiveComponent.GetHashCode();
        }

    }

    public class ShaderContentV2
    {
        public GlProgramGlobal? ProgramGlobal;

        public readonly Dictionary<ShaderMaterialKey, MaterialContentV2> Contents = [];

        public KeyValuePair<ShaderMaterialKey, MaterialContentV2>[]? SortedContent = [];

        public bool IsDirty;
    }

    public class MaterialContentV2
    {
        public readonly Dictionary<EngineObject, VertexContentV2> Contents = [];

        public Material? Material;

        public GlProgramInstance? ProgramInstance;

        public bool IsHidden;

        public bool UseInstanceDraw;

        public VertexComponent ActiveComponents;
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
