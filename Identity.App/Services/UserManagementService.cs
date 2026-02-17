using Identity.App.Data;
using Identity.App.EndPoints.External.Models;
using Identity.App.Helpers;
using Identity.App.Models;
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

    public UserManagementService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
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

        var users = await qry
            .Skip(filter?.Page ?? 0 * (filter?.PageSize ?? 50))
            .Take(filter?.PageSize ?? 50)
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
            Console.Error.WriteLine($"Failed to create user {user.Id}: {string.Join(", ", res.Errors.Select(e => e.Description))}");
            return TypedResults.Conflict();
        }

        if (!string.IsNullOrWhiteSpace(createUserDto.Password))
            await _userManager.AddPasswordAsync(user, createUserDto.Password);

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

        if (!string.IsNullOrWhiteSpace(updateUserDto.Username))
        {
            user.UserName = updateUserDto.Username;
        }

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            Console.Error.WriteLine($"Failed to update user {user.Id}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            return TypedResults.Conflict();
        }

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
            return TypedResults.BadRequest();
        }
        return TypedResults.Ok(new EndPoints.Users.Models.UserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            Username = user.UserName ?? "",
            TwoFactorEnabled = user.TwoFactorEnabled,
        });
    }

    public async Task<bool> AddUserToRoleByIdAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        await EnsureRoleExistsAsync(roleName);
        
        if (!await _userManager.IsInRoleAsync(user, roleName))
        {
            var result = await _userManager.AddToRoleAsync(user, roleName);
            return result.Succeeded;
        }
        return true;
    }

    public async Task<bool> AddUserToRoleByEmailAsync(string email, string roleName)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return false;

        await EnsureRoleExistsAsync(roleName);
        
        if (!await _userManager.IsInRoleAsync(user, roleName))
        {
            var result = await _userManager.AddToRoleAsync(user, roleName);
            return result.Succeeded;
        }
        return true;
    }

    public async Task<bool> RemoveUserFromRoleByIdAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        if (await _userManager.IsInRoleAsync(user, roleName))
        {
            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            return result.Succeeded;
        }
        return true;
    }

    public async Task<bool> RemoveUserFromRoleByEmailAsync(string email, string roleName)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return false;

        if (await _userManager.IsInRoleAsync(user, roleName))
        {
            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            return result.Succeeded;
        }
        return true;
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

    private async Task<bool> EnsureRoleExistsAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
            return result.Succeeded;
        }
        return true;
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
