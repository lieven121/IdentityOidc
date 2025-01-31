using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using Identity.App.Data;
using Quartz;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.App.Hosting;
public static class OpenIdictConfig
{
    public static IHostApplicationBuilder ConfigureOpenIdict(this IHostApplicationBuilder app)
    {
        app.Services.AddQuartz(options =>
        {
            options.UseSimpleTypeLoader();
            options.UseInMemoryStore();
        });

        app.Services.AddOpenIddict()

            // Register the OpenIddict core components.
            .AddCore(options =>
            {
                // Configure OpenIddict to use the Entity Framework Core stores and models.
                // Note: call ReplaceDefaultEntities() to replace the default entities.
                options.UseEntityFrameworkCore()
                       .UseDbContext<ApplicationDbContext>();

                options.UseQuartz();
            })
            .AddServer(options =>
            {
                // Enable the token endpoint.
                options.SetTokenEndpointUris("connect/token");

                // Enable the authorization, logout, userinfo, and introspection endpoints.
                options.SetAuthorizationEndpointUris("connect/authorize")
                //options.SetLogoutEndpointUris("/connect/logout")
                //.SetIntrospectionEndpointUris("/connect/introspect")
                .SetUserInfoEndpointUris("/connect/userinfo");

                var scopes = new List<string>
                {
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                }.Select(s => s.Replace(Permissions.Prefixes.Scope, ""));

                options.RegisterScopes(scopes.ToArray());

                //allowed grant types

                //options.AllowClientCredentialsFlow();
                options.AllowAuthorizationCodeFlow();
                options.AllowRefreshTokenFlow();

                // Register the signing and encryption credentials.
                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                // Register the ASP.NET Core host and configure the ASP.NET Core options.
                options.UseAspNetCore()
                       .EnableTokenEndpointPassthrough()
                       .EnableAuthorizationEndpointPassthrough();
            //})
            //.AddValidation(options =>
            //{
            //    // Note: the validation handler uses OpenID Connect discovery
            //    // to retrieve the address of the introspection endpoint.
            //    options.SetClientId(openIddictSettings.IdentityClientId);

            //    // Import the configuration from the local OpenIddict server instance.
            //    options.UseLocalServer();

            //    // Register the System.Net.Http. integration
            //    options.UseSystemNetHttp();

            //    // Register the ASP.NET Core host.
            //    options.UseAspNetCore();
            });

        //app.Services
        //    .AddRazorPages();

        return app;
    }

    public static IApplicationBuilder UseOpenIdict(this WebApplication app)
    {
        app.UseCors();

        return app;
    }
}
