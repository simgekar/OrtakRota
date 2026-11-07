using System.ComponentModel.DataAnnotations;
using OrtakRota.Web.Models;

namespace OrtakRota.Api.Dtos;

// API'den dönen gezi bilgisi
public class TripDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string InviteCode { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }

    // Giriş yapan kullanıcının bu gezideki rolü (üye olmayan Admin için null)
    public TripRole? MyRole { get; set; }
}

public class TripMemberDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TripRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class TripDetailDto : TripDto
{
    public List<TripMemberDto> Members { get; set; } = new();
    public int PlaceCount { get; set; }
    public int ExpenseCount { get; set; }
}

// Ekleme ve güncellemede istemciden gelen veri
public class TripSaveDto : IValidatableObject
{
    [Required(ErrorMessage = "Gezi adı zorunludur.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Gezi adı 3-100 karakter arasında olmalıdır.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    public DateOnly? StartDate { get; set; }

    [Required(ErrorMessage = "Bitiş tarihi zorunludur.")]
    public DateOnly? EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate.HasValue && EndDate.HasValue && EndDate < StartDate)
        {
            yield return new ValidationResult(
                "Bitiş tarihi başlangıç tarihinden önce olamaz.",
                new[] { nameof(EndDate) });
        }
    }
}

public class JoinTripDto
{
    [Required(ErrorMessage = "Davet kodu zorunludur.")]
    [StringLength(8, MinimumLength = 6, ErrorMessage = "Davet kodu 6 karakterdir.")]
    public string InviteCode { get; set; } = string.Empty;
}

public class UpdateMemberRoleDto
{
    [Required(ErrorMessage = "Rol zorunludur.")]
    public TripRole? Role { get; set; }
}

public class InviteCodeDto
{
    public string InviteCode { get; set; } = string.Empty;
}
