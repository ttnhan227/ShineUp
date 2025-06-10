using System.ComponentModel.DataAnnotations;

namespace Server.DTOs;

public class CommunityDTO
{
    public int CommunityID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string CoverImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserID { get; set; }
    public int? PrivacyID { get; set; }
    public bool IsCurrentUserModerator { get; set; }
    public bool IsCurrentUserMember { get; set; }
    public List<int> MemberUserIds { get; set; } = new();
    public List<CommunityMemberDTO> Members { get; set; } = new();
    public List<PostListResponseDto> Posts { get; set; } = new();
}

public class CreateCommunityDTO
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }

    public IFormFile? CoverImage { get; set; }

    public int? PrivacyID { get; set; }
}

public class UpdateCommunityDTO
{
    [Required(ErrorMessage = "CommunityID is required")]
    public int CommunityID { get; set; }

    [MaxLength(100, ErrorMessage = "Community name must not exceed 100 characters")]
    public string? Name { get; set; }

    [MaxLength(500, ErrorMessage = "Description must not exceed 500 characters")]
    public string? Description { get; set; }

    public IFormFile? CoverImage { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Invalid PrivacyID")]
    public int? PrivacyID { get; set; }
}