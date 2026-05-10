

using Identity.App.Data;
using Identity.App.Models;
using Identity.App.Models.Consts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.App.Workers;

[RegisterHostedService]
public class OpenIddictWorker(IServiceProvider serviceProvider, IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Database
            .MigrateAsync();

        await CreateRolesAsync(scope, cancellationToken);
        await CreateApplicationsAsync(scope, cancellationToken);
        await CreateUsersAsync(scope, cancellationToken);
    }

    private async Task CreateRolesAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var roles = configuration.GetSection("OpenIddict:Roles").Get<IEnumerable<string>>() 
                    ?? new[] { Roles.Admin, Roles.User };
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (result.Succeeded)
                {
                    Console.WriteLine($"Created role: {roleName}");
                }
                else
                {
                    Console.Error.WriteLine($"Failed to create role {roleName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }

    private async Task CreateApplicationsAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var applications = configuration.GetSection("OpenIddict:ApplicationConfigs").Get<IEnumerable<ApplicationConfig>>();

        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
        var scopeDictionary = new Dictionary<string, OpenIddictScopeDescriptor>();


        foreach (var applicationConfig in applications)
        {
            var client = await manager.FindByClientIdAsync(applicationConfig.ClientId, cancellationToken);
            if(client != null)
            {
                await manager.DeleteAsync(client);
            }

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
                    Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OpenId,
                    Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess,
                },
                ClientType = string.IsNullOrWhiteSpace(applicationConfig.ClientSecret) ? ClientTypes.Public : ClientTypes.Confidential,
                ClientSecret = string.IsNullOrWhiteSpace(applicationConfig.ClientSecret) ? null : applicationConfig.ClientSecret,
                
            };

            // Store required roles in application properties
            if (applicationConfig.RequiredRoles != null && applicationConfig.RequiredRoles.Any())
            {
                app.Properties["RequiredRoles"] = System.Text.Json.JsonSerializer.SerializeToElement(
                    applicationConfig.RequiredRoles.ToList());
            }

            var appScopes = applicationConfig.Scope.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var appScope in appScopes)
            {
                app.Permissions.Add(Permissions.Prefixes.Scope + appScope);
                if (!scopeDictionary.TryAdd(appScope, new OpenIddictScopeDescriptor
                {
                    Name = appScope,
                    DisplayName = appScope,
                    Resources =
                {
                    applicationConfig.ClientId
                }
                }))
                {
                    scopeDictionary[appScope].Resources.Add(applicationConfig.ClientId);
                }
            }

            if (app.ClientType == ClientTypes.Confidential)
            {
                app.Permissions.Add(Permissions.GrantTypes.ClientCredentials);
                app.Permissions.Add(Permissions.ResponseTypes.Token);
                //app.Permissions.Remove(Permissions.GrantTypes.AuthorizationCode);
            }

            if(applicationConfig.PKCE)
            {
                app.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
            } else {
                app.Requirements.Remove(Requirements.Features.ProofKeyForCodeExchange);
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


        foreach (var appScope in scopeDictionary)
        {
            var s = await scopeManager.FindByNameAsync(appScope.Key);
            if (s != null)
            {
                await scopeManager.DeleteAsync(s);
            }
            await scopeManager.CreateAsync(appScope.Value);
        }
    }

    private async Task CreateUsersAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var users = configuration.GetSection("OpenIddict:Users").Get<IEnumerable<UserConfig>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userStore = scope.ServiceProvider.GetRequiredService<IUserStore<ApplicationUser>>();

        foreach (var userConfig in users ?? Enumerable.Empty<UserConfig>())
        {
            if (string.IsNullOrWhiteSpace(userConfig.Email))
                continue;

            var user = await userManager.FindByEmailAsync(userConfig.Email);
            
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = userConfig.Username,
                    Email = userConfig.Email,
                    EmailConfirmed = true
                };
                
                if (string.IsNullOrWhiteSpace(userConfig.Password))
                {
                    userConfig.Password = Guid.NewGuid().ToString();
                    //add 3 random upper case letters
                    userConfig.Password += new string(Enumerable.Range(0, 3).Select(_ => (char)Random.Shared.Next('A', 'Z')).ToArray());
                    Console.WriteLine($"Creating user {userConfig.Email} with password '{userConfig.Password}'");
                }
                
                await userManager.CreateAsync(user, userConfig.Password);
                Console.WriteLine($"Created user {userConfig.Email}");
            }

            // Get desired roles from configuration
            var desiredRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Add roles from Roles property
            if (userConfig.Roles != null)
            {
                foreach (var role in userConfig.Roles)
                {
                    if (!string.IsNullOrWhiteSpace(role))
                        desiredRoles.Add(role);
                }
            }

            // Get current roles
            var currentRoles = await userManager.GetRolesAsync(user);
            var currentRolesSet = new HashSet<string>(currentRoles, StringComparer.OrdinalIgnoreCase);

            // Add missing roles
            foreach (var role in desiredRoles)
            {
                if (!currentRolesSet.Contains(role))
                {
                    // Ensure role exists
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        Console.WriteLine($"Warning: Role '{role}' does not exist. Skipping assignment to user {userConfig.Email}");
                        continue;
                    }
                    
                    var result = await userManager.AddToRoleAsync(user, role);
                    if (result.Succeeded)
                    {
                        Console.WriteLine($"Added user {userConfig.Email} to role: {role}");
                    }
                    else
                    {
                        Console.Error.WriteLine($"Failed to add user {userConfig.Email} to role {role}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }

            // Remove roles that are no longer desired
            foreach (var role in currentRoles)
            {
                if (!desiredRoles.Contains(role))
                {
                    var result = await userManager.RemoveFromRoleAsync(user, role);
                    if (result.Succeeded)
                    {
                        Console.WriteLine($"Removed user {userConfig.Email} from role: {role}");
                    }
                    else
                    {
                        Console.Error.WriteLine($"Failed to remove user {userConfig.Email} from role {role}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }
    }


    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
