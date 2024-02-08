
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using OpenXr.Framework.Vulkan;
using OpenXr.WebLink;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddSingleton<IXrThread, CurrentXrThread>();
builder.Services.AddSingleton(new XrApp(
    new XrVulkanGraphicDriver(new VulkanDevice()),
    new OculusXrPlugin()));
builder.Services.AddOpenXrWebLink();

var app = builder.Build();

app.UseOpenXrWebLink();

app.Run();
