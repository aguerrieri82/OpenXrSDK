namespace OpenXr.Framework
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class XrPathAttribute : Attribute
    {
        public XrPathAttribute(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }
}
