using System.ComponentModel.DataAnnotations;

namespace OrtakRota.Web.Models;

public class Expense
{
    public int Id { get; set; }

    public int TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    [Required(ErrorMessage = "Açıklama zorunludur.")]
    [StringLength(150, ErrorMessage = "Açıklama en fazla 150 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 1_000_000, ErrorMessage = "Tutar 0,01 ile 1.000.000 arasında olmalıdır.")]
    [Display(Name = "Tutar")]
    public decimal Amount { get; set; }

    [Display(Name = "Tarih")]
    public DateOnly Date { get; set; }

    [Required]
    public string PaidById { get; set; } = string.Empty;
    public AppUser PaidBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ExpenseShare> Shares { get; set; } = new List<ExpenseShare>();
}
