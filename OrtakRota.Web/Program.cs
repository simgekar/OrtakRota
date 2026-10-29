using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OrtakRota.Web.Data;
using OrtakRota.Web.Models;
using OrtakRota.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "'DefaultConnection' bağlantı dizesi bulunamadı. README.md dosyasındaki User Secrets adımını uygulayın.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services
    .AddIdentity<AppUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<TurkishIdentityErrorDescriber>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

await DbSeeder.SeedAsync(app.Services);

if (!app.Environment.IsDevelopment())
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

app.Run();
