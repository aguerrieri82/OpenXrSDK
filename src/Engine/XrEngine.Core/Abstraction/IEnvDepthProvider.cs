namespace XrEngine
{
    public interface IEnvDepthProvider
    {
        Texture2D? Acquire(Camera depthCamera);

        Texture2D? Acquire(Camera depthCamera, out long lastFrameTime);

        float Bias { get; set; }

        bool Freeze { get; set; }

        bool Blur { get; set; }
    }
}
