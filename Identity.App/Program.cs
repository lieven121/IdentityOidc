using Identity.App.Data;
using Identity.App.EndPoints;
using Identity.App.EndPoints.Admin;
using Identity.App.EndPoints.External;
using Identity.App.EndPoints.Identity;
using Identity.App.EndPoints.OpenIdConnect;
using Identity.App.EndPoints.Users;
using Identity.App.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    //options.UseSqlServer(connectionString);
    options.UseSqlite(connectionString);
});

builder.Services
    .AutoRegisterFromIdentityApp()
    .AddHttpContextAccessor();

builder
    .ConfigureIdentity()
    .ConfigureOpenIddict()
    .ConfigureCors()
    .ConfigureSwagger()
    .ConfigureRateLimiting()
    .ConfigureReverseProxySupport();


builder.Services
    .AddAntiforgery();

builder.Services
    .AddDatabaseDeveloperPageExceptionFilter();



//builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();
app.UseSecurityHeaders();
app.UseSwagger();
app.UseUrlsFromConfig();


app.UseReverseProxySupport();
app.UseOpenIddict();
app.UseRateLimiter();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

var apiEnpoints = app
    .MapGroup("api")
    .RequireCors(CorsConfig.ApiPolicy);


apiEnpoints.MapStatusEndpoints();
apiEnpoints.MapUsersEndpoints();
apiEnpoints.MapAdminEndpoints();
apiEnpoints.MapExternalEndpoints();

apiEnpoints.MapIdentityEndpoints();

app.MapOpenIdConnectEndpoints();

app.UseVueFallbackSpa();


await app.StartAsync();

await app.GenerateClients();

await app.WaitForShutdownAsync();
