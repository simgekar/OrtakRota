using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using OrtakRota.Web.Data;

namespace OrtakRota.Web.Services;

public static class InviteCodeGenerator
{
    // Birbirine benzeyen karakterler (0/O, 1/I) bilerek çıkarıldı
    private const string Characters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int Length = 6;

    public static async Task<string> GenerateUniqueAsync(AppDbContext db)
    {
        string code;
        do
        {
            code = RandomNumberGenerator.GetString(Characters, Length);
        } while (await db.Trips.AnyAsync(t => t.InviteCode == code));

        return code;
    }
}
