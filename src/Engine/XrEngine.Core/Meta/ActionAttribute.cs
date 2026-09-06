namespace XrEngine
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ActionAttribute : Attribute
    {
        public ActionAttribute(string? name = null)
        {
            Name = name;
        }

        public string? Name { get; set; }
    }
}
