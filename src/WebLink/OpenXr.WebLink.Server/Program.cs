
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using OpenXr.Framework.Vulkan;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddProvider()



builder.Services.AddSingleton<IXrThread, XrCurrentThread>();
builder.Services.AddSingleton(new XrApp(
    new XrVulkanGraphicDriver(new VulkanDevice()),
    new OculusXrPlugin()));
builder.Services.AddOpenXrWebLink();

var app = builder.Build();

app.UseOpenXrWebLink();

var test = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.StartsWith("Microsoft.AspNetCore"));


app.Run();
