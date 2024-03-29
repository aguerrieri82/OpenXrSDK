using System.ComponentModel;

namespace XrEngine
{
    public class StateContext
    {
    }

    public interface IStateContainer
    {
        IStateContainer Enter(string key);

        void WriteRef(string key, object? value);

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

    public interface ITypeStateManager
    {
        void GetState(object obj, StateContext ctx, IStateContainer container);

        void SetState(object obj, StateContext ctx, IStateContainer container);

        bool CanHandle(Type type);
    }

    public interface ITypeStateManager<T> : ITypeStateManager   
    {
        void GetState(T obj, StateContext ctx, IStateContainer container);

        void SetState(T obj, StateContext ctx, IStateContainer container);

        bool ITypeStateManager.CanHandle(Type type) => 
            typeof(T).IsAssignableFrom(type);

        void ITypeStateManager.GetState(object obj, StateContext ctx, IStateContainer container) =>
            GetState((T)obj, ctx, container);

        void ITypeStateManager.SetState(object obj, StateContext ctx, IStateContainer container) =>
            SetState((T)obj, ctx, container);
    }
}
