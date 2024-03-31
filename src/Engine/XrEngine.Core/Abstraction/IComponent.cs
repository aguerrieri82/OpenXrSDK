
namespace XrEngine
{
    public interface IComponent : IStateManager, IObjectId
    {
        void Attach(IComponentHost host);

        void Detach();

        bool IsEnabled { get; set; }

        IComponentHost? Host { get; }
    }

    public interface IComponent<THost> : IComponent where THost : IComponentHost
    {
        void Attach(THost host);

        new THost? Host { get; }

        void IComponent.Attach(IComponentHost host) => Attach((THost)host);

        IComponentHost? IComponent.Host => Host;
    }

}
