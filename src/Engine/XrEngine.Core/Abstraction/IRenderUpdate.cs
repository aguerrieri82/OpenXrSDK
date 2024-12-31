
namespace XrEngine
{
    public interface IRenderUpdate : ILayer3DItem
    {
        void Update(RenderContext ctx);

        void Reset(bool onlySelf = false);

        int UpdatePriority { get; }
    }
}
