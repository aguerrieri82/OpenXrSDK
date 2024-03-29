namespace XrEngine
{
    public interface IStateContainer
    {
        IStateContainer Enter(string key);

        void WriteRef(string key, object? value);

        void Write(string key, object? value);

        object? Read(string key, Type type);

        T Read<T>(string key);

        int Count { get; }

        bool Contains(string key);  

        IEnumerable<string> Keys { get; }  
    }
}
