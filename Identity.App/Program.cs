using Identity.App.Data;
using Identity.App.EndPoints;
using Identity.App.EndPoints.Identity;
using Identity.App.EndPoints.OpenIdConnect;
using Identity.App.EndPoints.Users;
using Identity.App.Hosting;
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
    .AutoRegisterFromIdentityApp();

builder
    .ConfigureIdentity()
    .ConfigureOpenIddict()
    .ConfigureCors()
    .ConfigureSwagger();

builder.Services
    .AddAntiforgery();

builder.Services
    .AddDatabaseDeveloperPageExceptionFilter();

//builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();
app.UseSwagger();
app.UseUrlsFromConfig();

app.UseCertificateForwarding();
//app.UseHttpsRedirection();

app.UseOpenIddict();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

var apiEnpoints = app
    .MapGroup("api");


apiEnpoints.MapStatusEndpoints();
apiEnpoints.MapUsersEndpoints();

apiEnpoints.MapIdentityEndpoints();

app.MapOpenIdConnectEndpoints();

app.UseVueFallbackSpa();


await app.StartAsync();

await app.GenerateClients();

await app.WaitForShutdownAsync();
