using System.ComponentModel.DataAnnotations;
using OrtakRota.Web.Models;

namespace OrtakRota.Web.Dtos;

// API'den dönen mekan bilgisi
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
    public string AddedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int Score { get; set; }
    public int VoteCount { get; set; }
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

    [Required(ErrorMessage = "Konum zorunludur. Haritaya tıklayarak bir nokta seçin.")]
    [Range(-90, 90, ErrorMessage = "Enlem -90 ile 90 arasında olmalıdır.")]
    public double? Latitude { get; set; }

    [Required(ErrorMessage = "Konum zorunludur. Haritaya tıklayarak bir nokta seçin.")]
    [Range(-180, 180, ErrorMessage = "Boylam -180 ile 180 arasında olmalıdır.")]
    public double? Longitude { get; set; }

    [StringLength(250, ErrorMessage = "Adres en fazla 250 karakter olabilir.")]
    public string? Address { get; set; }

    [StringLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir.")]
    public string? Note { get; set; }
}
