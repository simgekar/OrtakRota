using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrtakRota.Api.Dtos;
using OrtakRota.Api.Services;
using OrtakRota.Web.Data;
using OrtakRota.Web.Models;

namespace OrtakRota.Api.Controllers;

// Gezi üyeleri: listeleme, rol değiştirme, üye çıkarma ve geziden ayrılma
[ApiController]
[Route("api/trips/{tripId:int}/members")]
[Produces("application/json")]
[Authorize]
public class TripMembersController : ControllerBase
{
    private readonly AppDbContext _db;

    public TripMembersController(AppDbContext db)
    {
        _db = db;
    }

    // GET: api/trips/5/members
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<TripMemberDto>>> GetAll(int tripId)
    {
        if (await GetMyRoleAsync(tripId) is null && !User.IsAdmin())
            return NotFound(new { message = $"{tripId} numaralı gezi bulunamadı." });

        var members = await _db.TripMembers
            .AsNoTracking()
            .Where(m => m.TripId == tripId)
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
            .ToListAsync();

        return Ok(members);
    }

    // PUT: api/trips/5/members/{userId}  — üyenin rolünü Düzenleyici/İzleyici yapar (yalnızca Kurucu)
    [HttpPut("{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(int tripId, string userId, UpdateMemberRoleDto dto)
    {
        var myRole = await GetMyRoleAsync(tripId);
        if (myRole is null && !User.IsAdmin())
            return NotFound(new { message = $"{tripId} numaralı gezi bulunamadı." });
        if (myRole != TripRole.Owner && !User.IsAdmin())
            return Forbid();

        var member = await _db.TripMembers.FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userId);
        if (member is null)
            return NotFound(new { message = "Bu kullanıcı gezinin üyesi değil." });
        if (member.Role == TripRole.Owner)
            return BadRequest(new { message = "Kurucunun rolü değiştirilemez." });
        if (dto.Role == TripRole.Owner)
            return BadRequest(new { message = "Bir geziye yalnızca bir kurucu atanabilir." });

        member.Role = dto.Role!.Value;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/trips/5/members/{userId}
    // Kendi kimliğiyle çağrılırsa geziden ayrılma; başkası için çağrılırsa üyeyi çıkarma (yalnızca Kurucu)
    [HttpDelete("{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(int tripId, string userId)
    {
        var myRole = await GetMyRoleAsync(tripId);
        if (myRole is null && !User.IsAdmin())
            return NotFound(new { message = $"{tripId} numaralı gezi bulunamadı." });

        var isSelf = userId == User.GetUserId();
        if (!isSelf && myRole != TripRole.Owner && !User.IsAdmin())
            return Forbid();

        var member = await _db.TripMembers.FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userId);
        if (member is null)
            return NotFound(new { message = "Bu kullanıcı gezinin üyesi değil." });
        if (member.Role == TripRole.Owner)
            return BadRequest(new { message = "Kurucu geziden çıkarılamaz. Gezi tamamen silinebilir." });

        _db.TripMembers.Remove(member);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<TripRole?> GetMyRoleAsync(int tripId)
    {
        var userId = User.GetUserId();
        return await _db.TripMembers
            .Where(m => m.TripId == tripId && m.UserId == userId)
            .Select(m => (TripRole?)m.Role)
            .FirstOrDefaultAsync();
    }
}
