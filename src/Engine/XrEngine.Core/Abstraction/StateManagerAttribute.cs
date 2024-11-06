namespace XrEngine
{
    public enum StateManagerMode
    {
        Explicit,
        Auto
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class StateManagerAttribute : Attribute
    {
        public StateManagerAttribute(StateManagerMode mode)
        {
            Mode = mode;
        }

        public StateManagerMode Mode { get; }
    }
}
