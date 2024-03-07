namespace Xr.Engine
{
    public interface IComponentHost
    {
        T AddComponent<T>(T component) where T : IComponent;    

        void RemoveComponent(IComponent component);

        IEnumerable<T> Components<T>() where T : IComponent;
    }
}
