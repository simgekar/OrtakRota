using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrtakRota.Web.Data;
using OrtakRota.Web.Services;
using OrtakRota.Web.ViewModels;

namespace OrtakRota.Web.Controllers;

[Authorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class PlacesController : Controller
{
    private readonly AppDbContext _db;
    private readonly TripAccessService _access;

    public PlacesController(AppDbContext db, TripAccessService access)
    {
        _db = db;
        _access = access;
    }

    // GET: /Trips/5/Places
    [HttpGet("Trips/{tripId:int}/Places")]
    public async Task<IActionResult> Index(int tripId)
    {
        var tripName = await _db.Trips
            .AsNoTracking()
            .Where(t => t.Id == tripId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync();

        if (tripName is null)
            return NotFound();
        if (!await _access.CanViewAsync(tripId, User))
            return Forbid();

        var model = new PlacesPageViewModel
        {
            TripId = tripId,
            TripName = tripName,
            CanEdit = await _access.CanEditAsync(tripId, User)
        };

        return View(model);
    }
}
