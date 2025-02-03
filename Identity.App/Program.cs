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

app.UseHttpsRedirection();

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
//using (var scope = app.Services.CreateScope())
//{
//    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
//    var userStore = scope.ServiceProvider.GetRequiredService<IUserStore<ApplicationUser>>();
//    var userEmailStore = userStore as IUserEmailStore<ApplicationUser>;

//    //await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();

//    if (await userManager.FindByEmailAsync("admin@localhost") == null)
//    {
//        var res = await userManager.CreateAsync(new ApplicationUser
//        {
//            UserName = "admin",
//            Email = "admin@localhost",
//            EmailConfirmed = true
//        }, "Azerty123$");
//    }
//}

await app.WaitForShutdownAsync();
