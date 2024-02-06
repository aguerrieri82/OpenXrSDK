using OpenXr.WebLink.Client;
using OpenXr.WebLink.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr
{
    public class WebLinkHandler : IWebLinkHandler
    {
        public void OnObjectChanged(TrackInfo info)
        {
            Console.WriteLine(info.Pose);
        }
    }
}
