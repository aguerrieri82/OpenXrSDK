namespace XrEngine
{
    public interface IRenderUpdate
    {
        void Update(RenderContext ctx);

        void Reset(bool onlySelf = false);
    }
}
