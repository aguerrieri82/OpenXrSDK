namespace XrEngine
{
    public enum QueryTextureType
    {
        Color
    }

    public interface IRenderPass
    {
        Texture2D QueryTexture(QueryTextureType type);
    }
}
