namespace XrSamples
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SampleAttribute : Attribute
    {
        public SampleAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
