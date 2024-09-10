namespace XrEngine.OpenXr
{
    public static class XrPlatform
    {
        static IXrEnginePlatform? _current;

        public static IXrEnginePlatform? Current
        {
            get => _current;
            set
            {
                _current = value;
                if (value != null)
                    Context.Implement(value);
            }
        }

        public static bool IsEditor => Current?.Name == "Editor";

        public static bool IsAndroid => Current?.Name == "Android";

        public static bool IsConsole => Current?.Name == "Console";
    }
}
