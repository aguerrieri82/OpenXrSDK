namespace OpenXr.WebLink.Entities
{
    public interface IXrWebLinkClient
    {
        Task ObjectChanged(TrackInfo trackInfo);
    }
}
