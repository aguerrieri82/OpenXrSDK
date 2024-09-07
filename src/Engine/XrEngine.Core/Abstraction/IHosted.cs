namespace XrEngine
{

    internal interface IHosted
    {
        void Attach(EngineObject obj);

        void Detach(EngineObject obj);

        IReadOnlySet<EngineObject> Hosts { get; }
    }
}
