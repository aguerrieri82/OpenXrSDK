
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using OpenXr.Framework.Vulkan;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);


builder.Services.AddSingleton<IXrThread, XrCurrentThread>();
builder.Services.AddSingleton(new XrApp(
    new XrVulkanGraphicDriver(new VulkanDevice()),
    new OculusXrPlugin()));
builder.Services.AddOpenXrWebLink();

WebApplication app = builder.Build();

app.UseOpenXrWebLink();

app.Run();
