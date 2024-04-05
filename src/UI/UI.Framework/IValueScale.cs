namespace CanvasUI
{
    public interface IValueScale
    {
        float ToScale(float value);

        float FromScale(float scaleValue);
    }
}
