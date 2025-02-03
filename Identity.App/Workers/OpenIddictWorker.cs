
using Identity.App.Data;
using Identity.App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.App.Workers;

[RegisterHostedService]
public class OpenIddictWorker(IServiceProvider serviceProvider, IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        //var openIddicttSettings = new OpenIddictSettings();
        //configuration.GetSection("OpenIddict").Bind(openIddicttSettings);

        //using var scope = serviceProvider.CreateScope();

        //var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        //await context.Database.EnsureCreatedAsync(cancellationToken);

        //await CreateScopesAsync(scope, cancellationToken);
        //return;

        using var scope = serviceProvider.CreateScope();

        //scope.

        await CreateApplicationsAsync(scope, cancellationToken);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var userStore = scope.ServiceProvider.GetRequiredService<IUserStore<ApplicationUser>>();
        var userEmailStore = userStore as IUserEmailStore<ApplicationUser>;

        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();

        if (await userManager.FindByEmailAsync("admin@localhost") == null)
        {
            var res = await userManager.CreateAsync(new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@localhost",
                EmailConfirmed = true
            }, "Azerty123$");
        }

    }

    private async Task CreateApplicationsAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var applications = configuration.GetSection("OpenIddict:ApplicationConfigs").Get<IEnumerable<ApplicationConfig>>();

        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        foreach (var applicationConfig in applications)
        {
            var client = await manager.FindByClientIdAsync(applicationConfig.ClientId, cancellationToken);
            if(client != null)
            {
                await manager.DeleteAsync(client);
                client = null;
            }

            if (client == null)
            {
                var app = new OpenIddictApplicationDescriptor
                {
                    ClientId = applicationConfig.ClientId,
                    DisplayName = applicationConfig.Name,
                    Permissions =
                    {
                        Permissions.Endpoints.Authorization,
                        Permissions.Endpoints.EndSession,
                        Permissions.Endpoints.Token,
                        Permissions.GrantTypes.AuthorizationCode,
                        Permissions.GrantTypes.RefreshToken,
                        Permissions.ResponseTypes.Code,
                        Permissions.Scopes.Email,
                        Permissions.Scopes.Profile,
                        Permissions.Scopes.Roles,
                        Permissions.Prefixes.Scope + applicationConfig.Scope,
                        Permissions.Prefixes.Scope + Scopes.OfflineAccess,
                    },
                    ClientType = string.IsNullOrWhiteSpace(applicationConfig.ClientSecret) ? ClientTypes.Public : ClientTypes.Confidential,
                    ClientSecret = string.IsNullOrWhiteSpace(applicationConfig.ClientSecret) ? null : applicationConfig.ClientSecret,

                };
                if(applicationConfig.PKCE)
                {
                    app.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
                }
                if(applicationConfig.RedirectUri != null)
                    foreach (var uri in applicationConfig.RedirectUri)
                    {
                        app.RedirectUris.Add(new Uri(uri));
                    }

                if (applicationConfig.PostLogoutRedirectUri != null)
                    foreach (var uri in applicationConfig.PostLogoutRedirectUri)
                    {
                        app.PostLogoutRedirectUris.Add(new Uri(uri));
                    }


                await manager.CreateAsync(app, cancellationToken);
            }

        }

    }

    //private async Task CreateApplicationsAsync(IServiceScope scope, CancellationToken cancellationToken)
    //{
    //    // ReSharper disable once AccessToDisposedClosure
    //    var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

    //    if (await manager.FindByClientIdAsync(openIddicttSettings.ClientId, cancellationToken) == null)
    //    {
    //        var descriptor = new OpenIddictApplicationDescriptor
    //        {
    //            ClientId = openIddicttSettings.ClientId,
    //            DisplayName = openIddicttSettings.ClientDisplayName,
    //            PostLogoutRedirectUris = { new Uri(new Uri(openIddicttSettings.ClientUrl), relativeUri: "/signout-oidc") },
    //            RedirectUris = { new Uri(new Uri(openIddicttSettings.ClientUrl), relativeUri: "/signin-oidc") },
    //            Permissions =
    //                {
    //                    Permissions.Endpoints.Authorization,
    //                    Permissions.Endpoints.EndSession,
    //                    Permissions.Endpoints.Token,
    //                    Permissions.GrantTypes.AuthorizationCode,
    //                    Permissions.GrantTypes.RefreshToken,
    //                    Permissions.ResponseTypes.Code,
    //                    //Permissions.Scopes.Email,
    //                    //Permissions.Scopes.Profile,
    //                    Permissions.Scopes.Roles,
    //                    Permissions.Prefixes.Scope + openIddicttSettings.ApiScope,
    //                    Permissions.Prefixes.Scope + Scopes.OfflineAccess,
    //                    Permissions.Prefixes.Scope + Scopes.OpenId,
    //                    Permissions.Prefixes.Scope + Scopes.Email,
    //                    Permissions.Prefixes.Scope + Scopes.Profile
    //                },
    //            ClientType = ClientTypes.Public
    //        };

    //        await manager.CreateAsync(descriptor, cancellationToken);
    //    }

    //    if (await manager.FindByClientIdAsync(openIddicttSettings.ApiClientId, cancellationToken) == null)
    //    {
    //        var descriptor = new OpenIddictApplicationDescriptor
    //        {
    //            ClientId = openIddicttSettings.ApiClientId,
    //            ClientSecret = openIddicttSettings.ApiSecret,
    //            DisplayName = openIddicttSettings.ApiClientDisplayName,
    //            Permissions =
    //                {
    //                    //Permissions.Endpoints.Introspection,
    //                    Permissions.Endpoints.Token,
    //                    Permissions.GrantTypes.ClientCredentials,
    //                    Permissions.ResponseTypes.Token,
    //                    Permissions.Prefixes.Scope + openIddicttSettings.ApiScope,
    //                    Permissions.Prefixes.Scope + openIddicttSettings.IdentityScope,
    //                },
    //            ClientType = ClientTypes.Confidential
    //        };

    //        await manager.CreateAsync(descriptor, cancellationToken);
    //    }

    //    if (await manager.FindByClientIdAsync(openIddicttSettings.SwaggerClientId, cancellationToken) == null)
    //    {
    //        var descriptor = new OpenIddictApplicationDescriptor
    //        {
    //            ClientId = openIddicttSettings.SwaggerClientId,
    //            ClientSecret = openIddicttSettings.SwaggerSecret,
    //            DisplayName = openIddicttSettings.SwaggerClientDisplayName,
    //            Permissions =
    //                {
    //                    //Permissions.Endpoints.Introspection,
    //                    Permissions.Endpoints.Token,
    //                    Permissions.GrantTypes.AuthorizationCode,
    //                    Permissions.GrantTypes.ClientCredentials,
    //                    Permissions.ResponseTypes.CodeIdTokenToken,
    //                    Permissions.Prefixes.Scope + openIddicttSettings.SwaggerScope,
    //                    Permissions.Prefixes.Scope + openIddicttSettings.IdentityScope,
    //                    Permissions.Prefixes.Scope + openIddicttSettings.ApiScope,
    //                },
    //            ClientType = ClientTypes.Confidential
    //        };

    //        await manager.CreateAsync(descriptor, cancellationToken);
    //    }
    //}

    //private async Task CreateScopesAsync(IServiceScope scope, CancellationToken cancellationToken)
    //{
    //    // ReSharper disable once AccessToDisposedClosure
    //    var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

    //    if (await manager.FindByNameAsync(openIddicttSettings.ApiScope, cancellationToken) == null)
    //    {
    //        var descriptor = new OpenIddictScopeDescriptor { Name = openIddicttSettings.ApiScope, Resources = { openIddicttSettings.ApiClientId } };
    //        await manager.CreateAsync(descriptor, cancellationToken);
    //    }

    //    if (await manager.FindByNameAsync(openIddicttSettings.IdentityScope, cancellationToken) == null)
    //    {
    //        var descriptor = new OpenIddictScopeDescriptor
    //        {
    //            Name = openIddicttSettings.IdentityScope,
    //            Resources = { openIddicttSettings.IdentityClientId, }
    //        };
    //        await manager.CreateAsync(descriptor, cancellationToken);
    //    }

    //    if (await manager.FindByNameAsync(openIddicttSettings.SwaggerScope, cancellationToken) == null)
    //    {
    //        var descriptor = new OpenIddictScopeDescriptor
    //        {
    //            Name = openIddicttSettings.SwaggerScope,
    //            Resources = { openIddicttSettings.SwaggerClientId }
    //        };
    //        await manager.CreateAsync(descriptor, cancellationToken);
    //    }
    //}

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
