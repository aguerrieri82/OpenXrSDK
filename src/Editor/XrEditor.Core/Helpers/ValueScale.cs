namespace XrEditor
{
    public class ValueScale : IValueScale
    {
        public ValueScale()
        {
            DecimalDigits = 3;
        }

        public float ScaleMin { get; set; }

        public float ScaleMax { get; set; }

        public float ScaleStep { get; set; }

        public float ScaleSmallStep { get; set; }

        public int DecimalDigits { get; set; }

        public virtual string? Format(float scaleValue)
        {
            return Math.Round(scaleValue, DecimalDigits).ToString();
        }

        public virtual float ScaleToValue(float scaleValue)
        {
            return scaleValue;
        }

        public virtual float ValueToScale(float value)
        {
            return value;
        }
    }
}
