using XrMath;

namespace XrEngine
{
    public interface IShadowMaterial : IMaterial
    {
        bool ReceiveShadows { get; set; }

        Color ShadowColor { get; set; }
    }
}
