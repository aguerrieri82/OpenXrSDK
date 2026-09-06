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
            if (scaleValue != 0.0f && MathF.Abs(scaleValue) < MathF.Pow(10.0f, -DecimalDigits))
                return scaleValue.ToString("0." + new string('#', DecimalDigits) + "E+0");

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
