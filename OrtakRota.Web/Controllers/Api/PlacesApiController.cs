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
[Route("api")]
[Authorize]
[Produces("application/json")]
[Tags("Mekanlar")]
public class PlacesApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TripAccessService _access;

    public PlacesApiController(AppDbContext db, TripAccessService access)
    {
        _db = db;
        _access = access;
    }

    // GET: api/trips/5/places
    [HttpGet("trips/{tripId:int}/places")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<PlaceDto>>> GetByTrip(int tripId)
    {
        if (!await _db.Trips.AnyAsync(t => t.Id == tripId))
            return NotFound(new { message = $"{tripId} numaralı gezi bulunamadı." });
        if (!await _access.CanViewAsync(tripId, User))
            return Forbid();

        var places = await QueryPlaces()
            .Where(p => p.TripId == tripId)
            .OrderByDescending(p => p.Score)
            .ThenBy(p => p.Name)
            .ToListAsync();

        return Ok(WithCategoryNames(places));
    }

    // GET: api/places/7
    [HttpGet("places/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlaceDto>> GetById(int id)
    {
        var place = await QueryPlaces().FirstOrDefaultAsync(p => p.Id == id);
        if (place is null)
            return NotFound(new { message = $"{id} numaralı mekan bulunamadı." });
        if (!await _access.CanViewAsync(place.TripId, User))
            return Forbid();

        place.CategoryName = place.Category.GetDisplayName();
        return Ok(place);
    }

    // POST: api/trips/5/places
    [HttpPost("trips/{tripId:int}/places")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlaceDto>> Create(int tripId, PlaceSaveDto dto)
    {
        if (!await _db.Trips.AnyAsync(t => t.Id == tripId))
            return NotFound(new { message = $"{tripId} numaralı gezi bulunamadı." });
        if (!await _access.CanEditAsync(tripId, User))
            return Forbid();

        var place = new Place
        {
            TripId = tripId,
            AddedById = TripAccessService.GetUserId(User)
        };
        Apply(place, dto);

        _db.Places.Add(place);
        await _db.SaveChangesAsync();

        var created = await QueryPlaces().FirstAsync(p => p.Id == place.Id);
        created.CategoryName = created.Category.GetDisplayName();
        return CreatedAtAction(nameof(GetById), new { id = place.Id }, created);
    }

    // PUT: api/places/7
    [HttpPut("places/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, PlaceSaveDto dto)
    {
        var place = await _db.Places.FirstOrDefaultAsync(p => p.Id == id);
        if (place is null)
            return NotFound(new { message = $"{id} numaralı mekan bulunamadı." });
        if (!await _access.CanEditAsync(place.TripId, User))
            return Forbid();

        Apply(place, dto);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/places/7
    [HttpDelete("places/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var place = await _db.Places.FirstOrDefaultAsync(p => p.Id == id);
        if (place is null)
            return NotFound(new { message = $"{id} numaralı mekan bulunamadı." });
        if (!await _access.CanEditAsync(place.TripId, User))
            return Forbid();

        _db.Places.Remove(place);
        await _db.SaveChangesAsync();
        return NoContent();
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

    private IQueryable<PlaceDto> QueryPlaces() =>
        _db.Places
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
                AddedByName = p.AddedBy.FullName,
                CreatedAt = p.CreatedAt,
                Score = p.Votes.Sum(v => v.Value),
                VoteCount = p.Votes.Count
            });

    // Kategorinin Türkçe adı veritabanında değil enum'da durduğu için sorgudan sonra eklenir
    private static List<PlaceDto> WithCategoryNames(List<PlaceDto> places)
    {
        foreach (var place in places)
            place.CategoryName = place.Category.GetDisplayName();
        return places;
    }
}
