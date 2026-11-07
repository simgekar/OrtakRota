namespace OrtakRota.Web.ViewModels;

// Fikir Havuzu sayfası: haritayı ve listeyi JavaScript API'den doldurur,
// sayfanın kendisi yalnızca gezinin kimliğini ve kullanıcının yetkisini taşır
public class PlacesPageViewModel
{
    public int TripId { get; set; }
    public string TripName { get; set; } = string.Empty;
    public bool CanEdit { get; set; }
}
