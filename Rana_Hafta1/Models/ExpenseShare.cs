using System.ComponentModel.DataAnnotations;

namespace OrtakRota.Web.Models;

public class ExpenseShare
{
    public int Id { get; set; }

    public int ExpenseId { get; set; }
    public Expense Expense { get; set; } = null!;

    [Required]
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    [Range(0.0, 1_000_000.0)]
    [Display(Name = "Pay Tutarı")]
    public decimal ShareAmount { get; set; }
}
