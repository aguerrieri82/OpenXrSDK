namespace CanvasUI
{
    public class LinearScale : IValueScale
    {
        LinearScale() { }

        public float FromScale(float scaleValue)
        {
            return scaleValue;
        }

        public float ToScale(float value)
        {
            return value;
        }

        public static readonly LinearScale Instance = new();
    }
}
