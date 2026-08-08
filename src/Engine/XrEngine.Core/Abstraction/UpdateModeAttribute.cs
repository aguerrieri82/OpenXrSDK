namespace XrEngine
{
    [AttributeUsage(AttributeTargets.Class)]
    public class UpdateModeAttribute : Attribute
    {

        public bool IsParallel { get; set; }
    }
}
