using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrtakRota.Api.Dtos;
using OrtakRota.Api.Services;
using OrtakRota.Web.Data;
using OrtakRota.Web.Models;

namespace OrtakRota.Api.Controllers;

// Fikir havuzu: gezide gidilecek mekan önerileri ve bunlara verilen oylar
[ApiController]
[Route("api")]
[Produces("application/json")]
[Authorize]
public class PlacesController : ControllerBase
{
    private static readonly Dictionary<PlaceCategory, string> CategoryNames = new()
    {
        [PlaceCategory.Sight] = "Gezilecek Yer",
        [PlaceCategory.Food] = "Restoran / Kafe",
        [PlaceCategory.Museum] = "Müze",
        [PlaceCategory.Accommodation] = "Konaklama",
        [PlaceCategory.Shopping] = "Alışveriş",
        [PlaceCategory.Other] = "Diğer"
    };

    private readonly AppDbContext _db;
    private readonly TripAccessService _access;

    public PlacesController(AppDbContext db, TripAccessService access)
    {
        _db = db;
        _access = access;
    }

    // GET: api/trips/5/places?category=Food  — en çok oy alandan en aza
    [HttpGet("trips/{tripId:int}/places")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<PlaceDto>>> GetByTrip(int tripId, [FromQuery] PlaceCategory? category)
    {
        var role = await _access.GetRoleAsync(tripId, User);
        if (role is null || !await _db.Trips.AnyAsync(t => t.Id == tripId))
            return NotFound(new { message = $"{tripId} numaralı gezi bulunamadı." });

        var query = QueryPlaces().Where(p => p.TripId == tripId);
        if (category.HasValue)
            query = query.Where(p => p.Category == category.Value);

        var places = await query
            .OrderByDescending(p => p.Score)
            .ThenBy(p => p.Name)
            .ToListAsync();

        places.ForEach(p => Complete(p, role.Value));
        return Ok(places);
    }

    // GET: api/places/7
    [HttpGet("places/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlaceDto>> GetById(int id)
    {
        var (place, role) = await LoadAsync(id);
        if (place is null || role is null)
            return NotFound(new { message = $"{id} numaralı mekan bulunamadı." });

        return Ok(place);
    }

    // POST: api/trips/5/places  (Kurucu ve Düzenleyici)
    [HttpPost("trips/{tripId:int}/places")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlaceDto>> Create(int tripId, PlaceSaveDto dto)
    {
        var role = await _access.GetRoleAsync(tripId, User);
        if (role is null || !await _db.Trips.AnyAsync(t => t.Id == tripId))
            return NotFound(new { message = $"{tripId} numaralı gezi bulunamadı." });
        if (role < TripRole.Editor)
            return Forbid();

        var place = new Place
        {
            TripId = tripId,
            AddedById = User.GetUserId()
        };
        Apply(place, dto);

        _db.Places.Add(place);
        await _db.SaveChangesAsync();

        var (created, _) = await LoadAsync(place.Id);
        return CreatedAtAction(nameof(GetById), new { id = place.Id }, created);
    }

    // PUT: api/places/7  (mekanı ekleyen kişi ya da Kurucu)
    [HttpPut("places/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, PlaceSaveDto dto)
    {
        var place = await _db.Places.FirstOrDefaultAsync(p => p.Id == id);
        var role = place is null ? null : await _access.GetRoleAsync(place.TripId, User);
        if (place is null || role is null)
            return NotFound(new { message = $"{id} numaralı mekan bulunamadı." });
        if (!CanEdit(place.AddedById, role.Value))
            return Forbid();

        Apply(place, dto);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/places/7  (mekanı ekleyen kişi ya da Kurucu)
    [HttpDelete("places/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var place = await _db.Places.FirstOrDefaultAsync(p => p.Id == id);
        var role = place is null ? null : await _access.GetRoleAsync(place.TripId, User);
        if (place is null || role is null)
            return NotFound(new { message = $"{id} numaralı mekan bulunamadı." });
        if (!CanEdit(place.AddedById, role.Value))
            return Forbid();

        _db.Places.Remove(place);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // PUT: api/places/7/vote  { "value": 1 }  — oy verir ya da oyu değiştirir (gezinin her üyesi)
    [HttpPut("places/{id:int}/vote")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlaceDto>> Vote(int id, VoteDto dto)
    {
        var place = await _db.Places.FirstOrDefaultAsync(p => p.Id == id);
        var role = place is null ? null : await _access.GetRoleAsync(place.TripId, User);
        if (place is null || role is null)
            return NotFound(new { message = $"{id} numaralı mekan bulunamadı." });

        // Her kullanıcının bir mekana tek oyu olur; tekrar oy verirse eskisi güncellenir
        var userId = User.GetUserId();
        var vote = await _db.Votes.FirstOrDefaultAsync(v => v.PlaceId == id && v.UserId == userId);
        if (vote is null)
        {
            _db.Votes.Add(new Vote { PlaceId = id, UserId = userId, Value = dto.Value!.Value });
        }
        else
        {
            vote.Value = dto.Value!.Value;
            vote.CreatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();

        var (updated, _) = await LoadAsync(id);
        return Ok(updated);
    }

    // DELETE: api/places/7/vote  — oyu geri alır
    [HttpDelete("places/{id:int}/vote")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlaceDto>> RemoveVote(int id)
    {
        var place = await _db.Places.FirstOrDefaultAsync(p => p.Id == id);
        var role = place is null ? null : await _access.GetRoleAsync(place.TripId, User);
        if (place is null || role is null)
            return NotFound(new { message = $"{id} numaralı mekan bulunamadı." });

        var userId = User.GetUserId();
        var vote = await _db.Votes.FirstOrDefaultAsync(v => v.PlaceId == id && v.UserId == userId);
        if (vote is not null)
        {
            _db.Votes.Remove(vote);
            await _db.SaveChangesAsync();
        }

        var (updated, _) = await LoadAsync(id);
        return Ok(updated);
    }

    private static void Apply(Place place, PlaceSaveDto dto)
    {
        place.Name = dto.Name.Trim();
        place.Category = dto.Category!.Value;
        place.Latitude = dto.Latitude!.Value;
        place.Longitude = dto.Longitude!.Value;
        place.Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim();
        place.Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();
    }

    // Düzenleyici en az olmalı; ayrıca mekanı kendisi eklemiş olmalı ya da Kurucu olmalı
    private bool CanEdit(string addedById, TripRole role) =>
        role >= TripRole.Editor && (role == TripRole.Owner || addedById == User.GetUserId());

    private void Complete(PlaceDto place, TripRole role)
    {
        place.CategoryName = CategoryNames.GetValueOrDefault(place.Category, "Diğer");
        place.CanEdit = CanEdit(place.AddedById, role);
    }

    private async Task<(PlaceDto? Place, TripRole? Role)> LoadAsync(int id)
    {
        var place = await QueryPlaces().FirstOrDefaultAsync(p => p.Id == id);
        if (place is null)
            return (null, null);

        var role = await _access.GetRoleAsync(place.TripId, User);
        if (role is null)
            return (null, null);

        Complete(place, role.Value);
        return (place, role);
    }

    private IQueryable<PlaceDto> QueryPlaces()
    {
        var userId = User.GetUserId();

        return _db.Places
            .AsNoTracking()
            .Select(p => new PlaceDto
            {
                Id = p.Id,
                TripId = p.TripId,
                Name = p.Name,
                Category = p.Category,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                Address = p.Address,
                Note = p.Note,
                AddedById = p.AddedById,
                AddedByName = p.AddedBy.FullName,
                CreatedAt = p.CreatedAt,
                Score = p.Votes.Sum(v => v.Value),
                UpVotes = p.Votes.Count(v => v.Value > 0),
                DownVotes = p.Votes.Count(v => v.Value < 0),
                MyVote = p.Votes.Where(v => v.UserId == userId).Select(v => v.Value).FirstOrDefault()
            });
    }
}
