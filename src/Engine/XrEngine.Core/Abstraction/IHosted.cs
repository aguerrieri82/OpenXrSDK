namespace XrEngine
{

    public interface IHosted
    {
        void Attach(EngineObject obj);

        void Detach(EngineObject obj);

        IReadOnlySet<EngineObject> Hosts { get; }
    }
}
