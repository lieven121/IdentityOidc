

using Identity.App.Data;
using Identity.App.Helpers;
using Identity.App.Models;
using Identity.App.Models.Consts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.App.Workers;

[RegisterHostedService]
public class OpenIddictWorker(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<OpenIddictWorker> logger) : IHostedService
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
                    logger.LogInformation("Created role {Role}", roleName);
                }
                else
                {
                    logger.LogError("Failed to create role {Role}: {Errors}", roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
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

            // Public clients can't authenticate at the token endpoint, so PKCE is their only
            // protection against authorization code interception.
            if(applicationConfig.PKCE || app.ClientType == ClientTypes.Public)
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
            var initialPasswordFile = GetInitialPasswordFilePath(userConfig.Email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = userConfig.Username,
                    Email = userConfig.Email,
                    EmailConfirmed = true
                };

                var generatePassword = string.IsNullOrWhiteSpace(userConfig.Password);
                if (generatePassword)
                    userConfig.Password = PasswordGenerator.QuicklyGeneratePassword(20, 28);

                var createResult = await userManager.CreateAsync(user, userConfig.Password);
                if (!createResult.Succeeded)
                {
                    logger.LogError("Failed to create user {Email}: {Errors}", userConfig.Email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    continue;
                }
                logger.LogInformation("Created user {Email}", userConfig.Email);

                // Never log a generated password: logs are long-lived and widely readable.
                if (generatePassword)
                {
                    WriteInitialPasswordFile(initialPasswordFile, userConfig.Email, userConfig.Password);
                    logger.LogWarning("Generated password for {Email} written to {Path}. Sign in, change the password and delete the file.",
                        userConfig.Email, initialPasswordFile);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(userConfig.Password))
                {
                    var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResult = await userManager.ResetPasswordAsync(user, resetToken, userConfig.Password);
                    if (passwordResult.Succeeded)
                    {
                        logger.LogInformation("Updated password for user {Email}", userConfig.Email);
                    }
                    else
                    {
                        logger.LogError("Failed to update password for user {Email}: {Errors}", userConfig.Email, string.Join(", ", passwordResult.Errors.Select(e => e.Description)));
                    }
                }

                if (File.Exists(initialPasswordFile))
                    logger.LogWarning("Initial password file {Path} still exists. Delete it once the password has been changed.", initialPasswordFile);
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
                        logger.LogWarning("Role {Role} does not exist. Skipping assignment to user {Email}", role, userConfig.Email);
                        continue;
                    }
                    
                    var result = await userManager.AddToRoleAsync(user, role);
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Added user {Email} to role {Role}", userConfig.Email, role);
                    }
                    else
                    {
                        logger.LogError("Failed to add user {Email} to role {Role}: {Errors}", userConfig.Email, role, string.Join(", ", result.Errors.Select(e => e.Description)));
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
                        logger.LogInformation("Removed user {Email} from role {Role}", userConfig.Email, role);
                    }
                    else
                    {
                        logger.LogError("Failed to remove user {Email} from role {Role}: {Errors}", userConfig.Email, role, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
        }
    }


    // OpenIddict:InitialPasswordDirectory, defaulting to the content root. Point it at a mounted
    // volume in containers so the file survives until it is read.
    private string GetInitialPasswordFilePath(string email)
    {
        var directory = configuration["OpenIddict:InitialPasswordDirectory"];
        if (string.IsNullOrWhiteSpace(directory))
            directory = environment.ContentRootPath;

        var safeName = string.Concat(email.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        return Path.GetFullPath(Path.Combine(directory, $"initial-password-{safeName}.txt"));
    }

    private static void WriteInitialPasswordFile(string path, string email, string password)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write };
        if (!OperatingSystem.IsWindows())
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

        using var writer = new StreamWriter(path, options);
        writer.WriteLine($"Initial password for {email}:");
        writer.WriteLine(password);
        writer.WriteLine();
        writer.WriteLine("Sign in, change this password, then delete this file.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
