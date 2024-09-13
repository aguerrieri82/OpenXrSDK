
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenXr.Framework;
using XrSamples;



var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging((ctx, logging) =>
    {
        logging.AddConfiguration(ctx.Configuration)
               .AddOneLineConsole();
    })
    .ConfigureServices((ctx, services) =>
    {
        var envName = ctx.HostingEnvironment.EnvironmentName;

    })
    .Build();

_ = host.RunAsync();

Gpu.EnableNvAPi();

Tasks.Services = host.Services;
Tasks.ReadRtsp();

//await Tasks.OvrLibTask(logger);
//await WindowSceneApp.Run(host.Services);
//await XrSceneApp.Run(host.Services);
//await SceneAnchors.Run(host.Services, logger);
//await Physics.Run(host.Services, logger);

await host.StopAsync();


