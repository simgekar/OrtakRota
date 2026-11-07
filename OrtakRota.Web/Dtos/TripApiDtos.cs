using System.ComponentModel.DataAnnotations;
using OrtakRota.Web.Models;

namespace OrtakRota.Web.Dtos;

// API'den dönen gezi bilgisi
public class TripApiDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string InviteCode { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public int PlaceCount { get; set; }
    public TripRole? MyRole { get; set; }
}

// Gezi eklerken ve güncellerken istemciden gelen veri
public class TripSaveApiDto : IValidatableObject
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
