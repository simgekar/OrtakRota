using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrtakRota.Web.Data;
using OrtakRota.Web.Dtos;
using OrtakRota.Web.Models;
using OrtakRota.Web.Services;

namespace OrtakRota.Web.Controllers.Api;

[ApiController]
[Route("api/trips")]
[Authorize]
[Produces("application/json")]
[Tags("Geziler")]
public class TripsApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TripAccessService _access;

    public TripsApiController(AppDbContext db, TripAccessService access)
    {
        _db = db;
        _access = access;
    }

    // GET: api/trips
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TripApiDto>>> GetAll()
    {
        var userId = TripAccessService.GetUserId(User);
        var isAdmin = TripAccessService.IsAdmin(User);

        var trips = await _db.Trips
            .AsNoTracking()
            .Where(t => isAdmin || t.Members.Any(m => m.UserId == userId))
            .OrderBy(t => t.StartDate)
            .Select(t => new TripApiDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                InviteCode = t.InviteCode,
                OwnerName = t.Owner.FullName,
                MemberCount = t.Members.Count,
                PlaceCount = t.Places.Count,
                MyRole = t.Members
                    .Where(m => m.UserId == userId)
                    .Select(m => (TripRole?)m.Role)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(trips);
    }

    // GET: api/trips/5
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TripApiDto>> GetById(int id)
    {
        var trip = await QueryTrip(id).FirstOrDefaultAsync();
        if (trip is null)
            return NotFound(new { message = $"{id} numaralı gezi bulunamadı." });
        if (!await _access.CanViewAsync(id, User))
            return Forbid();

        return Ok(trip);
    }

    // POST: api/trips
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TripApiDto>> Create(TripSaveApiDto dto)
    {
        var userId = TripAccessService.GetUserId(User);

        var trip = new Trip
        {
            Name = dto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            StartDate = dto.StartDate!.Value,
            EndDate = dto.EndDate!.Value,
            InviteCode = await InviteCodeGenerator.GenerateUniqueAsync(_db),
            OwnerId = userId
        };
        // Geziyi oluşturan kişi otomatik olarak Kurucu (Owner) rolüyle üye olur
        trip.Members.Add(new TripMember { UserId = userId, Role = TripRole.Owner });

        _db.Trips.Add(trip);
        await _db.SaveChangesAsync();

        var created = await QueryTrip(trip.Id).FirstAsync();
        return CreatedAtAction(nameof(GetById), new { id = trip.Id }, created);
    }

    // PUT: api/trips/5
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, TripSaveApiDto dto)
    {
        var trip = await _db.Trips.FirstOrDefaultAsync(t => t.Id == id);
        if (trip is null)
            return NotFound(new { message = $"{id} numaralı gezi bulunamadı." });
        if (!await _access.CanEditAsync(id, User))
            return Forbid();

        trip.Name = dto.Name.Trim();
        trip.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        trip.StartDate = dto.StartDate!.Value;
        trip.EndDate = dto.EndDate!.Value;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/trips/5
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var trip = await _db.Trips.FirstOrDefaultAsync(t => t.Id == id);
        if (trip is null)
            return NotFound(new { message = $"{id} numaralı gezi bulunamadı." });
        if (!await _access.CanManageAsync(id, User))
            return Forbid();

        _db.Trips.Remove(trip);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private IQueryable<TripApiDto> QueryTrip(int id)
    {
        var userId = TripAccessService.GetUserId(User);
        return _db.Trips
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TripApiDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                InviteCode = t.InviteCode,
                OwnerName = t.Owner.FullName,
                MemberCount = t.Members.Count,
                PlaceCount = t.Places.Count,
                MyRole = t.Members
                    .Where(m => m.UserId == userId)
                    .Select(m => (TripRole?)m.Role)
                    .FirstOrDefault()
            });
    }
}
