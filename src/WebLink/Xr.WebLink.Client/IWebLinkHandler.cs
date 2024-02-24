using Xr.WebLink.Entities;

namespace Xr.WebLink.Client
{
    public interface IWebLinkHandler
    {
        void OnObjectChanged(TrackInfo info);
    }
}
