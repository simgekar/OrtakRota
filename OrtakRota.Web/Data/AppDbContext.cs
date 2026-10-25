using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OrtakRota.Web.Models;

namespace OrtakRota.Web.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripMember> TripMembers => Set<TripMember>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<ItineraryItem> ItineraryItems => Set<ItineraryItem>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseShare> ExpenseShares => Set<ExpenseShare>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Trip>(e =>
        {
            e.HasIndex(t => t.InviteCode).IsUnique();
            e.HasOne(t => t.Owner)
                .WithMany(u => u.OwnedTrips)
                .HasForeignKey(t => t.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TripMember>(e =>
        {
            e.HasIndex(m => new { m.TripId, m.UserId }).IsUnique();
            e.HasOne(m => m.Trip)
                .WithMany(t => t.Members)
                .HasForeignKey(m => m.TripId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.User)
                .WithMany(u => u.TripMemberships)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Place>(e =>
        {
            e.HasOne(p => p.Trip)
                .WithMany(t => t.Places)
                .HasForeignKey(p => p.TripId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.AddedBy)
                .WithMany(u => u.AddedPlaces)
                .HasForeignKey(p => p.AddedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Vote>(e =>
        {
            e.HasIndex(v => new { v.PlaceId, v.UserId }).IsUnique();
            e.HasOne(v => v.Place)
                .WithMany(p => p.Votes)
                .HasForeignKey(v => v.PlaceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(v => v.User)
                .WithMany(u => u.Votes)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ItineraryItem>(e =>
        {
            e.HasIndex(i => new { i.TripId, i.DayNumber, i.OrderIndex });
            e.HasOne(i => i.Trip)
                .WithMany(t => t.ItineraryItems)
                .HasForeignKey(i => i.TripId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Place)
                .WithMany(p => p.ItineraryItems)
                .HasForeignKey(i => i.PlaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Expense>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(12, 2);
            e.HasOne(x => x.Trip)
                .WithMany(t => t.Expenses)
                .HasForeignKey(x => x.TripId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.PaidBy)
                .WithMany(u => u.PaidExpenses)
                .HasForeignKey(x => x.PaidById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ExpenseShare>(e =>
        {
            e.Property(s => s.ShareAmount).HasPrecision(12, 2);
            e.HasIndex(s => new { s.ExpenseId, s.UserId }).IsUnique();
            e.HasOne(s => s.Expense)
                .WithMany(x => x.Shares)
                .HasForeignKey(s => s.ExpenseId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.User)
                .WithMany(u => u.ExpenseShares)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ChecklistItem>(e =>
        {
            e.HasOne(c => c.Trip)
                .WithMany(t => t.ChecklistItems)
                .HasForeignKey(c => c.TripId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.CheckedBy)
                .WithMany()
                .HasForeignKey(c => c.CheckedById)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
