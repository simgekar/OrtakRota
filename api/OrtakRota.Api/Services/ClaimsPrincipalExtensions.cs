using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using OrtakRota.Api.Data;

namespace OrtakRota.Api.Services;

public static class ClaimsPrincipalExtensions
{
    // Token'daki "sub" bilgisi giriş yapan kullanıcının kimliğidir
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? throw new InvalidOperationException("Token'da kullanıcı kimliği yok.");

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(DbSeeder.AdminRole);
}
