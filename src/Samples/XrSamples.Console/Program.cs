
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenXr.Framework;
using System.IO;
using XrEngine;




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

var data2 = PvrTranscoder.Instance.Read(File.OpenRead("d:\\real-pvr.pvr"));

string fileName = "D:\\Development\\Library\\glTF-Sample-Viewer\\assets\\environments\\pisa.hdr";

var data = HdrReader.Instance.Read(File.OpenRead(fileName));

using (var stream = File.OpenWrite("d:\\test.pvr"))
    PvrTranscoder.Instance.Write(stream, data);


var logger = host.Services.GetRequiredService<ILogger<object>>();

//await WebLinkApp.Run(host.Services, logger);
//await Tasks.OvrLibTask(logger);
//await XrSceneApp.Run(host.Services, logger);
//await SceneAnchors.Run(host.Services, logger);
//await Physics.Run(host.Services, logger);

await host.StopAsync();


