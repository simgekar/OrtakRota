using System.ComponentModel.DataAnnotations;
using OrtakRota.Web.Models;

namespace OrtakRota.Web.ViewModels;

// Gezi oluşturma ve düzenleme formu
public class TripFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Gezi adı zorunludur.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Gezi adı 3-100 karakter arasında olmalıdır.")]
    [Display(Name = "Gezi Adı")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    [Display(Name = "Başlangıç Tarihi")]
    public DateOnly? StartDate { get; set; }

    [Required(ErrorMessage = "Bitiş tarihi zorunludur.")]
    [Display(Name = "Bitiş Tarihi")]
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

// Davet koduyla geziye katılma formu
public class JoinTripViewModel
{
    [Required(ErrorMessage = "Davet kodu zorunludur.")]
    [StringLength(8, MinimumLength = 6, ErrorMessage = "Davet kodu 6 karakterdir.")]
    [Display(Name = "Davet Kodu")]
    public string InviteCode { get; set; } = string.Empty;
}

// Gezilerim listesindeki her satır
public class TripListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public TripRole? MyRole { get; set; }
}

// Gezi detay sayfası
public class TripDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string InviteCode { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public TripRole? MyRole { get; set; }
    public bool CanEdit { get; set; }
    public bool CanManage { get; set; }
    public List<TripMemberViewModel> Members { get; set; } = new();
}

public class TripMemberViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TripRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}

public static class TripRoleExtensions
{
    public static string ToTurkish(this TripRole role) => role switch
    {
        TripRole.Owner => "Kurucu",
        TripRole.Editor => "Düzenleyici",
        TripRole.Viewer => "İzleyici",
        _ => role.ToString()
    };

    public static string BadgeClass(this TripRole role) => role switch
    {
        TripRole.Owner => "bg-primary",
        TripRole.Editor => "bg-success",
        _ => "bg-secondary"
    };
}
