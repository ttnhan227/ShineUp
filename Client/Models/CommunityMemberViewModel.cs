namespace Client.Models;

public class CommunityMemberViewModel
{
    public int UserID { get; set; }
    public UserViewModel User { get; set; } = null!;

    public int CommunityID { get; set; }
    public CommunityViewModel Community { get; set; } = null!;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastActiveAt { get; set; }

    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public CommunityRole Role { get; set; } = CommunityRole.Member;
}

public enum CommunityRole
{
    Member,
    Moderator
}