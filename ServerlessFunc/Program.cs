using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServerlessFunc.Data;
using ServerlessFunc.Models;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        builder.Services.AddControllersWithViews();
        builder.Services.AddDbContext<CraftMarketContext>(options => options.UseSqlServer(connectionString));
        builder.Services.AddHttpClient();

        var app = builder.Build();

        /*
         * Code Attribution
         * Part 5, work with a database in an ASP.NET Core MVC app
         * Microsoft Learn
         * https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-mvc-app/working-with-sql?view=aspnetcore-8.0&tabs=visual-studio
         */
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            SeedData.Initialize(services);
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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

        /*
         * Code Attribution
         * ASP.NET User Roles - Create and Assign Roles for AUTHORIZATION!
         * tutorialsEU - C#
         * YouTube
         * 23 February 2023
         * https://youtu.be/Y6DCP-yH-9Q?si=J7dGcI-e5d9UiVHB
         */
        using (var scope = app.Services.CreateScope())
        {
            var RoleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var roles = new[] { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await RoleManager.RoleExistsAsync(role))
                    await RoleManager.CreateAsync(new IdentityRole(role));
            }
        }

        using (var scope = app.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            string email = "admin@admin.com";
            string password = "ChangeMe!123";
            if (await userManager.FindByEmailAsync(email) == null)
            {
                var user = new IdentityUser();
                user.UserName = email;
                user.Email = email;
                await userManager.CreateAsync(user, password);
                await userManager.AddToRoleAsync(user, "Admin");
            }
        }

        app.Run();
    }
}