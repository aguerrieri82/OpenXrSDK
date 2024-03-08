namespace XrEngine
{
    public abstract class StateContext
    {
        public abstract T StateAs<T>(IObjectState state);
    }

    public interface IObjectState
    {

    }

    public interface IStateManager
    {
        IObjectState GetState(StateContext ctx);

        void SetState(IObjectState state, StateContext ctx);

        Type StateType { get; }
    }

    public interface IStateManager<T> : IStateManager where T : IObjectState
    {
        new T GetState(StateContext ctx);

        void SetState(T state, StateContext ctx);

        IObjectState IStateManager.GetState(StateContext ctx) => GetState(ctx)!;

        Type IStateManager.StateType => typeof(T);

        void IStateManager.SetState(IObjectState state, StateContext ctx) => SetState(ctx.StateAs<T>(state), ctx);
    }
}
