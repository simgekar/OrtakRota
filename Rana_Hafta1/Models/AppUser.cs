using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace OrtakRota.Web.Models;

public class AppUser : IdentityUser
{
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(100, ErrorMessage = "Ad soyad en fazla 100 karakter olabilir.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Trip> OwnedTrips { get; set; } = new List<Trip>();
    public ICollection<TripMember> TripMemberships { get; set; } = new List<TripMember>();
    public ICollection<Place> AddedPlaces { get; set; } = new List<Place>();
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    public ICollection<Expense> PaidExpenses { get; set; } = new List<Expense>();
    public ICollection<ExpenseShare> ExpenseShares { get; set; } = new List<ExpenseShare>();
}
