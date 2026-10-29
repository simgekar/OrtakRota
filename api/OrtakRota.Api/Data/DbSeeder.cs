using Microsoft.AspNetCore.Identity;
using OrtakRota.Web.Models;

namespace OrtakRota.Api.Data;

public static class DbSeeder
{
    public const string AdminRole = "Admin";
    public const string UserRole = "User";

    public const string AdminEmail = "admin@ortakrota.com";
    public const string DemoUserEmail = "demo@ortakrota.com";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        foreach (var role in new[] { AdminRole, UserRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        await EnsureUserAsync(userManager, AdminEmail, "Yönetici", "Admin123", AdminRole);
        await EnsureUserAsync(userManager, DemoUserEmail, "Demo Kullanıcı", "Demo123", UserRole);
    }

    private static async Task EnsureUserAsync(UserManager<AppUser> userManager,
        string email, string fullName, string password, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser { UserName = email, Email = email, FullName = fullName };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"{email} oluşturulamadı: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);
    }
}
