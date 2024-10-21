
namespace XrEngine
{
    public interface IMaterial
    {
        void NotifyChanged(ObjectChange change);

        AlphaMode Alpha { get; set; }

        bool CastShadows { get; set; }

        byte? CompareStencilMask { get; set; }

        bool DoubleSided { get; set; }

        bool IsEnabled { get; set; }

        string? Name { get; set; }

        StencilFunction StencilFunction { get; set; }

        bool UseDepth { get; set; }

        bool WriteColor { get; set; }

        bool WriteDepth { get; set; }

        byte? WriteStencil { get; set; }

        long Version { get; set; }
    }
}