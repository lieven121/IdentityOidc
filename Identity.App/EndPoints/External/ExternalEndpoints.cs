using Identity.App.EndPoints.Users;
using Identity.App.Models.Consts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

namespace Identity.App.EndPoints.External;

public static class ExternalEndpoints
{
    public static IEndpointRouteBuilder MapExternalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("External")
            .WithTags("External")
            .WithName("External")

            .RequireAuthorization(policy =>
            {
                policy.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("oi_scp", Scopes.Admin);
                // Only machine-to-machine (client credentials) tokens, whose subject is the client
                // itself. Rejects user-delegated tokens that happen to carry the admin scope.
                policy.RequireAssertion(context =>
                {
                    var subject = context.User.GetClaim(OpenIddictConstants.Claims.Subject);
                    var clientId = context.User.GetClaim(OpenIddictConstants.Claims.ClientId);
                    return !string.IsNullOrEmpty(subject) && subject == clientId;
                });
            }
            );

        group.RegisterUserAdminEndpoints("External/");

        return app;
    }
    
}
