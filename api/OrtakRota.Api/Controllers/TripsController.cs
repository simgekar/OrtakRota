using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrtakRota.Api.Dtos;
using OrtakRota.Api.Services;
using OrtakRota.Web.Data;
using OrtakRota.Web.Models;

namespace OrtakRota.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class TripsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TripsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: api/trips
    [HttpGet]
    public async Task<ActionResult<List<TripDto>>> GetAll()
    {
        var userId = User.GetUserId();

        var trips = await VisibleTrips()
            .AsNoTracking()
            .OrderBy(t => t.StartDate)
            .Select(t => new TripDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                InviteCode = t.InviteCode,
                OwnerName = t.Owner.FullName,
                MemberCount = t.Members.Count,
                CreatedAt = t.CreatedAt,
                MyRole = t.Members.Where(m => m.UserId == userId).Select(m => (TripRole?)m.Role).FirstOrDefault()
            })
            .ToListAsync();

        return Ok(trips);
    }

    // GET: api/trips/5
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TripDetailDto>> GetById(int id)
    {
        var trip = await LoadDetailAsync(id);
        if (trip is null)
            return NotFound(new { message = $"{id} numaralı gezi bulunamadı." });

        return Ok(trip);
    }

    // POST: api/trips
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TripDetailDto>> Create(TripSaveDto dto)
    {
        var userId = User.GetUserId();

        var trip = new Trip
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            StartDate = dto.StartDate!.Value,
            EndDate = dto.EndDate!.Value,
            InviteCode = await InviteCodeGenerator.GenerateUniqueAsync(_db),
            OwnerId = userId
        };
        // Geziyi kuran kişi otomatik olarak Owner rolüyle üye olur
        trip.Members.Add(new TripMember { UserId = userId, Role = TripRole.Owner });

        _db.Trips.Add(trip);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = trip.Id }, await LoadDetailAsync(trip.Id));
    }

    // PUT: api/trips/5  (Kurucu ve Düzenleyici)
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, TripSaveDto dto)
    {
        var trip = await VisibleTrips().FirstOrDefaultAsync(t => t.Id == id);
        if (trip is null)
            return NotFound(new { message = $"{id} numaralı gezi bulunamadı." });
        if (!await HasRoleAsync(id, TripRole.Editor))
            return Forbid();

        trip.Name = dto.Name.Trim();
        trip.Description = dto.Description?.Trim();
        trip.StartDate = dto.StartDate!.Value;
        trip.EndDate = dto.EndDate!.Value;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/trips/5  (yalnızca Kurucu)
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var trip = await VisibleTrips().FirstOrDefaultAsync(t => t.Id == id);
        if (trip is null)
            return NotFound(new { message = $"{id} numaralı gezi bulunamadı." });
        if (!await HasRoleAsync(id, TripRole.Owner))
            return Forbid();

        _db.Trips.Remove(trip);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST: api/trips/join  — davet koduyla geziye Düzenleyici olarak katılma
    [HttpPost("join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TripDetailDto>> Join(JoinTripDto dto)
    {
        var userId = User.GetUserId();
        var code = dto.InviteCode.Trim().ToUpperInvariant();

        var trip = await _db.Trips.FirstOrDefaultAsync(t => t.InviteCode == code);
        if (trip is null)
            return NotFound(new { message = "Bu davet koduyla bir gezi bulunamadı." });

        if (await _db.TripMembers.AnyAsync(m => m.TripId == trip.Id && m.UserId == userId))
            return Conflict(new { message = "Bu gezinin zaten üyesisin.", tripId = trip.Id });

        _db.TripMembers.Add(new TripMember { TripId = trip.Id, UserId = userId, Role = TripRole.Editor });
        await _db.SaveChangesAsync();

        return Ok(await LoadDetailAsync(trip.Id));
    }

    // POST: api/trips/5/invite-code  — davet kodunu yeniler, eski kod geçersiz olur (yalnızca Kurucu)
    [HttpPost("{id:int}/invite-code")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InviteCodeDto>> RegenerateInviteCode(int id)
    {
        var trip = await VisibleTrips().FirstOrDefaultAsync(t => t.Id == id);
        if (trip is null)
            return NotFound(new { message = $"{id} numaralı gezi bulunamadı." });
        if (!await HasRoleAsync(id, TripRole.Owner))
            return Forbid();

        trip.InviteCode = await InviteCodeGenerator.GenerateUniqueAsync(_db);
        await _db.SaveChangesAsync();

        return Ok(new InviteCodeDto { InviteCode = trip.InviteCode });
    }

    // Admin tüm gezileri, diğer kullanıcılar yalnızca üyesi oldukları gezileri görür
    private IQueryable<Trip> VisibleTrips()
    {
        if (User.IsAdmin())
            return _db.Trips;

        var userId = User.GetUserId();
        return _db.Trips.Where(t => t.Members.Any(m => m.UserId == userId));
    }

    // Kullanıcının gezideki rolü en az "minimum" mu? (Viewer < Editor < Owner; Admin her zaman yetkili)
    private async Task<bool> HasRoleAsync(int tripId, TripRole minimum)
    {
        if (User.IsAdmin())
            return true;

        var userId = User.GetUserId();
        var role = await _db.TripMembers
            .Where(m => m.TripId == tripId && m.UserId == userId)
            .Select(m => (TripRole?)m.Role)
            .FirstOrDefaultAsync();

        return role.HasValue && role.Value >= minimum;
    }

    private async Task<TripDetailDto?> LoadDetailAsync(int id)
    {
        var userId = User.GetUserId();

        return await VisibleTrips()
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TripDetailDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                InviteCode = t.InviteCode,
                OwnerName = t.Owner.FullName,
                MemberCount = t.Members.Count,
                CreatedAt = t.CreatedAt,
                MyRole = t.Members.Where(m => m.UserId == userId).Select(m => (TripRole?)m.Role).FirstOrDefault(),
                PlaceCount = t.Places.Count,
                ExpenseCount = t.Expenses.Count,
                Members = t.Members
                    .OrderByDescending(m => m.Role)
                    .ThenBy(m => m.JoinedAt)
                    .Select(m => new TripMemberDto
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
    }
}
