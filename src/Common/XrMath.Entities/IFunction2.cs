namespace XrMath
{
    public interface IFunction2
    {
        float Value(float x);

        Bounds1 RangeX { get; }
    }
}
