using Microsoft.Extensions.Logging;
using OpenXr.WebLink.Client;
using OpenXr.WebLink.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Samples
{
    public class WebLinkApp
    {
        public class WebLinkHandler : IWebLinkHandler
        {
            public void OnObjectChanged(TrackInfo info)
            {
                Console.WriteLine(info.Pose);
            }
        }

        public static async Task Run(IServiceProvider services, ILogger logger)
        {
            var client = new WebLinkClient("http://192.168.1.221:8080", new WebLinkHandler());

            await client.ConnectAsync("");

            await client.StartSessionAsync();

            var anchors = await client.GetAnchorsAsync(new XrAnchorFilter
            {
                Components = XrAnchorComponent.All
            });

            await client.TrackObjectAsync(TrackObjectType.Head, null, true);

            Console.WriteLine("Press a key to exit");
            Console.ReadKey();

            await client.StopSessionAsync();
        }
    }
}
