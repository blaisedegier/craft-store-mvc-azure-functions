using CraftMarket.Data;
using CraftMarket.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<CraftMarketContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<CraftMarketContext>();
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.AddScoped<OrderEmailSender>();

var app = builder.Build();

// Adapted from https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-mvc-app/working-with-sql?view=aspnetcore-8.0&tabs=visual-studio
using (var scope = app.Services.CreateScope())
{
    SeedData.Initialize(scope.ServiceProvider);
    await SeedRolesAndAdminAsync(scope.ServiceProvider, app.Configuration, app.Logger);
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

// Creates the Admin and User roles, and an admin account when SeedAdmin:Password is configured.
// Adapted from https://youtu.be/Y6DCP-yH-9Q?si=J7dGcI-e5d9UiVHB
static async Task SeedRolesAndAdminAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Admin", "User" })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var email = configuration["SeedAdmin:Email"];
    var password = configuration["SeedAdmin:Password"];
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        logger.LogWarning("SeedAdmin:Email or SeedAdmin:Password is not set, so no admin account was created.");
        return;
    }

    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    if (await userManager.FindByEmailAsync(email) != null)
    {
        return;
    }

    var admin = new IdentityUser { UserName = email, Email = email };
    var result = await userManager.CreateAsync(admin, password);
    if (!result.Succeeded)
    {
        logger.LogError("Could not create the admin account: {Errors}",
            string.Join(" ", result.Errors.Select(e => e.Description)));
        return;
    }
    await userManager.AddToRoleAsync(admin, "Admin");
}
