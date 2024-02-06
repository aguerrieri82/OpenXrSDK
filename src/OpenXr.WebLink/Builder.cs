using Microsoft.AspNetCore.Builder;
using OpenXr.Framework;
using OpenXr.WebLink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
                    .AddHubOptions<OpenXrHub>(o =>
                    {
                        o.EnableDetailedErrors = true;
                        o.MaximumReceiveMessageSize = 50 * 1024 * 1024;
                    });

            services.AddSingleton<OpenXrService>();
            if (hostService)
                services.AddHostedService(sp => sp.GetRequiredService<OpenXrService>());

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
            app.MapHub<OpenXrHub>("hub/openxr");

            app.UseCors("AllowAll");

        }
    }
}
