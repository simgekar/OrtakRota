using Microsoft.AspNetCore.Identity;
using OrtakRota.Web.Models;

namespace OrtakRota.Api.Data;

public static class DbSeeder
{
    public const string AdminRole = "Admin";
    public const string UserRole = "User";
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

        if (await userManager.FindByEmailAsync(DemoUserEmail) is null)
        {
            var demo = new AppUser
            {
                UserName = DemoUserEmail,
                Email = DemoUserEmail,
                FullName = "Demo Kullanıcı"
            };
            var result = await userManager.CreateAsync(demo, "Demo123");
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    "Demo kullanıcı oluşturulamadı: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(demo, UserRole);
        }
    }
}
