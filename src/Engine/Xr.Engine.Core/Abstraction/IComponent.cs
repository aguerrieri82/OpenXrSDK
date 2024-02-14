namespace OpenXr.Engine
{
    public interface IComponent
    {
        void Attach(IComponentHost host);

        void Detach();

        bool IsEnabled { get; set; }

        IComponentHost? Host { get; }
    }
}
