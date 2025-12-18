
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenXr.Framework;
using XrSamples;


IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging((ctx, logging) =>
    {
        logging.AddConfiguration(ctx.Configuration)
               .AddOneLineConsole();
    })
    .ConfigureServices((ctx, services) =>
    {
        string envName = ctx.HostingEnvironment.EnvironmentName;

    })
    .Build();

_ = host.RunAsync();



Gpu.EnableNvAPi();

Tasks.Services = host.Services;

Tasks.ParseGeoTiff();
//Tasks.TestPivot();

return;

//await WindowSceneApp.Run(host.Services);
await XrSceneApp.Run(host.Services);
//await SceneAnchors.Run(host.Services, logger);
//await Physics.Run(host.Services, logger);

await host.StopAsync();


