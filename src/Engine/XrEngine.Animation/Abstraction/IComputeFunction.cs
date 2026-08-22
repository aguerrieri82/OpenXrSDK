namespace XrEngine.Animation
{
    public interface IComputeFunction
    {
        float Duration { get; }
    }

    public interface IComputeFunction<TValue> : IComputeFunction
    {
        TValue GetValue(float t);
    }

    public interface IComputeFunction<TValue, TOptions> : IComputeFunction<TValue>, IOptionsProvider<TOptions>
    {
    }
}
