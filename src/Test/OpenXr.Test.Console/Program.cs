
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenXr;
using OpenXr.Engine;
using OpenXr.Framework;
using OpenXr.Samples;
using System.Diagnostics;
using System.Numerics;

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

var matrix1 = Matrix4x4.CreateScale(0.5f) *
            Matrix4x4.CreateTranslation(1f, 0, 0);

var matrix2 = Matrix4x4.CreateTranslation(1f, 0, 0) *
              Matrix4x4.CreateScale(0.5f);


var vector = new Vector3(1, 1, 1);

Debug.WriteLine(vector.Transform(matrix1));
Debug.WriteLine(vector.Transform(matrix2));



Gpu.EnableNvAPi();

var logger = host.Services.GetRequiredService<ILogger<object>>();

//Tasks.LoadModel("Assets/DamagedHelmet.gltf");
for (int i = 0; i < 1000; i++)
{
    var now = DateTime.UtcNow;
    Tasks.CompressTexture("Assets/TestScreen.png");
    Console.WriteLine((DateTime.UtcNow - now).TotalSeconds);
}


//await WebLinkApp.Run(host.Services, logger);
await WindowSceneApp.Run(host.Services, logger);
//await XrSceneApp.Run(host.Services, logger);
//await SceneAnchors.Run(host.Services, logger);

await host.StopAsync();


