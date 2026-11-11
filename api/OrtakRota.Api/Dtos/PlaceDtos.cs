using System.ComponentModel.DataAnnotations;
using OrtakRota.Web.Models;

namespace OrtakRota.Api.Dtos;

// API'den dönen mekan bilgisi (fikir havuzundaki bir öneri)
public class PlaceDto
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public string Name { get; set; } = string.Empty;
    public PlaceCategory Category { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
    public string AddedById { get; set; } = string.Empty;
    public string AddedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Oylar: Score = (+1 sayısı) - (-1 sayısı)
    public int Score { get; set; }
    public int UpVotes { get; set; }
    public int DownVotes { get; set; }

    // Giriş yapan kullanıcının oyu: 1, -1 ya da oy vermediyse 0
    public int MyVote { get; set; }

    // Giriş yapan kullanıcı bu mekanı düzenleyip silebilir mi?
    public bool CanEdit { get; set; }
}

// Mekan eklerken ve güncellerken istemciden gelen veri
public class PlaceSaveDto
{
    [Required(ErrorMessage = "Mekan adı zorunludur.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Mekan adı 2-150 karakter arasında olmalıdır.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kategori zorunludur.")]
    [EnumDataType(typeof(PlaceCategory), ErrorMessage = "Geçersiz kategori.")]
    public PlaceCategory? Category { get; set; }

    [Required(ErrorMessage = "Konum zorunludur. Haritaya dokunarak bir nokta seçin.")]
    [Range(-90, 90, ErrorMessage = "Enlem -90 ile 90 arasında olmalıdır.")]
    public double? Latitude { get; set; }

    [Required(ErrorMessage = "Konum zorunludur. Haritaya dokunarak bir nokta seçin.")]
    [Range(-180, 180, ErrorMessage = "Boylam -180 ile 180 arasında olmalıdır.")]
    public double? Longitude { get; set; }

    [StringLength(250, ErrorMessage = "Adres en fazla 250 karakter olabilir.")]
    public string? Address { get; set; }

    [StringLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir.")]
    public string? Note { get; set; }
}

public class VoteDto : IValidatableObject
{
    // +1: beğendim, -1: beğenmedim
    [Required(ErrorMessage = "Oy değeri zorunludur.")]
    public int? Value { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Value is not (1 or -1))
            yield return new ValidationResult("Oy değeri +1 veya -1 olmalıdır.", new[] { nameof(Value) });
    }
}
