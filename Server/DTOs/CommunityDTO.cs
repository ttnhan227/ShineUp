using Server.DTOs;

public class CommunityDTO
{
    public int CommunityID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string CoverImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserID { get; set; }
    public int? PrivacyID { get; set; }
    public bool IsCurrentUserAdmin { get; set; }
    
    public bool IsCurrentUserMember { get; set; }
    public List<int> MemberUserIds { get; set; }
    public List<CommunityMemberDTO> Members { get; set; }
    public List<PostListResponseDto> Posts { get; set; }
}