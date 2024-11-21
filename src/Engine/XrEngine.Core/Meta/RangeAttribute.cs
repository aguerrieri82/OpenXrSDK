namespace XrEngine
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class RangeAttribute : Attribute
    {
        public RangeAttribute(float min, float max, float step)
        {
            Min = min;
            Max = max;
            Step = step;
        }

        public float Min { get; set; }

        public float Max { get; set; }

        public float Step { get; set; }
    }
}
