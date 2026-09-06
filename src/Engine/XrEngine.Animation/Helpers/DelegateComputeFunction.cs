namespace XrEngine.Animation
{
    public delegate TValue ComputeFunctionDelegate<TValue>(float t);

    public delegate TValue ComputeFunctionOptionsDelegate<TValue, TOptions>(float t, TOptions options);

    public delegate float ComputeFunctionDurationDelegate<TOptions>(TOptions options);

    public readonly struct DelegateComputeFunction<TValue> : IComputeFunction<TValue>
    {
        readonly ComputeFunctionDelegate<TValue> _getValue;
        readonly float _duration;

        public DelegateComputeFunction(ComputeFunctionDelegate<TValue> getValue, float duration)
        {
            _getValue = getValue;
            _duration = duration;
        }

        public TValue GetValue(float t)
        {
            return _getValue(t);
        }

        public float Duration => _duration;
    }

    public struct DelegateComputeFunction<TValue, TOptions> : IComputeFunction<TValue, TOptions>
    {
        readonly ComputeFunctionOptionsDelegate<TValue, TOptions> _getValue;
        readonly ComputeFunctionDurationDelegate<TOptions> _getDuration;
        TOptions _options;

        public DelegateComputeFunction(
            ComputeFunctionOptionsDelegate<TValue, TOptions> getValue,
            ComputeFunctionDurationDelegate<TOptions> getDuration,
            TOptions options = default!)
        {
            _getValue = getValue;
            _getDuration = getDuration;
            _options = options;
        }

        public readonly TValue GetValue(float t)
        {
            return _getValue(t, _options);
        }

        public TOptions Options
        {
            get => _options;
            set => _options = value;
        }

        public readonly float Duration => _getDuration(_options);
    }

}