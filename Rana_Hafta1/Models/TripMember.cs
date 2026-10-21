using System.ComponentModel.DataAnnotations;

namespace OrtakRota.Web.Models;

public class TripMember
{
    public int Id { get; set; }

    public int TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    [Required]
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    [Display(Name = "Gezi İçi Rol")]
    public TripRole Role { get; set; } = TripRole.Editor;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
