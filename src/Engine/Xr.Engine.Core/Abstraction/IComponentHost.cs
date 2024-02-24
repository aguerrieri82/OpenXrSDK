namespace Xr.Engine
{
    public interface IComponentHost
    {
        void AddComponent(IComponent component);

        void RemoveComponent(IComponent component);

        IEnumerable<T> Components<T>() where T : IComponent;
    }
}
