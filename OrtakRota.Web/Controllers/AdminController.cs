using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrtakRota.Web.Data;
using OrtakRota.Web.Models;
using OrtakRota.Web.ViewModels;

namespace OrtakRota.Web.Controllers;

// Yönetici paneli: yalnızca Admin rolündeki kullanıcılar erişebilir
[Authorize(Roles = DbSeeder.AdminRole)]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public AdminController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // GET: /Admin
    public async Task<IActionResult> Index()
    {
        var adminIds = await GetAdminIdsAsync();
        var now = DateTimeOffset.UtcNow;

        var model = new AdminDashboardViewModel
        {
            UserCount = await _db.Users.CountAsync(),
            AdminCount = adminIds.Count,
            LockedCount = await _db.Users.CountAsync(u => u.LockoutEnd != null && u.LockoutEnd > now),
            TripCount = await _db.Trips.CountAsync(),
            PlaceCount = await _db.Places.CountAsync(),
            VoteCount = await _db.Votes.CountAsync(),
            RecentUsers = await QueryUsers(now).OrderByDescending(u => u.CreatedAt).Take(5).ToListAsync(),
            RecentTrips = await QueryTrips().OrderByDescending(t => t.CreatedAt).Take(5).ToListAsync()
        };

        foreach (var user in model.RecentUsers)
            user.IsAdmin = adminIds.Contains(user.Id);

        return View(model);
    }

    // GET: /Admin/Users?q=ali
    public async Task<IActionResult> Users(string? q)
    {
        var adminIds = await GetAdminIdsAsync();
        var query = QueryUsers(DateTimeOffset.UtcNow);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
        }

        var users = await query.OrderBy(u => u.FullName).ToListAsync();
        foreach (var user in users)
            user.IsAdmin = adminIds.Contains(user.Id);

        ViewData["Query"] = q;
        return View(users);
    }

    // POST: /Admin/ToggleAdmin — Admin rolünü verir ya da geri alır
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdmin(string id)
    {
        if (id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "Kendi yönetici yetkini kaldıramazsın.";
            return RedirectToAction(nameof(Users));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        if (await _userManager.IsInRoleAsync(user, DbSeeder.AdminRole))
        {
            await _userManager.RemoveFromRoleAsync(user, DbSeeder.AdminRole);
            TempData["Message"] = $"{user.FullName} artık yönetici değil.";
        }
        else
        {
            await _userManager.AddToRoleAsync(user, DbSeeder.AdminRole);
            TempData["Message"] = $"{user.FullName} yönetici yapıldı.";
        }

        // Rol değişikliğinin kullanıcının açık oturumuna da yansıması için
        await _userManager.UpdateSecurityStampAsync(user);
        return RedirectToAction(nameof(Users));
    }

    // POST: /Admin/ToggleLock — hesabı kilitler ya da kilidini açar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(string id)
    {
        if (id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "Kendi hesabını kilitleyemezsin.";
            return RedirectToAction(nameof(Users));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        if (await _userManager.IsLockedOutAsync(user))
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
            TempData["Message"] = $"{user.FullName} hesabının kilidi açıldı.";
        }
        else
        {
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            // Kilitlenen kullanıcının açık oturumu da geçersiz olsun
            await _userManager.UpdateSecurityStampAsync(user);
            TempData["Message"] = $"{user.FullName} hesabı kilitlendi; artık giriş yapamaz.";
        }

        return RedirectToAction(nameof(Users));
    }

    // GET: /Admin/Trips?q=istanbul
    public async Task<IActionResult> Trips(string? q)
    {
        var query = QueryTrips();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(term) || t.OwnerName.ToLower().Contains(term));
        }

        ViewData["Query"] = q;
        return View(await query.OrderByDescending(t => t.CreatedAt).ToListAsync());
    }

    // GET: /Admin/Places?tripId=3&q=müze
    public async Task<IActionResult> Places(int? tripId, string? q)
    {
        var query = _db.Places.AsNoTracking();
        if (tripId.HasValue)
            query = query.Where(p => p.TripId == tripId.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term));
        }

        var model = new AdminPlacesViewModel
        {
            TripId = tripId,
            Query = q,
            Places = await query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new AdminPlaceRow
                {
                    Id = p.Id,
                    TripId = p.TripId,
                    TripName = p.Trip.Name,
                    Name = p.Name,
                    Category = p.Category,
                    AddedByName = p.AddedBy.FullName,
                    Score = p.Votes.Sum(v => v.Value),
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync(),
            Trips = await _db.Trips
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .Select(t => new AdminTripOption { Id = t.Id, Name = t.Name })
                .ToListAsync()
        };

        return View(model);
    }

    // POST: /Admin/DeletePlace — uygunsuz ya da hatalı mekanı kaldırır
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePlace(int id, int? tripId)
    {
        var place = await _db.Places.FirstOrDefaultAsync(p => p.Id == id);
        if (place is null)
            return NotFound();

        _db.Places.Remove(place);
        await _db.SaveChangesAsync();

        TempData["Message"] = $"\"{place.Name}\" mekanı silindi.";
        return RedirectToAction(nameof(Places), new { tripId });
    }

    private async Task<HashSet<string>> GetAdminIdsAsync() =>
        (await _userManager.GetUsersInRoleAsync(DbSeeder.AdminRole)).Select(u => u.Id).ToHashSet();

    private IQueryable<AdminUserRow> QueryUsers(DateTimeOffset now) =>
        _db.Users
            .AsNoTracking()
            .Select(u => new AdminUserRow
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                IsLocked = u.LockoutEnd != null && u.LockoutEnd > now,
                TripCount = u.TripMemberships.Count,
                CreatedAt = u.CreatedAt
            });

    private IQueryable<AdminTripRow> QueryTrips() =>
        _db.Trips
            .AsNoTracking()
            .Select(t => new AdminTripRow
            {
                Id = t.Id,
                Name = t.Name,
                OwnerName = t.Owner.FullName,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                MemberCount = t.Members.Count,
                PlaceCount = t.Places.Count,
                CreatedAt = t.CreatedAt
            });
}
