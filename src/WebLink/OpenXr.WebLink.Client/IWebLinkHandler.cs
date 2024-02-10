using OpenXr.WebLink.Entities;

namespace OpenXr.WebLink.Client
{
    public interface IWebLinkHandler
    {
        void OnObjectChanged(TrackInfo info);
    }
}
