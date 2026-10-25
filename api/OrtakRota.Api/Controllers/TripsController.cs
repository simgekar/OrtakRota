using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrtakRota.Api.Data;
using OrtakRota.Api.Dtos;
using OrtakRota.Web.Data;
using OrtakRota.Web.Models;

namespace OrtakRota.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
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
        var trips = await _db.Trips
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
                CreatedAt = t.CreatedAt
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
        var trip = await _db.Trips
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
                PlaceCount = t.Places.Count,
                ExpenseCount = t.Expenses.Count,
                Members = t.Members
                    .OrderByDescending(m => m.Role)
                    .Select(m => new TripMemberDto
                    {
                        UserId = m.UserId,
                        FullName = m.User.FullName,
                        Role = m.Role
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (trip is null)
            return NotFound(new { message = $"{id} numaralı gezi bulunamadı." });

        return Ok(trip);
    }

    // POST: api/trips
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TripDto>> Create(TripSaveDto dto)
    {
        var ownerId = await GetCurrentUserIdAsync();
        if (ownerId is null)
            return Problem("Gezi sahibi olacak kullanıcı bulunamadı.");

        var trip = new Trip
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            StartDate = dto.StartDate!.Value,
            EndDate = dto.EndDate!.Value,
            InviteCode = await GenerateInviteCodeAsync(),
            OwnerId = ownerId
        };
        // Geziyi kuran kişi otomatik olarak Owner rolüyle üye olur
        trip.Members.Add(new TripMember { UserId = ownerId, Role = TripRole.Owner });

        _db.Trips.Add(trip);
        await _db.SaveChangesAsync();

        var ownerName = await _db.Users
            .Where(u => u.Id == ownerId)
            .Select(u => u.FullName)
            .FirstAsync();

        var result = new TripDto
        {
            Id = trip.Id,
            Name = trip.Name,
            Description = trip.Description,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            InviteCode = trip.InviteCode,
            OwnerName = ownerName,
            MemberCount = 1,
            CreatedAt = trip.CreatedAt
        };
        return CreatedAtAction(nameof(GetById), new { id = trip.Id }, result);
    }

    // PUT: api/trips/5
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, TripSaveDto dto)
    {
        var trip = await _db.Trips.FindAsync(id);
        if (trip is null)
            return NotFound(new { message = $"{id} numaralı gezi bulunamadı." });

        trip.Name = dto.Name.Trim();
        trip.Description = dto.Description?.Trim();
        trip.StartDate = dto.StartDate!.Value;
        trip.EndDate = dto.EndDate!.Value;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/trips/5
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var trip = await _db.Trips.FindAsync(id);
        if (trip is null)
            return NotFound(new { message = $"{id} numaralı gezi bulunamadı." });

        _db.Trips.Remove(trip);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // 1. hafta: giriş henüz yok, işlemler demo kullanıcı adına yapılır.
    // 2. haftada JWT eklenince kullanıcı kimliği token'dan okunacak.
    private async Task<string?> GetCurrentUserIdAsync()
    {
        return await _db.Users
            .Where(u => u.Email == DbSeeder.DemoUserEmail)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<string> GenerateInviteCodeAsync()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        string code;
        do
        {
            code = RandomNumberGenerator.GetString(chars, 6);
        } while (await _db.Trips.AnyAsync(t => t.InviteCode == code));
        return code;
    }
}
