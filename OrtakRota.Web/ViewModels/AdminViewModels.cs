using OrtakRota.Web.Models;

namespace OrtakRota.Web.ViewModels;

// Yönetici paneli özet sayfası
public class AdminDashboardViewModel
{
    public int UserCount { get; set; }
    public int AdminCount { get; set; }
    public int LockedCount { get; set; }
    public int TripCount { get; set; }
    public int PlaceCount { get; set; }
    public int VoteCount { get; set; }
    public List<AdminUserRow> RecentUsers { get; set; } = new();
    public List<AdminTripRow> RecentTrips { get; set; } = new();
}

public class AdminUserRow
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsLocked { get; set; }
    public int TripCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminTripRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int MemberCount { get; set; }
    public int PlaceCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminPlaceRow
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public string TripName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PlaceCategory Category { get; set; }
    public string AddedByName { get; set; } = string.Empty;
    public int Score { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Mekanlar sayfası: liste + gezi filtresi
public class AdminPlacesViewModel
{
    public int? TripId { get; set; }
    public string? Query { get; set; }
    public List<AdminPlaceRow> Places { get; set; } = new();
    public List<AdminTripOption> Trips { get; set; } = new();
}

public class AdminTripOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
