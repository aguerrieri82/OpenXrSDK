using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenXr.Framework.Oculus;
using OpenXr.Framework.Vulkan;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddOpenXrWebLink([
    new VulkanGraphicDriver(new VulkanDevice()),
    new OculusXrPlugin()
]);

var app = builder.Build();

app.UseOpenXrWebLink();

app.Run();
