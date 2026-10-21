using System.ComponentModel.DataAnnotations;

namespace OrtakRota.Web.Models;

public class ChecklistItem
{
    public int Id { get; set; }

    public int TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    [Required(ErrorMessage = "Madde metni zorunludur.")]
    [StringLength(150, ErrorMessage = "Madde en fazla 150 karakter olabilir.")]
    [Display(Name = "Madde")]
    public string Text { get; set; } = string.Empty;

    [Display(Name = "Tamamlandı")]
    public bool IsDone { get; set; }

    public string? CheckedById { get; set; }
    public AppUser? CheckedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
