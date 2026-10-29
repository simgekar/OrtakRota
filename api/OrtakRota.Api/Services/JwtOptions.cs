namespace OrtakRota.Api.Services;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "OrtakRota.Api";
    public string Audience { get; set; } = "OrtakRota.Mobile";
    public string Key { get; set; } = string.Empty;
    public int ExpiresMinutes { get; set; } = 120;
}
