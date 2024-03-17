using Microsoft.Extensions.Logging;
using OpenXr.Framework;
using XrWebLink.Client;
using XrWebLink.Entities;

namespace XrSamples
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
