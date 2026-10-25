using Identity.App.Data;
using Identity.App.Extensions;
using Identity.App.Hosting;
using Identity.App.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Net.Http;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.App.EndPoints.OpenIdConnect;

public static class OpenIdConnectEndpoints
{
    public static IEndpointRouteBuilder MapOpenIdConnectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("connect")
            .WithTags("OpenIdConnect")
            .WithName("OpenIdConnect");



        group.MapGet("/authorize", AuthorizeHandler)
            .WithName("Authorize")
            .AllowAnonymous()
            .DisableAntiforgery();

        group.MapPost("/token", (Delegate)TokenHandler)
            .WithName("Token")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingConfig.TokenPolicy)
            .DisableAntiforgery();

        group.MapGet("/userinfo", UserInfoHandler)
            .WithName("UserInfo")
            .Produces(StatusCodes.Status200OK, typeof(IEnumerable<Claim>), "application/json")
            .RequireAuthorization(policy =>
                {
                    policy.AddAuthenticationSchemes(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                    policy.RequireAuthenticatedUser();
                }
            );

        group.MapGet("/endsession", EndSessionHandler)
            .WithName("EndSession");



        return app;
    }

    #region Handlers


    private static async Task<IResult> AuthorizeHandler(HttpContext httpContext,
           SignInManager<ApplicationUser> signInManager,
           UserManager<ApplicationUser> userManager,
           IOpenIddictScopeManager scopeManager,
           IOpenIddictApplicationManager applicationManager,
           TwoFactorPolicy twoFactorPolicy,
           SecurityAudit audit
           )
    {
        var user = httpContext.User;
        var request = httpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // Only an authenticated cookie session may obtain an authorization code here. Do not
        // authenticate against the OpenIddict scheme: at this endpoint that returns the principal
        // from a caller-supplied id_token_hint, which would issue codes without a login.
        if (user.Identity?.IsAuthenticated != true)
            return Results.Challenge();

        var appUser = await userManager.GetUserAsync(user);
        if (appUser is null)
            return Results.Challenge();

        // Users who must use 2FA but haven't set it up are sent to the security page; it returns
        // to this authorize request once TOTP or a passkey is in place.
        if (await twoFactorPolicy.NeedsSetupAsync(appUser))
        {
            audit.Log(SecurityEvent.AuthorizationDenied, success: false, userId: appUser.Id, clientId: request.ClientId,
                detail: "two-factor setup required");
            var returnUrl = httpContext.Request.PathBase + httpContext.Request.Path + httpContext.Request.QueryString;
            return Results.Redirect($"{httpContext.Request.PathBase}/account/security?setup=required&ReturnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        var missingRolesError = await GetMissingRequiredRolesErrorAsync(applicationManager, userManager, request.ClientId, appUser);
        if (missingRolesError != null)
        {
            audit.Log(SecurityEvent.AuthorizationDenied, success: false, userId: appUser.Id, clientId: request.ClientId,
                detail: missingRolesError);
            return ForbidWithError(Errors.AccessDenied, missingRolesError);
        }

        var scopes = request.GetScopes();
        var principal = await CreatePrincipalAsync(
            signInManager, appUser, scopes, await scopeManager.ListResourcesAsync(scopes).ToListAsync());

        audit.Log(SecurityEvent.AuthorizationGranted, userId: appUser.Id, clientId: request.ClientId,
            detail: $"scopes '{string.Join(' ', scopes)}'");

        return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static async Task<IResult> TokenHandler(
        HttpContext httpContext,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> identityOptions,
        TwoFactorPolicy twoFactorPolicy,
        SecurityAudit audit)
    {
        var request = httpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");
        var result = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var principal = result.Principal;



        if (request.IsClientCredentialsGrantType())
        {
            var application = await applicationManager.FindByClientIdAsync(request.ClientId)?? throw new InvalidOperationException("Missing client_id");

            // Create a principal for the client
            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: Claims.Name,
                roleType: Claims.Role);

            // Add the client_id claim
            identity.SetClaim(Claims.Subject, request.ClientId);
            identity.SetClaim(Claims.ClientId, request.ClientId);
            identity.SetClaim(Claims.Name, await applicationManager.GetDisplayNameAsync(application));

            // Optionally add roles, scopes, or custom claims for this client
            // e.g. identity.SetClaim("role", "api_service");

            //set lifetime
            //identity.SetAccessTokenLifetime(TimeSpan.FromHours(1));

            // Mark which scopes were granted (important for resource APIs)
            identity.SetScopes(request.GetScopes());
            identity.SetResources(await scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

            // Store destinations so claims end up in tokens
            identity.SetDestinations(static claim => claim.Type switch
            {
                Claims.Name or Claims.Subject or Claims.ClientId => new[] { Destinations.AccessToken },
                _ => Array.Empty<string>()
            });

            principal = new ClaimsPrincipal(identity);

            audit.Log(SecurityEvent.TokenIssued, clientId: request.ClientId,
                detail: $"client_credentials, scopes '{string.Join(' ', identity.GetScopes())}'");
        }
        else if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            var grantType = request.GrantType;

            // Re-validate the user on every code redemption and refresh, so deleted, locked out,
            // re-credentialed or de-roled users stop receiving tokens.
            var userId = principal?.GetClaim(Claims.Subject);
            var appUser = string.IsNullOrEmpty(userId) ? null : await userManager.FindByIdAsync(userId);
            if (principal is null || appUser is null ||
                !await signInManager.CanSignInAsync(appUser) ||
                await userManager.IsLockedOutAsync(appUser))
            {
                audit.Log(SecurityEvent.TokenRefreshDenied, success: false, userId: userId, clientId: request.ClientId,
                    detail: $"{grantType}: user missing, not allowed to sign in or locked out");
                return ForbidWithError(Errors.InvalidGrant, "The token is no longer valid.");
            }

            // The security stamp changes on password reset, 2FA reset, etc.
            var tokenStamp = principal.GetClaim(identityOptions.Value.ClaimsIdentity.SecurityStampClaimType);
            if (tokenStamp != null && userManager.SupportsUserSecurityStamp &&
                tokenStamp != await userManager.GetSecurityStampAsync(appUser))
            {
                audit.Log(SecurityEvent.TokenRefreshDenied, success: false, userId: userId, clientId: request.ClientId,
                    detail: $"{grantType}: security stamp changed");
                return ForbidWithError(Errors.InvalidGrant, "The token is no longer valid.");
            }

            var missingRolesError = await GetMissingRequiredRolesErrorAsync(applicationManager, userManager, request.ClientId, appUser);
            if (missingRolesError != null)
            {
                audit.Log(SecurityEvent.TokenRefreshDenied, success: false, userId: userId, clientId: request.ClientId,
                    detail: $"{grantType}: {missingRolesError}");
                return ForbidWithError(Errors.InvalidGrant, missingRolesError);
            }

            // Covers enforcement switched on after tokens were issued: the client has to go back
            // through authorize, which sends the user to set up 2FA.
            if (await twoFactorPolicy.NeedsSetupAsync(appUser))
            {
                audit.Log(SecurityEvent.TokenRefreshDenied, success: false, userId: userId, clientId: request.ClientId,
                    detail: $"{grantType}: two-factor setup required");
                return ForbidWithError(Errors.InvalidGrant, "Two-factor authentication must be set up before continuing.");
            }

            // Rebuild from the current user so role/claim changes are reflected.
            principal = await CreatePrincipalAsync(signInManager, appUser, principal.GetScopes(), principal.GetResources());

            // Refreshes are routine and frequent; only the initial code redemption is audited.
            if (request.IsAuthorizationCodeGrantType())
                audit.Log(SecurityEvent.TokenIssued, userId: userId, clientId: request.ClientId, detail: grantType);
        }
        else
        {
            principal = null;
        }

        if (principal != null)
            return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        return Results.Forbid(authenticationSchemes: new List<string>
            {
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
            });

    }

    private static async Task<IResult> UserInfoHandler(HttpContext httpContext, UserManager<ApplicationUser> userManager)
    {
        //https://github.com/openiddict/openiddict-samples/blob/dev/samples/Dantooine/Dantooine.Server/Controllers/UserinfoController.cs
        var user = httpContext.User;

        var request = httpContext.GetOpenIddictServerRequest();

        var result = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var principal = result.Principal;

        if (principal == null)
            return Results.Unauthorized();

        var applicationUser = await userManager.GetUserAsync(principal);
        if (applicationUser is null)
            return Results.Unauthorized();

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            // Note: the "sub" claim is a mandatory claim and must be included in the JSON response.
            [Claims.Subject] = await userManager.GetUserIdAsync(applicationUser)
        };

        if (user.HasScope(Scopes.Email))
        {
            claims[Claims.Email] = await userManager.GetEmailAsync(applicationUser);
            claims[Claims.EmailVerified] = await userManager.IsEmailConfirmedAsync(applicationUser);
        }

        if (user.HasScope(Scopes.Phone))
        {
            claims[Claims.PhoneNumber] = await userManager.GetPhoneNumberAsync(applicationUser);
            claims[Claims.PhoneNumberVerified] = await userManager.IsPhoneNumberConfirmedAsync(applicationUser);
        }

        if (user.HasScope(Scopes.Roles))
        {
            claims[Claims.Role] = await userManager.GetRolesAsync(applicationUser);
        }

        return Results.Ok(claims);
    }

    private static async Task<IResult> EndSessionHandler(HttpContext context, SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.SignOut(authenticationSchemes: new List<string>
            {
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
            });
    }

    private static async Task<ClaimsPrincipal> CreatePrincipalAsync(
        SignInManager<ApplicationUser> signInManager,
        ApplicationUser appUser,
        IEnumerable<string> scopes,
        IEnumerable<string> resources)
    {
        var principal = await signInManager.CreateUserPrincipalAsync(appUser);

        var emailClaim = principal.Claims.FirstOrDefault(x => x.Type.Equals(ClaimTypes.Email));
        if (emailClaim != null)
        {
            var existing = principal.Claims.FirstOrDefault(c => c.Type == Claims.Email);
            if (existing != null)
                principal.SetClaim(Claims.Email, emailClaim.Value);
            else
                principal.AddClaim(Claims.Email, emailClaim.Value);
        }

        principal.SetScopes(scopes);
        principal.SetResources(resources);

        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, principal));
        }

        return principal;
    }

    // Returns an error description when the application requires roles the user doesn't have.
    private static async Task<string?> GetMissingRequiredRolesErrorAsync(
        IOpenIddictApplicationManager applicationManager,
        UserManager<ApplicationUser> userManager,
        string? clientId,
        ApplicationUser appUser)
    {
        if (string.IsNullOrEmpty(clientId))
            return null;

        var application = await applicationManager.FindByClientIdAsync(clientId);
        if (application == null)
            return null;

        var properties = await applicationManager.GetPropertiesAsync(application);
        if (!properties.TryGetValue("RequiredRoles", out var rolesElement) ||
            rolesElement.ValueKind != System.Text.Json.JsonValueKind.Array)
            return null;

        var requiredRoles = rolesElement.EnumerateArray()
            .Where(e => e.ValueKind == System.Text.Json.JsonValueKind.String)
            .Select(e => e.GetString() ?? "")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (!requiredRoles.Any())
            return null;

        var userRoles = await userManager.GetRolesAsync(appUser);
        var hasRequiredRole = requiredRoles.Any(requiredRole =>
            userRoles.Contains(requiredRole, StringComparer.OrdinalIgnoreCase));

        return hasRequiredRole ? null : $"User does not have required role(s): {string.Join(", ", requiredRoles)}";
    }

    private static IResult ForbidWithError(string error, string description) =>
        Results.Forbid(
            authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            }));

    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        // Note: by default, claims are NOT automatically included in the access and identity tokens.
        // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
        // whether they should be included in access tokens, in identity tokens or in both.

        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Subject:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;
            case OpenIddictConstants.Claims.Name:
                yield return OpenIddictConstants.Destinations.AccessToken;
                // TODO check
                //if (principal.HasScope(OpenIddictConstants.Permissions.Scopes.Profile))
                yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;
            case OpenIddictConstants.Claims.GivenName:
                yield return OpenIddictConstants.Destinations.AccessToken;
                if (principal.HasScope(OpenIddictConstants.Permissions.Scopes.Profile))
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;
            case OpenIddictConstants.Claims.FamilyName:
                yield return OpenIddictConstants.Destinations.AccessToken;
                if (principal.HasScope(OpenIddictConstants.Permissions.Scopes.Profile))
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;
            case OpenIddictConstants.Claims.Email:
                yield return OpenIddictConstants.Destinations.AccessToken;
                // TODO check
                //if (principal.HasScope(OpenIddictConstants.Permissions.Scopes.Email))
                yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            case OpenIddictConstants.Claims.Role:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (principal.HasScope(OpenIddictConstants.Permissions.Scopes.Roles))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            // Never include the security stamp in the access and identity tokens, as it's a secret value.
            case "AspNet.Identity.SecurityStamp": yield break;

            default:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;
        }
    }



    #endregion

}
