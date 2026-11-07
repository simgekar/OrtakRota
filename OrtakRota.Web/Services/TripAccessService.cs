using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OrtakRota.Web.Data;
using OrtakRota.Web.Models;

namespace OrtakRota.Web.Services;

// Bir kullanıcının bir gezide ne yapabileceğini tek yerden hesaplar
public class TripAccessService
{
    private readonly AppDbContext _db;

    public TripAccessService(AppDbContext db)
    {
        _db = db;
    }

    public static bool IsAdmin(ClaimsPrincipal user) => user.IsInRole(DbSeeder.AdminRole);

    public static string GetUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public async Task<TripRole?> GetRoleAsync(int tripId, ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        return await _db.TripMembers
            .Where(m => m.TripId == tripId && m.UserId == userId)
            .Select(m => (TripRole?)m.Role)
            .FirstOrDefaultAsync();
    }

    // Gezinin üyeleri (ve Admin) geziyi ve mekanlarını görebilir
    public async Task<bool> CanViewAsync(int tripId, ClaimsPrincipal user) =>
        IsAdmin(user) || await GetRoleAsync(tripId, user) is not null;

    // Kurucu ve Düzenleyici gezi bilgilerini ve mekanları değiştirebilir
    public async Task<bool> CanEditAsync(int tripId, ClaimsPrincipal user) =>
        IsAdmin(user) || await GetRoleAsync(tripId, user) is TripRole.Owner or TripRole.Editor;

    // Geziyi silme ve üye yönetimi yalnızca Kurucuya (ve Admin'e) açıktır
    public async Task<bool> CanManageAsync(int tripId, ClaimsPrincipal user) =>
        IsAdmin(user) || await GetRoleAsync(tripId, user) == TripRole.Owner;
}
