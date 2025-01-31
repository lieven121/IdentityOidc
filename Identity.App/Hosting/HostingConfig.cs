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

}
