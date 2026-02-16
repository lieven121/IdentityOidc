using Identity.App.EndPoints.Users;
using Identity.App.Models.Consts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
            }
            );

        group.RegisterUserAdminEndpoints("External/");

        return app;
    }
    
}
