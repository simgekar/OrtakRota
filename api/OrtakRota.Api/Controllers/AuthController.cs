using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using OrtakRota.Api.Data;
using OrtakRota.Api.Dtos;
using OrtakRota.Api.Services;
using OrtakRota.Web.Models;

namespace OrtakRota.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly TokenService _tokenService;

    public AuthController(UserManager<AppUser> userManager, TokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    // POST: api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        var user = new AppUser
        {
            UserName = dto.Email.Trim(),
            Email = dto.Email.Trim(),
            FullName = dto.FullName.Trim()
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(ErrorField(error.Code), Translate(error));
            return ValidationProblem(ModelState);
        }

        // Kayıt olan herkes "User" rolüyle başlar; Admin rolü yalnızca seed ile verilir
        await _userManager.AddToRoleAsync(user, DbSeeder.UserRole);

        return Ok(await _tokenService.CreateTokenAsync(user));
    }

    // POST: api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (user is null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            return Unauthorized(new { message = "E-posta veya şifre hatalı." });

        return Ok(await _tokenService.CreateTokenAsync(user));
    }

    // GET: api/auth/me  (token'ı geçerli olan kullanıcının bilgileri)
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Me()
    {
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var user = userId is null ? null : await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Unauthorized();

        return Ok(new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Roles = (await _userManager.GetRolesAsync(user)).ToList()
        });
    }

    private static string ErrorField(string code) =>
        code.StartsWith("Password") ? nameof(RegisterDto.Password)
        : code.Contains("Email") || code.Contains("UserName") ? nameof(RegisterDto.Email)
        : string.Empty;

    private static string Translate(IdentityError error) => error.Code switch
    {
        "DuplicateEmail" or "DuplicateUserName" => "Bu e-posta adresiyle kayıtlı bir kullanıcı zaten var.",
        "InvalidEmail" or "InvalidUserName" => "Geçerli bir e-posta adresi girin.",
        "PasswordTooShort" => "Şifre en az 6 karakter olmalıdır.",
        "PasswordRequiresDigit" => "Şifre en az bir rakam içermelidir.",
        "PasswordRequiresUpper" => "Şifre en az bir büyük harf içermelidir.",
        "PasswordRequiresLower" => "Şifre en az bir küçük harf içermelidir.",
        _ => error.Description
    };
}
