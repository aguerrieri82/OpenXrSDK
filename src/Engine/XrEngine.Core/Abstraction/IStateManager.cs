namespace XrEngine
{
    public class RefTable
    {
        public Dictionary<ObjectId, object> Resolved { get; } = [];

        public IStateContainer? Container { get; set; }
    }

    public enum StateContextFlags
    {
        None,
        SelfOnly = 0x1,
        Store = 0x2,
        Update = 0x4
    }

    public interface IStateContext
    {
        public RefTable RefTable { get; }

        public StateContextFlags Flags { get; set; }
    }

    public interface IStateManager
    {
        void GetState(IStateContainer container);

        void SetState(IStateContainer container);
    }

}
