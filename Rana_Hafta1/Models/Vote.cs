using System.ComponentModel.DataAnnotations;

namespace OrtakRota.Web.Models;

public class Vote
{
    public int Id { get; set; }

    public int PlaceId { get; set; }
    public Place Place { get; set; } = null!;

    [Required]
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    [Range(-1, 1, ErrorMessage = "Oy değeri +1 veya -1 olmalıdır.")]
    public int Value { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
