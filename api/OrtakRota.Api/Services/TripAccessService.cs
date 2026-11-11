using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OrtakRota.Web.Data;
using OrtakRota.Web.Models;

namespace OrtakRota.Api.Services;

/// Kullanıcının bir gezideki yetkisini bulur. Admin her gezide Kurucu sayılır.
public class TripAccessService
{
    private readonly AppDbContext _db;

    public TripAccessService(AppDbContext db)
    {
        _db = db;
    }

    /// Kullanıcının gezideki rolü; üye değilse null.
    public async Task<TripRole?> GetRoleAsync(int tripId, ClaimsPrincipal user)
    {
        if (user.IsAdmin())
            return TripRole.Owner;

        var userId = user.GetUserId();
        return await _db.TripMembers
            .Where(m => m.TripId == tripId && m.UserId == userId)
            .Select(m => (TripRole?)m.Role)
            .FirstOrDefaultAsync();
    }
}
