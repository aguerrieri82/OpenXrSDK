namespace OpenXr.Framework
{
    public class XrPathAttribute : Attribute
    {
        public XrPathAttribute(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }
}
