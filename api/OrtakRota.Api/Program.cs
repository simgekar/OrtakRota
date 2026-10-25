using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OrtakRota.Api.Data;
using OrtakRota.Web.Data;
using OrtakRota.Web.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "'DefaultConnection' bağlantı dizesi bulunamadı. README.md dosyasındaki User Secrets adımını uygulayın.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// Kullanıcı ve rol yönetimi (JWT girişi 2. haftada eklenecek)
builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "OrtakRota API v1");
        options.DocumentTitle = "OrtakRota API";
    });
}

await DbSeeder.SeedAsync(app.Services);

app.MapControllers();

app.Run();
