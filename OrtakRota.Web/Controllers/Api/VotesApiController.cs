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
[Tags("Oylar")]
public class VotesApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TripAccessService _access;

    public VotesApiController(AppDbContext db, TripAccessService access)
    {
        _db = db;
        _access = access;
    }

    // GET: api/trips/5/votes/mine — giriş yapan kullanıcının bu gezideki oyları
    [HttpGet("trips/{tripId:int}/votes/mine")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<MyVoteDto>>> GetMyVotes(int tripId)
    {
        if (!await _access.CanViewAsync(tripId, User))
            return Forbid();

        var userId = TripAccessService.GetUserId(User);
        var votes = await _db.Votes
            .AsNoTracking()
            .Where(v => v.UserId == userId && v.Place.TripId == tripId)
            .Select(v => new MyVoteDto { PlaceId = v.PlaceId, Value = v.Value })
            .ToListAsync();

        return Ok(votes);
    }

    // PUT: api/places/7/vote — oy ver ya da oyu değiştir (kullanıcı başına tek oy)
    [HttpPut("places/{placeId:int}/vote")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VoteResultDto>> Vote(int placeId, VoteSaveDto dto)
    {
        var tripId = await GetTripIdAsync(placeId);
        if (tripId is null)
            return NotFound(new { message = $"{placeId} numaralı mekan bulunamadı." });

        // Gezinin tüm üyeleri (İzleyici dahil) oy verebilir
        if (!await _access.CanViewAsync(tripId.Value, User))
            return Forbid();

        var userId = TripAccessService.GetUserId(User);
        var vote = await _db.Votes.FirstOrDefaultAsync(v => v.PlaceId == placeId && v.UserId == userId);

        if (vote is null)
        {
            _db.Votes.Add(new Vote { PlaceId = placeId, UserId = userId, Value = dto.Value });
        }
        else
        {
            vote.Value = dto.Value;
            vote.CreatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(await GetResultAsync(placeId, userId));
    }

    // DELETE: api/places/7/vote — oyu geri al
    [HttpDelete("places/{placeId:int}/vote")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VoteResultDto>> RemoveVote(int placeId)
    {
        var tripId = await GetTripIdAsync(placeId);
        if (tripId is null)
            return NotFound(new { message = $"{placeId} numaralı mekan bulunamadı." });
        if (!await _access.CanViewAsync(tripId.Value, User))
            return Forbid();

        var userId = TripAccessService.GetUserId(User);
        var vote = await _db.Votes.FirstOrDefaultAsync(v => v.PlaceId == placeId && v.UserId == userId);
        if (vote is not null)
        {
            _db.Votes.Remove(vote);
            await _db.SaveChangesAsync();
        }

        return Ok(await GetResultAsync(placeId, userId));
    }

    private async Task<int?> GetTripIdAsync(int placeId) =>
        await _db.Places
            .Where(p => p.Id == placeId)
            .Select(p => (int?)p.TripId)
            .FirstOrDefaultAsync();

    private async Task<VoteResultDto> GetResultAsync(int placeId, string userId)
    {
        var votes = _db.Votes.AsNoTracking().Where(v => v.PlaceId == placeId);

        return new VoteResultDto
        {
            PlaceId = placeId,
            Score = await votes.SumAsync(v => v.Value),
            VoteCount = await votes.CountAsync(),
            MyVote = await votes
                .Where(v => v.UserId == userId)
                .Select(v => (int?)v.Value)
                .FirstOrDefaultAsync()
        };
    }
}
