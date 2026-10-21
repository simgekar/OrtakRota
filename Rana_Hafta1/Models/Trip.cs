using System.ComponentModel.DataAnnotations;

namespace OrtakRota.Web.Models;

public class Trip
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Gezi adı zorunludur.")]
    [StringLength(100, ErrorMessage = "Gezi adı en fazla 100 karakter olabilir.")]
    [Display(Name = "Gezi Adı")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    [Display(Name = "Başlangıç Tarihi")]
    public DateOnly StartDate { get; set; }

    [Required(ErrorMessage = "Bitiş tarihi zorunludur.")]
    [Display(Name = "Bitiş Tarihi")]
    public DateOnly EndDate { get; set; }

    [Required]
    [StringLength(8, MinimumLength = 6)]
    [Display(Name = "Davet Kodu")]
    public string InviteCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string OwnerId { get; set; } = string.Empty;
    public AppUser Owner { get; set; } = null!;

    public ICollection<TripMember> Members { get; set; } = new List<TripMember>();
    public ICollection<Place> Places { get; set; } = new List<Place>();
    public ICollection<ItineraryItem> ItineraryItems { get; set; } = new List<ItineraryItem>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<ChecklistItem> ChecklistItems { get; set; } = new List<ChecklistItem>();
}
