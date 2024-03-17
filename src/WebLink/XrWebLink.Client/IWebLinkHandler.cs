using XrWebLink.Entities;

namespace XrWebLink.Client
{
    public interface IWebLinkHandler
    {
        void OnObjectChanged(TrackInfo info);
    }
}
