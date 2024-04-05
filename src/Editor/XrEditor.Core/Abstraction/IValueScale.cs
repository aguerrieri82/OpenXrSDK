namespace XrEditor
{
    public interface IValueScale
    {
        float ValueToScale(float value);

        float ScaleToValue(float scaleValue);

        string? Format(float scaleValue);

        float ScaleMin { get; }

        float ScaleMax { get; }

        float ScaleStep { get; }

        float ScaleSmallStep { get; }

        int DecimalDigits { get; }
    }
}
