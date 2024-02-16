
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenXr;
using OpenXr.Framework;
using OpenXr.Samples;

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

Tasks.LoadModel("Assets/DamagedHelmet.gltf");

//await WebLinkApp.Run(host.Services, logger);
await WindowSceneApp.Run(host.Services, logger);
//await XrSceneApp.Run(host.Services, logger);
//await SceneAnchors.Run(host.Services, logger);

await host.StopAsync();


