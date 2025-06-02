using System.ComponentModel.DataAnnotations;

namespace Server.Models;

public class Community
{
    [Key]
    public int CommunityID { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [Required]
    public int CreatedByUserID { get; set; }

    public User CreatedBy { get; set; } = null!;

    public int? PrivacyID { get; set; }

    public Privacy? Privacy { get; set; }

    public string? CoverImageUrl { get; set; }

    // Navigation
    public ICollection<Post> Posts { get; set; } = new List<Post>();

    public ICollection<CommunityMember> Members { get; set; } = new List<CommunityMember>();
}