using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Identity.App.Hosting;
public static class RateLimitingConfig
{
    public const string LoginPolicy = "login";
    public const string TokenPolicy = "token";

    public static IHostApplicationBuilder ConfigureRateLimiting(this IHostApplicationBuilder app)
    {
        var configuration = app.Configuration;

        app.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            AddPerIpPolicy(options, LoginPolicy, configuration.GetSection("RateLimiting:Login"), defaultPermitLimit: 10);
            AddPerIpPolicy(options, TokenPolicy, configuration.GetSection("RateLimiting:Token"), defaultPermitLimit: 60);
        });

        return app;
    }

    // Partitioned on the client IP, so it relies on Hosting:ReverseProxyAddresses being set when behind a proxy.
    private static void AddPerIpPolicy(RateLimiterOptions options, string policyName, IConfigurationSection section, int defaultPermitLimit)
    {
        var permitLimit = section.GetValue<int?>("PermitLimit") ?? defaultPermitLimit;
        var window = TimeSpan.FromSeconds(section.GetValue<int?>("WindowSeconds") ?? 60);

        options.AddPolicy(policyName, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueLimit = 0,
                }));
    }
}
