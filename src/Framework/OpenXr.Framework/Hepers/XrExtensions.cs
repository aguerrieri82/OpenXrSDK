using System.Diagnostics;

namespace OpenXr.Framework
{
    public static class XrExtensions
    {

        public static void AddProjection(this XrLayerManager manager, RenderViewDelegate renderView)
        {
            manager.Layers.Add(new XrProjectionLayer(renderView));
        }

        public static void StartEventLoop(this XrApp app, Func<bool> isExited, int poolPeriodMs = 50)
        {
            _ = Task.Run(async () =>
            {
                while (!isExited())
                {
                    if (!app.HandleEvents())
                        break;

                    await Task.Delay(poolPeriodMs);
                }
                Debug.WriteLine("---Event Loop exit---");
            });

        }
    }
}
