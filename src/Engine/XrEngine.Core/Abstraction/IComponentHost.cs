namespace XrEngine
{
    public interface IComponentHost
    {
        T AddComponent<T>(T component) where T : IComponent;

        void RemoveComponent(IComponent component);

        IReadOnlyList<IComponent> Components();
    }
}
