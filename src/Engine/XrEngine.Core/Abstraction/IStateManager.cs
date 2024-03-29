namespace XrEngine
{
    public class StateContext
    {
    }

    public interface IStateContainer
    {
        IStateContainer Enter(string key);

        void WriteRef(string key, object value);

        void Write(string key, object? value);

        T Read<T>(string key);

        int Count { get; }

        IEnumerable<string> Keys { get; }  
    }

    public interface IStateManager
    {
        void GetState(StateContext ctx, IStateContainer container);

        void SetState(StateContext ctx, IStateContainer container);
    }

}
