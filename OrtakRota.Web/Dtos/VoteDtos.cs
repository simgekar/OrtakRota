using System.ComponentModel.DataAnnotations;

namespace OrtakRota.Web.Dtos;

// Oy verirken istemciden gelen veri: +1 (beğen) veya -1 (beğenme)
public class VoteSaveDto
{
    [AllowedValues(1, -1, ErrorMessage = "Oy değeri +1 veya -1 olmalıdır.")]
    public int Value { get; set; }
}

// Oy işleminden sonra mekanın güncel durumu
public class VoteResultDto
{
    public int PlaceId { get; set; }
    public int Score { get; set; }
    public int VoteCount { get; set; }
    public int? MyVote { get; set; }
}

// Kullanıcının bir gezideki oyları
public class MyVoteDto
{
    public int PlaceId { get; set; }
    public int Value { get; set; }
}
