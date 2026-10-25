using System.ComponentModel.DataAnnotations;

namespace OrtakRota.Web.Models;

public class ItineraryItem
{
    public int Id { get; set; }

    public int TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    public int PlaceId { get; set; }
    public Place Place { get; set; } = null!;

    [Range(1, 60, ErrorMessage = "Gün numarası 1 ile 60 arasında olmalıdır.")]
    [Display(Name = "Gün")]
    public int DayNumber { get; set; } = 1;

    [Display(Name = "Sıra")]
    public int OrderIndex { get; set; }

    [Display(Name = "Saat")]
    public TimeOnly? StartTime { get; set; }

    [StringLength(250)]
    [Display(Name = "Not")]
    public string? Note { get; set; }
}
