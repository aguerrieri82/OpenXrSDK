using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.WebLink.Entities
{
    public interface IXrWebLinkClient
    {
        Task ObjectChanged(TrackInfo trackInfo);
    }
}
