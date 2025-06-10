using Newtonsoft.Json.Converters;
using System.Text.Json.Serialization;

namespace Client.Models;

public class CommunityMemberViewModel
{
    public int UserID { get; set; }
    public UserViewModel User { get; set; } = null!;

    public int CommunityID { get; set; }
    public CommunityViewModel Community { get; set; } = null!;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastActiveAt { get; set; }

    [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
    public CommunityRole Role { get; set; } = CommunityRole.Member;
}

public enum CommunityRole
{
    None, // Not a member
    Member, // Regular member
    Moderator // Community moderator
}