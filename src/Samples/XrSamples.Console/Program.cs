
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenXr.Framework;
using XrSamples;

if (args.Length > 0 && args[0].Equals("android-benchmark", StringComparison.OrdinalIgnoreCase))
{
    await AndroidBenchmark.RunAsync(args.ElementAtOrDefault(1));
    return;
}

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

await WindowSceneApp.Run(host.Services);
//await XrSceneApp.Run(host.Services);
//await SceneAnchors.Run(host.Services, logger);
//await Physics.Run(host.Services, logger);

//await host.StopAsync();

