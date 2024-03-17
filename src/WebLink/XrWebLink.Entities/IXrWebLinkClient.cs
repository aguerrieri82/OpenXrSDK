namespace XrWebLink.Entities
{
    public interface IXrWebLinkClient
    {
        Task ObjectChanged(TrackInfo trackInfo);
    }
}
