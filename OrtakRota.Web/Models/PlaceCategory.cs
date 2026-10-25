using System.ComponentModel.DataAnnotations;

namespace OrtakRota.Web.Models;

public enum PlaceCategory
{
    [Display(Name = "Gezilecek Yer")] Sight = 0,
    [Display(Name = "Restoran / Kafe")] Food = 1,
    [Display(Name = "Müze")] Museum = 2,
    [Display(Name = "Konaklama")] Accommodation = 3,
    [Display(Name = "Alışveriş")] Shopping = 4,
    [Display(Name = "Diğer")] Other = 5
}
