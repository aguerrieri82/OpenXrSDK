using OpenXr.Framework.Oculus;
using OpenXr.Framework.Vulkan;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOpenXrWebLink([
    new VulkanGraphicDriver(new VulkanDevice()),
    new OculusXrPlugin()
]);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseOpenXrWebLink();

app.Run();
