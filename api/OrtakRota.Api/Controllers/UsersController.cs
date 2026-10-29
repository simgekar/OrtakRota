using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrtakRota.Api.Data;
using OrtakRota.Api.Dtos;
using OrtakRota.Web.Models;

namespace OrtakRota.Api.Controllers;

// Yalnızca Admin rolüne açık yönetim uç noktaları (7. haftada genişletilecek)
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Roles = DbSeeder.AdminRole)]
public class UsersController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;

    public UsersController(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    // GET: api/users
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .ToListAsync();

        var result = new List<UserDto>();
        foreach (var user in users)
        {
            result.Add(new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Roles = (await _userManager.GetRolesAsync(user)).ToList()
            });
        }
        return Ok(result);
    }
}
