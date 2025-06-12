using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Client.Models;

// DTO for community list items
public class CommunityListItemDto
{
    [JsonPropertyName("communityID")]
    public int CommunityID { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("privacyID")]
    public int? PrivacyID { get; set; }

    [JsonPropertyName("privacyName")]
    public string? PrivacyName { get; set; }

    [JsonPropertyName("memberCount")]
    public int MemberCount { get; set; }

    [JsonPropertyName("postCount")]
    public int PostCount { get; set; }

    [JsonPropertyName("isCurrentUserModerator")]
    public bool IsCurrentUserModerator { get; set; }

    [JsonPropertyName("isCurrentUserMember")]
    public bool IsCurrentUserMember { get; set; }
}

// Base Community ViewModel
public class CommunityViewModel
{
    [Key]
    [JsonPropertyName("communityID")]
    public int CommunityID { get; set; }

    [Required]
    [MaxLength(100)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [MaxLength(500)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("createdByUserID")]
    public int? CreatedByUserID { get; set; }

    [JsonPropertyName("createdBy")]
    public UserViewModel? CreatedBy { get; set; }

    [JsonPropertyName("privacyID")]
    public int? PrivacyID { get; set; }

    [JsonPropertyName("privacy")]
    public PrivacyViewModel? Privacy { get; set; }
    [Required(ErrorMessage = "Cover image is required.")]

    [JsonPropertyName("coverImageUrl")]
    public string? CoverImageUrl { get; set; }

    // Navigation
    [JsonPropertyName("posts")]
    public ICollection<PostViewModel> Posts { get; set; } = new List<PostViewModel>();

    [JsonPropertyName("members")]
    public ICollection<CommunityMemberViewModel> Members { get; set; } = new List<CommunityMemberViewModel>();
}