using System.ComponentModel.DataAnnotations;

namespace OrtakRota.Web.Models;

public class Place
{
    public int Id { get; set; }

    public int TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    [Required(ErrorMessage = "Mekan adı zorunludur.")]
    [StringLength(150, ErrorMessage = "Mekan adı en fazla 150 karakter olabilir.")]
    [Display(Name = "Mekan Adı")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Kategori")]
    public PlaceCategory Category { get; set; } = PlaceCategory.Sight;

    [Range(-90, 90, ErrorMessage = "Enlem -90 ile 90 arasında olmalıdır.")]
    [Display(Name = "Enlem")]
    public double Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Boylam -180 ile 180 arasında olmalıdır.")]
    [Display(Name = "Boylam")]
    public double Longitude { get; set; }

    [StringLength(250)]
    [Display(Name = "Adres")]
    public string? Address { get; set; }

    [StringLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir.")]
    [Display(Name = "Not")]
    public string? Note { get; set; }

    [Required]
    public string AddedById { get; set; } = string.Empty;
    public AppUser AddedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    public ICollection<ItineraryItem> ItineraryItems { get; set; } = new List<ItineraryItem>();
}
