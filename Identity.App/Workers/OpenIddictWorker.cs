

using Identity.App.Data;
using Identity.App.Helpers;
using Identity.App.Models;
using Identity.App.Models.Consts;
using Identity.App.Services;
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
        var roles = scope.ServiceProvider.GetRequiredService<ConfigEntries>().RoleNames;
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

            var app = ApplicationDescriptorBuilder.Build(applicationConfig);

            foreach (var appScope in ApplicationDescriptorBuilder.GetScopes(applicationConfig))
            {
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

            await manager.CreateAsync(app, cancellationToken);
        }


        // Merge rather than recreate: applications managed in the admin UI may also be resources
        // of these scopes, and must keep them across restarts.
        var configClientIds = new HashSet<string>(applications.Select(a => a.ClientId), StringComparer.OrdinalIgnoreCase);
        foreach (var appScope in scopeDictionary)
        {
            var s = await scopeManager.FindByNameAsync(appScope.Key, cancellationToken);
            if (s == null)
            {
                await scopeManager.CreateAsync(appScope.Value, cancellationToken);
                continue;
            }

            var existing = new OpenIddictScopeDescriptor();
            await scopeManager.PopulateAsync(existing, s, cancellationToken);
            foreach (var resource in existing.Resources.Where(r => !configClientIds.Contains(r)))
                appScope.Value.Resources.Add(resource);
            await scopeManager.UpdateAsync(s, appScope.Value, cancellationToken);
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
