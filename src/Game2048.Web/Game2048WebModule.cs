using System.Diagnostics;
using System.Text.Json;
using Game2048Model = Game2048.Game.Game2048;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.Studio.Client.AspNetCore;
using Volo.Abp.Auditing;

namespace Game2048.Web;

[DependsOn(typeof(AbpAspNetCoreMvcModule))]
[DependsOn(typeof(AbpAutofacModule))]
[DependsOn(typeof(AbpStudioClientAspNetCoreModule))]
public class Game2048WebModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        IConfiguration configuration = context.Services.GetConfiguration();

        // 强制开启全量审计上报
        Configure<AbpAuditingOptions>(options =>
        {
            options.IsEnabled = true; 
            options.IsEnabledForGetRequests = true;     // 关键：强制记录 GET 请求
            options.IsEnabledForAnonymousUsers = true;  // 关键：未登录的测试请求也强制记录
        });

        Configure<MvcOptions>(options =>
        {
            options.Conventions.Add(new ConditionalTestApiControllerConvention(
                configuration.GetValue<bool>("Game2048:EnableTestApi")));
        });

        Configure<JsonOptions>(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        IConfiguration configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
        ConfigureGame2048(configuration);

        IApplicationBuilder app = context.GetApplicationBuilder();
        ILogger requestLogger = context.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("LegacyRequestLogging");

        app.Use(async (httpContext, next) =>
        {
            string method = httpContext.Request.Method;
            string path = httpContext.Request.Path.HasValue ? httpContext.Request.Path.Value! : "/";
            string queryString = httpContext.Request.QueryString.HasValue ? httpContext.Request.QueryString.Value! : string.Empty;
            long startTimestamp = Stopwatch.GetTimestamp();

            try
            {
                await next();
            }
            finally
            {
                requestLogger.LogInformation(
                    "LegacyRequest {Method} {Path}{QueryString} => {StatusCode} ({ElapsedMilliseconds:0.0} ms)",
                    method,
                    path,
                    queryString,
                    httpContext.Response.StatusCode,
                    Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);
            }
        });

        app.UseCorrelationId();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAbpStudioLink();
        app.UseAuditing();
        app.UseConfiguredEndpoints();

        if (app is WebApplication webApplication)
        {
            webApplication.MapFallbackToFile("index.html");
        }
    }

    private static void ConfigureGame2048(IConfiguration configuration)
    {
        string connectionString = configuration["Game2048:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = Game2048Model.GetDefaultConnectionString();
        }

        Game2048Model.ConfigurePersistence(connectionString);
        Game2048Model.EnsureDatabaseReady();
        Game2048Model.ConfigureGeneratedTileValue(configuration["Game2048:ForcedGeneratedTileValue"]);
        Game2048Model.ConfigureLeaderboardWallUrl(configuration["Game2048:LeaderboardWallUrl"]);
    }
}
