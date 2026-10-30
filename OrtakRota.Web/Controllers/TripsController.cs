using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrtakRota.Web.Data;
using OrtakRota.Web.Models;
using OrtakRota.Web.Services;
using OrtakRota.Web.ViewModels;

namespace OrtakRota.Web.Controllers;

[Authorize]
public class TripsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public TripsController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private string CurrentUserId => _userManager.GetUserId(User)!;

    private bool IsAdmin => User.IsInRole(DbSeeder.AdminRole);

    // GET: /Trips
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId;
        var isAdmin = IsAdmin;

        // Admin tüm gezileri, diğer kullanıcılar yalnızca üyesi oldukları gezileri görür
        var trips = await _db.Trips
            .AsNoTracking()
            .Where(t => isAdmin || t.Members.Any(m => m.UserId == userId))
            .OrderBy(t => t.StartDate)
            .Select(t => new TripListItemViewModel
            {
                Id = t.Id,
                Name = t.Name,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                OwnerName = t.Owner.FullName,
                MemberCount = t.Members.Count,
                MyRole = t.Members
                    .Where(m => m.UserId == userId)
                    .Select(m => (TripRole?)m.Role)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return View(trips);
    }

    // GET: /Trips/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var userId = CurrentUserId;

        var trip = await _db.Trips
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TripDetailsViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                InviteCode = t.InviteCode,
                OwnerName = t.Owner.FullName,
                MyRole = t.Members
                    .Where(m => m.UserId == userId)
                    .Select(m => (TripRole?)m.Role)
                    .FirstOrDefault(),
                Members = t.Members
                    .OrderByDescending(m => m.Role)
                    .ThenBy(m => m.JoinedAt)
                    .Select(m => new TripMemberViewModel
                    {
                        UserId = m.UserId,
                        FullName = m.User.FullName,
                        Email = m.User.Email ?? string.Empty,
                        Role = m.Role,
                        JoinedAt = m.JoinedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (trip is null)
            return NotFound();

        // Üye olmayan kullanıcı geziyi göremez (Admin hariç)
        if (trip.MyRole is null && !IsAdmin)
            return Forbid();

        trip.CanEdit = IsAdmin || trip.MyRole is TripRole.Owner or TripRole.Editor;
        trip.CanManage = IsAdmin || trip.MyRole == TripRole.Owner;

        return View(trip);
    }

    // GET: /Trips/Create
    public IActionResult Create()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return View(new TripFormViewModel
        {
            StartDate = today,
            EndDate = today.AddDays(3)
        });
    }

    // POST: /Trips/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TripFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = CurrentUserId;
        var trip = new Trip
        {
            Name = model.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            StartDate = model.StartDate!.Value,
            EndDate = model.EndDate!.Value,
            InviteCode = await InviteCodeGenerator.GenerateUniqueAsync(_db),
            OwnerId = userId
        };

        // Geziyi oluşturan kişi otomatik olarak Kurucu (Owner) rolüyle üye olur
        trip.Members.Add(new TripMember { UserId = userId, Role = TripRole.Owner });

        _db.Trips.Add(trip);
        await _db.SaveChangesAsync();

        TempData["Message"] = $"\"{trip.Name}\" gezisi oluşturuldu. Davet kodu: {trip.InviteCode}";
        return RedirectToAction(nameof(Details), new { id = trip.Id });
    }

    // GET: /Trips/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var trip = await _db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (trip is null)
            return NotFound();
        if (!await CanEditAsync(id))
            return Forbid();

        return View(new TripFormViewModel
        {
            Id = trip.Id,
            Name = trip.Name,
            Description = trip.Description,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate
        });
    }

    // POST: /Trips/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TripFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        var trip = await _db.Trips.FirstOrDefaultAsync(t => t.Id == id);
        if (trip is null)
            return NotFound();
        if (!await CanEditAsync(id))
            return Forbid();

        if (!ModelState.IsValid)
            return View(model);

        trip.Name = model.Name.Trim();
        trip.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        trip.StartDate = model.StartDate!.Value;
        trip.EndDate = model.EndDate!.Value;

        await _db.SaveChangesAsync();

        TempData["Message"] = "Gezi bilgileri güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: /Trips/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var trip = await _db.Trips
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TripListItemViewModel
            {
                Id = t.Id,
                Name = t.Name,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                OwnerName = t.Owner.FullName,
                MemberCount = t.Members.Count
            })
            .FirstOrDefaultAsync();

        if (trip is null)
            return NotFound();
        if (!await CanManageAsync(id))
            return Forbid();

        return View(trip);
    }

    // POST: /Trips/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var trip = await _db.Trips.FirstOrDefaultAsync(t => t.Id == id);
        if (trip is null)
            return NotFound();
        if (!await CanManageAsync(id))
            return Forbid();

        _db.Trips.Remove(trip);
        await _db.SaveChangesAsync();

        TempData["Message"] = $"\"{trip.Name}\" gezisi silindi.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Trips/Join
    public IActionResult Join()
    {
        return View(new JoinTripViewModel());
    }

    // POST: /Trips/Join
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(JoinTripViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var code = model.InviteCode.Trim().ToUpperInvariant();
        var trip = await _db.Trips
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.InviteCode == code);

        if (trip is null)
        {
            ModelState.AddModelError(nameof(model.InviteCode), "Bu davet koduyla bir gezi bulunamadı.");
            return View(model);
        }

        var userId = CurrentUserId;
        if (trip.Members.Any(m => m.UserId == userId))
        {
            TempData["Message"] = $"Zaten \"{trip.Name}\" gezisinin üyesisin.";
            return RedirectToAction(nameof(Details), new { id = trip.Id });
        }

        // Davet koduyla katılan kişi Düzenleyici (Editor) olarak başlar; kurucu bunu değiştirebilir
        trip.Members.Add(new TripMember { UserId = userId, Role = TripRole.Editor });
        await _db.SaveChangesAsync();

        TempData["Message"] = $"\"{trip.Name}\" gezisine katıldın.";
        return RedirectToAction(nameof(Details), new { id = trip.Id });
    }

    // POST: /Trips/ChangeRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(int tripId, string userId, TripRole role)
    {
        if (!await CanManageAsync(tripId))
            return Forbid();

        // Kurucu rolü devredilemez; üyelere yalnızca Düzenleyici veya İzleyici verilebilir
        if (role == TripRole.Owner)
            return BadRequest();

        var member = await _db.TripMembers
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userId);
        if (member is null)
            return NotFound();

        if (member.Role == TripRole.Owner)
        {
            TempData["Error"] = "Kurucunun rolü değiştirilemez.";
            return RedirectToAction(nameof(Details), new { id = tripId });
        }

        member.Role = role;
        await _db.SaveChangesAsync();

        TempData["Message"] = "Üyenin rolü güncellendi.";
        return RedirectToAction(nameof(Details), new { id = tripId });
    }

    // POST: /Trips/RemoveMember
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int tripId, string userId)
    {
        if (!await CanManageAsync(tripId))
            return Forbid();

        var member = await _db.TripMembers
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userId);
        if (member is null)
            return NotFound();

        if (member.Role == TripRole.Owner)
        {
            TempData["Error"] = "Kurucu geziden çıkarılamaz.";
            return RedirectToAction(nameof(Details), new { id = tripId });
        }

        _db.TripMembers.Remove(member);
        await _db.SaveChangesAsync();

        TempData["Message"] = "Üye geziden çıkarıldı.";
        return RedirectToAction(nameof(Details), new { id = tripId });
    }

    private async Task<TripRole?> GetMyRoleAsync(int tripId)
    {
        var userId = CurrentUserId;
        return await _db.TripMembers
            .Where(m => m.TripId == tripId && m.UserId == userId)
            .Select(m => (TripRole?)m.Role)
            .FirstOrDefaultAsync();
    }

    // Gezi bilgilerini Kurucu ve Düzenleyici değiştirebilir
    private async Task<bool> CanEditAsync(int tripId)
    {
        if (IsAdmin)
            return true;
        var role = await GetMyRoleAsync(tripId);
        return role is TripRole.Owner or TripRole.Editor;
    }

    // Geziyi silme ve üye yönetimi yalnızca Kurucuya (ve Admin'e) açıktır
    private async Task<bool> CanManageAsync(int tripId)
    {
        if (IsAdmin)
            return true;
        return await GetMyRoleAsync(tripId) == TripRole.Owner;
    }
}
