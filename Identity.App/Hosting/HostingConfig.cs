using Microsoft.AspNetCore.HttpOverrides;

namespace Identity.App.Hosting;
public static class HostingConfig
{
    public static IApplicationBuilder UseUrlsFromConfig(this WebApplication app)
    {
        var httpsRedirection = app.Configuration.GetSection("Hosting:HttpsRedirection")?.Get<bool>() != false;
        if (httpsRedirection)
            app.UseHttpsRedirection();

        var urls = app.Configuration.GetSection("Hosting:Urls")?.Get<string[]>();
        if (urls != null && urls.Any())
        {
            app.Urls.Clear();
            foreach (var url in urls)
            {
                app.Urls.Add(url);
            }
        }
        return app;
    }


    public static IHostApplicationBuilder ConfigureReverseProxySupport(this IHostApplicationBuilder app)
    {

        var rProxySupport = app.Configuration.GetSection("Hosting:ReverseProxySupport")?.Get<bool>() != false;
        if(rProxySupport)
            app.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
            });

        return app;
    }

    public static WebApplication UseReverseProxySupport(this WebApplication app)
    {
        var rProxySupport = app.Configuration.GetSection("Hosting:ReverseProxySupport")?.Get<bool>() != false;

        if(rProxySupport)
        {
            app.UseCertificateForwarding();
            var forwardedHeaderOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };
            forwardedHeaderOptions.KnownIPNetworks.Clear();
            forwardedHeaderOptions.KnownProxies.Clear();
            var ips = app.Configuration.GetSection("Hosting:ReverseProxyAddresses")?.Get<string[]?>();
            foreach (var ip in ips ?? Array.Empty<string>())
            {
                if (System.Net.IPAddress.TryParse(ip, out var address))
                    forwardedHeaderOptions.KnownProxies.Add(address);
                else if (System.Net.IPNetwork.TryParse(ip, out var network))
                    forwardedHeaderOptions.KnownIPNetworks.Add(network);
                else
                    app.Logger.LogWarning("Ignoring invalid Hosting:ReverseProxyAddresses entry '{Entry}'", ip);
            }

            // Empty known lists mean X-Forwarded-* is trusted from any client (kept for backwards
            // compatibility), which lets callers spoof their IP and scheme.
            if (forwardedHeaderOptions.KnownProxies.Count == 0 && forwardedHeaderOptions.KnownIPNetworks.Count == 0)
                app.Logger.LogWarning("Hosting:ReverseProxyAddresses is empty; forwarded headers are trusted from any source. Configure the proxy IPs/CIDRs.");

            app.UseForwardedHeaders(forwardedHeaderOptions);
        }

        var assumeEveryRequestHttps = app.Configuration.GetSection("Hosting:AssumeEveryRequestHttps")?.Get<bool>() == true;
        if (assumeEveryRequestHttps)
            app.Use((context, next) =>
            {
                context.Request.Scheme = "https";

                return next();
            });

        return app;
    }

    public static WebApplication UseSecurityHeaders(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            app.UseHsts();

        app.Use((context, next) =>
        {
            var headers = context.Response.Headers;
            // Prevent clickjacking of the login and account pages.
            headers["X-Frame-Options"] = "DENY";
            headers["Content-Security-Policy"] = "frame-ancestors 'none'";
            headers["X-Content-Type-Options"] = "nosniff";
            headers["Referrer-Policy"] = "no-referrer";

            return next();
        });

        return app;
    }

}
