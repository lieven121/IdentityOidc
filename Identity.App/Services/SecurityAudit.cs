using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.App.Services;

public enum SecurityEvent
{
    // Authentication
    LoginSucceeded = 1000,
    LoginFailed = 1001,
    LoginLockedOut = 1002,
    LoginTwoFactorFailed = 1003,
    Logout = 1004,

    // OpenID Connect
    AuthorizationGranted = 1100,
    AuthorizationDenied = 1101,
    TokenIssued = 1102,
    TokenRefreshDenied = 1103,

    // Self-service account changes
    PasswordChanged = 1200,
    ProfileUpdated = 1201,
    TwoFactorEnabled = 1202,
    TwoFactorDisabled = 1203,
    RecoveryCodesRegenerated = 1204,
    TwoFactorVerificationFailed = 1205,
    PasskeyAdded = 1206,
    PasskeyRemoved = 1207,

    // Administrative actions (admin UI and External API)
    UserCreated = 1300,
    UserUpdated = 1301,
    PasswordReset = 1302,
    RoleAdded = 1303,
    RoleRemoved = 1304,
    TwoFactorReset = 1305,
    ApplicationRolesUpdated = 1306,
    ApplicationCreated = 1307,
    ApplicationUpdated = 1308,
    ApplicationDeleted = 1309,
    ApplicationSecretChanged = 1310,
    RoleCreated = 1311,
    RoleRenamed = 1312,
    RoleDeleted = 1313,
}

/// <summary>
/// Structured audit trail for security-relevant events. Never pass passwords, codes or tokens.
/// The actor (user or client) and IP are taken from the current request.
/// </summary>
[RegisterScoped]
public class SecurityAudit(ILogger<SecurityAudit> logger, IHttpContextAccessor httpContextAccessor)
{
    public void Log(
        SecurityEvent securityEvent,
        bool success = true,
        string? userId = null,
        string? clientId = null,
        string? detail = null)
    {
        var context = httpContextAccessor.HttpContext;
        var principal = context?.User;
        var actor = principal?.Identity?.IsAuthenticated == true ? principal.GetClaim(Claims.Subject) : null;
        clientId ??= principal?.GetClaim(Claims.ClientId);
        var remoteIp = context?.Connection.RemoteIpAddress?.ToString();

        logger.Log(
            success ? LogLevel.Information : LogLevel.Warning,
            new EventId((int)securityEvent, securityEvent.ToString()),
            "Security audit: {SecurityEvent} {Outcome} user={UserId} actor={Actor} client={ClientId} ip={RemoteIp} {Detail}",
            securityEvent, success ? "succeeded" : "failed", userId, actor, clientId, remoteIp, detail ?? string.Empty);
    }
}
