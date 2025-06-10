using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models;

public class User
{
    [Key] public int UserID { get; set; }

    [Required]
    public string Username { get; set; }

    public string? FullName { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    public string? GoogleId { get; set; }
    public string? PasswordHash { get; set; }

    public string? Bio { get; set; }
    public string? ProfileImageURL { get; set; }

    // Social Media Links
    [Url]
    public string? InstagramUrl { get; set; }

    [Url]
    public string? YouTubeUrl { get; set; }

    [Url]
    public string? TwitterUrl { get; set; }

    // Profile Media
    public string? CoverPhotoUrl { get; set; }

    [ForeignKey("Role")]
    public int RoleID { get; set; }

    public Role Role { get; set; }

    public string? TalentArea { get; set; }
    public DateTime CreatedAt { get; set; }

    // New properties for user status
    public bool IsActive { get; set; } = true; // Default to true for existing users
    public bool Verified { get; set; } = false; // Default to false, needs verification

    // New properties for profile enhancements
    public DateTime? LastLoginTime { get; set; }
    public ProfilePrivacy ProfilePrivacy { get; set; } = ProfilePrivacy.Public; // Default to public

    // Navigation properties
    public ICollection<Video> Videos { get; set; }
    public ICollection<Comment> Comments { get; set; }
    public ICollection<Like> Likes { get; set; }
    public ICollection<ContestEntry> ContestEntries { get; set; }
    public ICollection<Message> SentMessages { get; set; }
    public ICollection<Message> ReceivedMessages { get; set; }
    public ICollection<Post> Posts { get; set; }
    public ICollection<Image> Images { get; set; }
    public ICollection<Community> CreatedCommunities { get; set; }
    public ICollection<CommunityMember> CommunityMemberships { get; set; }
    public ICollection<TalentOpportunity> PostedOpportunities { get; set; }
    public ICollection<OpportunityApplication> OpportunityApplications { get; set; }
    public ICollection<Notification> Notifications { get; set; }
}