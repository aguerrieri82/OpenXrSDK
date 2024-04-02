namespace XrEngine
{


    public interface IStateContainer
    {
        IStateContainer Enter(string key, bool resolveRef = false);

        bool IsRef(string key);

        void Write(string key, object? value);

        object? Read(string key, Type type);

        int Count { get; }

        bool Contains(string key);  

        IEnumerable<string> Keys { get; }  

        IStateContext Context { get; }
    }
}
