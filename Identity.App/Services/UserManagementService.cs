using Identity.App.Data;
using Identity.App.EndPoints.External.Models;
using Identity.App.Helpers;
using Identity.App.Models;
using Identity.App.Models.Consts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;

namespace Identity.App.Services;

[RegisterScoped]
public class UserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SecurityAudit _audit;
    private readonly ConfigEntries _configEntries;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        SecurityAudit audit,
        ConfigEntries configEntries,
        IHttpContextAccessor httpContextAccessor,
        ILogger<UserManagementService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _audit = audit;
        _configEntries = configEntries;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Results<Ok<EndPoints.Users.Models.UserDto?>, NotFound>> GetUserByEmail(string email, bool returnNullWhenNotFound = false)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            if (returnNullWhenNotFound)
                return TypedResults.Ok<EndPoints.Users.Models.UserDto?>(null);
            return TypedResults.NotFound();
        }
        return TypedResults.Ok<EndPoints.Users.Models.UserDto?>(new EndPoints.Users.Models.UserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            Username = user.UserName ?? "",
            TwoFactorEnabled = user.TwoFactorEnabled,
        });
    }

    public async Task<Results<Ok<EndPoints.Users.Models.UserDto?>, NotFound>> GetUserById(string id, bool returnNullWhenNotFound = false)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            if (returnNullWhenNotFound)
                return TypedResults.Ok<EndPoints.Users.Models.UserDto?>(null);
            return TypedResults.NotFound();
        }
        return TypedResults.Ok<EndPoints.Users.Models.UserDto?>(new EndPoints.Users.Models.UserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            Username = user.UserName ?? "",
            TwoFactorEnabled = user.TwoFactorEnabled,
        });
    }

    public async Task<Results<Ok<ResultPage<EndPoints.Users.Models.UserDto>>, ForbidHttpResult>> GetUsers(UsersFilter? filter = null)
    {
        var qry = _userManager.Users.AsQueryable();

        if (filter != null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Username))
            {
                qry = qry.Where(q => q.UserName != null && q.UserName.ToLower().Contains(filter.Username.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(filter.Email))
            {
                qry = qry.Where(q => q.Email != null && q.Email.ToLower().Contains(filter.Email.ToLower()));
            }
        }
        else
        {
            qry = qry.OrderByDescending(x => x.Id);
        }

        var count = await qry.CountAsync();

        var pageSize = Math.Clamp(filter?.PageSize ?? 50, 1, 100);
        var page = Math.Max(filter?.Page ?? 0, 0);

        var users = await qry
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(x => new EndPoints.Users.Models.UserDto
            {
                Id = x.Id,
                Email = x.Email ?? "",
                Username = x.UserName ?? "",
                TwoFactorEnabled = x.TwoFactorEnabled,
            })
            .ToListAsync();

        return TypedResults.Ok(new ResultPage<EndPoints.Users.Models.UserDto>
        {
            Items = users,
            TotalCount = count,
        });
    }

    public async Task<Results<Ok<EndPoints.Users.Models.UserDto>, Conflict>> CreateUser(CreateUserDto createUserDto)
    {
        var result = await _userManager.FindByEmailAsync(createUserDto.Email);
        if (result != null)
            return TypedResults.Conflict();

        var user = new ApplicationUser
        {
            Email = createUserDto.Email,
            EmailConfirmed = createUserDto.EmailConfirmed,
            UserName = createUserDto.Username,
        };

        var res = await _userManager.CreateAsync(user);
        if (!res.Succeeded)
        {
            _logger.LogError("Failed to create user {Email}: {Errors}", user.Email, string.Join(", ", res.Errors.Select(e => e.Description)));
            _audit.Log(SecurityEvent.UserCreated, success: false, detail: $"email '{user.Email}'");
            return TypedResults.Conflict();
        }

        _audit.Log(SecurityEvent.UserCreated, userId: user.Id, detail: $"email '{user.Email}'");

        if (!string.IsNullOrWhiteSpace(createUserDto.Password))
        {
            var passwordResult = await _userManager.AddPasswordAsync(user, createUserDto.Password);
            if (!passwordResult.Succeeded)
                _logger.LogWarning("User {UserId} created without password: {Errors}", user.Id, string.Join(", ", passwordResult.Errors.Select(e => e.Description)));
        }

        return TypedResults.Ok(new EndPoints.Users.Models.UserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            Username = user.UserName ?? "",
            TwoFactorEnabled = user.TwoFactorEnabled,
        });
    }

    public async Task<Results<Ok<EndPoints.Users.Models.UserDto>, Conflict>> UpdateUserById(string userId, UpdateUserDto updateUserDto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return await UpdateUser(user, updateUserDto);
    }

    public async Task<Results<Ok<EndPoints.Users.Models.UserDto>, Conflict>> UpdateUserByEmail(string email, UpdateUserDto updateUserDto)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return await UpdateUser(user, updateUserDto);
    }

    private async Task<Results<Ok<EndPoints.Users.Models.UserDto>, Conflict>> UpdateUser(ApplicationUser? user, UpdateUserDto updateUserDto)
    {
        if (user is null)
        {
            return TypedResults.Conflict();
        }

        var oldUserName = user.UserName;
        if (!string.IsNullOrWhiteSpace(updateUserDto.Username))
        {
            user.UserName = updateUserDto.Username;
        }

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            _logger.LogError("Failed to update user {UserId}: {Errors}", user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
            _audit.Log(SecurityEvent.UserUpdated, success: false, userId: user.Id);
            return TypedResults.Conflict();
        }

        _audit.Log(SecurityEvent.UserUpdated, userId: user.Id, detail: $"username '{oldUserName}' -> '{user.UserName}'");

        return TypedResults.Ok(new EndPoints.Users.Models.UserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            Username = user.UserName ?? "",
            TwoFactorEnabled = user.TwoFactorEnabled,
        });
    }

    public async Task<Results<Ok<EndPoints.Users.Models.UserDto>, BadRequest, Conflict>> ResetPasswordById(string userId, ResetPasswordDto resetPasswordDto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return await ResetPassword(user, resetPasswordDto);
    }

    public async Task<Results<Ok<EndPoints.Users.Models.UserDto>, BadRequest, Conflict>> ResetPasswordByEmail(string email, ResetPasswordDto resetPasswordDto)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return await ResetPassword(user, resetPasswordDto);
    }

    private async Task<Results<Ok<EndPoints.Users.Models.UserDto>, BadRequest, Conflict>> ResetPassword(ApplicationUser? user, ResetPasswordDto resetPasswordDto)
    {
        if (user is null)
        {
            return TypedResults.Conflict();
        }
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var password = !string.IsNullOrEmpty(resetPasswordDto?.NewPassword) ? resetPasswordDto.NewPassword : PasswordGenerator.QuicklyGeneratePassword(16, 24);

        var result = await _userManager.ResetPasswordAsync(user, token, password);
        if (!result.Succeeded)
        {
            _audit.Log(SecurityEvent.PasswordReset, success: false, userId: user.Id,
                detail: string.Join(", ", result.Errors.Select(e => e.Code)));
            return TypedResults.BadRequest();
        }

        _audit.Log(SecurityEvent.PasswordReset, userId: user.Id,
            detail: string.IsNullOrEmpty(resetPasswordDto?.NewPassword) ? "random password" : "password supplied");
        return TypedResults.Ok(new EndPoints.Users.Models.UserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            Username = user.UserName ?? "",
            TwoFactorEnabled = user.TwoFactorEnabled,
        });
    }

    public async Task<RoleMembershipResult> AddUserToRoleByIdAsync(string userId, string roleName)
        => await ChangeRoleMembershipAsync(await _userManager.FindByIdAsync(userId), roleName, add: true);

    public async Task<RoleMembershipResult> AddUserToRoleByEmailAsync(string email, string roleName)
        => await ChangeRoleMembershipAsync(await _userManager.FindByEmailAsync(email), roleName, add: true);

    public async Task<RoleMembershipResult> RemoveUserFromRoleByIdAsync(string userId, string roleName)
        => await ChangeRoleMembershipAsync(await _userManager.FindByIdAsync(userId), roleName, add: false);

    public async Task<RoleMembershipResult> RemoveUserFromRoleByEmailAsync(string email, string roleName)
        => await ChangeRoleMembershipAsync(await _userManager.FindByEmailAsync(email), roleName, add: false);

    private async Task<RoleMembershipResult> ChangeRoleMembershipAsync(ApplicationUser? user, string roleName, bool add)
    {
        if (user == null) return RoleMembershipResult.UserNotFound;

        // Roles are managed explicitly; an unknown name is a mistake, not a request to create one.
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role?.Name == null) return RoleMembershipResult.RoleNotFound;

        if (await _userManager.IsInRoleAsync(user, role.Name) == add)
            return RoleMembershipResult.Ok;

        // The startup worker re-syncs configured users' roles, so a change here would be reverted.
        if (_configEntries.IsConfigUser(user.Email))
            return RoleMembershipResult.ConfigManaged;

        if (!add && string.Equals(role.Name, Roles.Admin, StringComparison.OrdinalIgnoreCase) &&
            user.Id == _userManager.GetUserId(_httpContextAccessor.HttpContext?.User ?? new()))
            return RoleMembershipResult.OwnAdminRole;

        var result = add
            ? await _userManager.AddToRoleAsync(user, role.Name)
            : await _userManager.RemoveFromRoleAsync(user, role.Name);
        _audit.Log(add ? SecurityEvent.RoleAdded : SecurityEvent.RoleRemoved, success: result.Succeeded,
            userId: user.Id, detail: $"role '{role.Name}'");
        return result.Succeeded ? RoleMembershipResult.Ok : RoleMembershipResult.Failed;
    }

    public async Task<IList<string>> GetUserRolesByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return new List<string>();
        return await _userManager.GetRolesAsync(user);
    }

    public async Task<IList<string>> GetUserRolesByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return new List<string>();
        return await _userManager.GetRolesAsync(user);
    }

    public async Task<bool> IsUserInRoleByIdAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;
        return await _userManager.IsInRoleAsync(user, roleName);
    }

    public async Task<bool> IsUserInRoleByEmailAsync(string email, string roleName)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return false;
        return await _userManager.IsInRoleAsync(user, roleName);
    }

    // Two-Factor Authentication Methods

    public async Task<EndPoints.Users.Models.TwoFactorStatusDto?> GetTwoFactorStatusAsync(ApplicationUser? user)
    {
        if (user == null) return null;

        var hasAuthenticator = await _userManager.GetAuthenticatorKeyAsync(user) != null;
        var recoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);

        return new EndPoints.Users.Models.TwoFactorStatusDto
        {
            Is2faEnabled = user.TwoFactorEnabled,
            HasAuthenticator = hasAuthenticator,
            RecoveryCodesLeft = recoveryCodesLeft
        };
    }

    public async Task<EndPoints.Users.Models.TwoFactorSetupDto?> LoadSharedKeyAndQrCodeUriAsync(ApplicationUser? user, string appName)
    {
        if (user == null || user.Email == null) return null;

        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        if (unformattedKey == null) return null;

        var sharedKey = FormatKey(unformattedKey);
        var authenticatorUri = GenerateQrCodeUri(user.Email, unformattedKey, appName);

        return new EndPoints.Users.Models.TwoFactorSetupDto
        {
            SharedKey = sharedKey,
            AuthenticatorUri = authenticatorUri
        };
    }

    public async Task<(bool Success, IEnumerable<string>? RecoveryCodes)> EnableTwoFactorAsync(ApplicationUser? user, string verificationCode)
    {
        if (user == null) return (false, null);

        var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

        if (!is2faTokenValid)
        {
            return (false, null);
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        
        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        return (true, recoveryCodes);
    }

    // Accepts a current authenticator code, or an unused recovery code (which is consumed).
    public async Task<bool> VerifyTwoFactorCodeAsync(ApplicationUser? user, string? code)
    {
        if (user == null || string.IsNullOrWhiteSpace(code)) return false;

        var authenticatorCode = code.Replace(" ", string.Empty).Replace("-", string.Empty);
        if (await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, authenticatorCode))
            return true;

        var result = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, code.Trim());
        return result.Succeeded;
    }

    public async Task<bool> DisableTwoFactorAsync(ApplicationUser? user)
    {
        if (user == null) return false;

        var result = await _userManager.SetTwoFactorEnabledAsync(user, false);
        return result.Succeeded;
    }

    public async Task<IEnumerable<string>?> GenerateNewRecoveryCodesAsync(ApplicationUser? user)
    {
        if (user == null) return null;

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        return recoveryCodes;
    }

    public async Task<bool> ResetTwoFactorByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return await ResetTwoFactorAsync(user);
    }

    public async Task<bool> ResetTwoFactorByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return await ResetTwoFactorAsync(user);
    }

    private async Task<bool> ResetTwoFactorAsync(ApplicationUser? user)
    {
        if (user == null) return false;

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);

        // This is the lost-device path, so passkeys go too.
        var passkeys = await _userManager.GetPasskeysAsync(user);
        foreach (var passkey in passkeys)
            await _userManager.RemovePasskeyAsync(user, passkey.CredentialId);

        _audit.Log(SecurityEvent.TwoFactorReset, userId: user.Id,
            detail: passkeys.Count > 0 ? $"removed {passkeys.Count} passkey(s)" : null);
        return true;
    }

    private static string FormatKey(string unformattedKey)
    {
        var result = new System.Text.StringBuilder();
        int currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }
        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition));
        }

        return result.ToString().ToLowerInvariant();
    }

    private static string GenerateQrCodeUri(string email, string unformattedKey, string appName)
    {
        return string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6",
            UrlEncoder.Default.Encode(appName),
            UrlEncoder.Default.Encode(email),
            unformattedKey);
    }
}

public enum RoleMembershipResult
{
    Ok,
    UserNotFound,
    RoleNotFound,
    ConfigManaged,
    OwnAdminRole,
    Failed,
}
