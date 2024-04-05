namespace XrEngine
{
    public class RefTable
    {
        public readonly Dictionary<ObjectId, object> Resolved = [];

        public IStateContainer? Container;
    }

    public enum StateContextFlags
    {
        None,
        SelfOnly = 0x1
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
