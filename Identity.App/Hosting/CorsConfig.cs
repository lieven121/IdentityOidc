using Identity.App.Models;
using Identity.App.Services;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace Identity.App.Hosting;
public static class CorsConfig
{
    public const string ApiPolicy = "api";

    public static IHostApplicationBuilder ConfigureCors(this IHostApplicationBuilder app)
    {
        var configuration = app.Configuration;
        var services = app.Services;

        var apiOrigins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
        var origins = apiOrigins;

        var applications = configuration.GetSection("OpenIddict:ApplicationConfigs").Get<IEnumerable<ApplicationConfig>>();
        if(applications?.Any() == true)
        {
            origins = origins.Concat(
                applications.SelectMany(
                    a => a.RedirectUri?.Select(r => GetBaseAddressFromRedirectUri(r)) ?? Enumerable.Empty<string>())
            ).ToArray();
        }

        services.AddCors(options =>
        {
            // OIDC endpoints (discovery, token, userinfo) are called with bearer tokens / client
            // credentials, so client origins never need the identity cookie. Origins of applications
            // managed in the admin UI are added per request by ApplicationCorsPolicyProvider.
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod();
            });

            // Cookie-authenticated API (account + admin): only explicitly trusted origins.
            options.AddPolicy(ApiPolicy, policy =>
            {
                policy.WithOrigins(apiOrigins)
                .AllowAnyHeader()
                .AllowCredentials()
                .AllowAnyMethod();
            });
        });
        services.AddSingleton<ICorsPolicyProvider, ApplicationCorsPolicyProvider>();

        return app;
    }

    public static IApplicationBuilder Cors(this WebApplication app)
    {
        app.UseCors();

        return app;
    }

    private static string GetBaseAddressFromRedirectUri(string redirectUri)
    {
        var uri = new Uri(redirectUri);
        return uri.GetLeftPart(UriPartial.Authority);
    }

    /// <summary>
    /// Extends the default policy with the origins of all applications in the store, so
    /// applications created at runtime work without a restart. Named policies are unchanged.
    /// </summary>
    private sealed class ApplicationCorsPolicyProvider(IOptions<CorsOptions> options, ApplicationOrigins applicationOrigins)
        : ICorsPolicyProvider
    {
        private readonly DefaultCorsPolicyProvider inner = new(options);

        public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
        {
            var policy = await inner.GetPolicyAsync(context, policyName);
            if (policyName != null || policy == null)
                return policy;

            var origin = context.Request.Headers.Origin.ToString();
            if (string.IsNullOrEmpty(origin) || policy.IsOriginAllowed(origin))
                return policy;

            var known = await applicationOrigins.GetAsync(context.RequestAborted);
            if (!known.Contains(origin))
                return policy;

            return new CorsPolicyBuilder(policy).WithOrigins(origin).Build();
        }
    }
}
