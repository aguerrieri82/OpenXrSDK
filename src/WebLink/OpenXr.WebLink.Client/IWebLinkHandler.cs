using OpenXr.WebLink.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.WebLink.Client
{
    public interface IWebLinkHandler
    {
        void OnObjectChanged(TrackInfo info);
    }
}
