namespace XrEngine
{
    public interface IEnvDepthProvider
    {
        Texture2D? Acquire(Camera depthCamera);

        float Bias { get; set; }

        bool Freeze { get; set; }

        bool Blur { get; set; }
    }
}
