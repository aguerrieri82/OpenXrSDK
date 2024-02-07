
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenXr;

using IHost host = Host.CreateDefaultBuilder(args)

    .ConfigureHostConfiguration(builder =>
    {

    })
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

Tasks.Services = host.Services;

await Tasks.AnchorsTask();

await host.StopAsync();


