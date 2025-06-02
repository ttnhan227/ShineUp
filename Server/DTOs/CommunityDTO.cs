using Server.Models;

namespace Server.DTOs;

public class CommunityDTO
{
    public int CommunityID { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public int CreatedByUserID { get; set; }

    public int? PrivacyID { get; set; }
    public List<int> MemberUserIds { get; set; } = new();
}
