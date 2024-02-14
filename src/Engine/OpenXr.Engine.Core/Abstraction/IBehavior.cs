namespace OpenXr.Engine
{
    public interface IBehavior : IComponent, IRenderUpdate
    {

  
        void Start(RenderContext ctx);

    }
}
