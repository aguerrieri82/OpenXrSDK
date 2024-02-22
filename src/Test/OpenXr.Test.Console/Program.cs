
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenXr;
using OpenXr.Engine;
using OpenXr.Framework;
using OpenXr.Samples;
using SkiaSharp;
using System.Diagnostics;
using System.Numerics;
using Xr.Engine.Compression;




var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging((ctx, logging) =>
    {
        logging.AddConfiguration(ctx.Configuration)
               .AddConsole();
    })
    .ConfigureServices((ctx, services) =>
    {
        var envName = ctx.HostingEnvironment.EnvironmentName;

    })
    .Build();

_ = host.RunAsync();

Gpu.EnableNvAPi();

var logger = host.Services.GetRequiredService<ILogger<object>>();

unsafe
{

    var data = EtcCompressor.Encode("d:\\11474523244911310074.jpg", 0);

    using var stream = File.OpenWrite("d:\\test.pvr");

    PvrDecoder.Instance.Write(stream, data);

}




//await WebLinkApp.Run(host.Services, logger);
await WindowSceneApp.Run(host.Services, logger);
//await XrSceneApp.Run(host.Services, logger);
//await SceneAnchors.Run(host.Services, logger);

await host.StopAsync();


