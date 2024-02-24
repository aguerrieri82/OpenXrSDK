using Microsoft.AspNetCore.Builder;
using System.Text.Json;
using Xr.WebLink;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class Builder
    {
        public static void AddOpenXrWebLink(this IServiceCollection services, bool hostService = true)
        {
            services.AddSignalR()
                    .AddJsonProtocol(options =>
                    {
                        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                        options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
                        options.PayloadSerializerOptions.IncludeFields = true;
                    })
                    .AddHubOptions<XrWebLinkHub>(o =>
                    {
                        o.EnableDetailedErrors = true;
                        o.MaximumReceiveMessageSize = 50 * 1024 * 1024;
                    });

            services.AddSingleton<XrWebLinkService>();
            if (hostService)
                services.AddHostedService(sp => sp.GetRequiredService<XrWebLinkService>());

            services.AddCors(options => options.AddPolicy("AllowAll",
                builder =>
                {
                    builder.AllowAnyHeader()
                           .AllowAnyMethod()
                           .SetIsOriginAllowed((host) => true)
                           .AllowCredentials();
                }));
        }

        public static void UseOpenXrWebLink(this WebApplication app)
        {
            app.MapHub<XrWebLinkHub>("hub/openxr");

            app.UseCors("AllowAll");

        }
    }
}
