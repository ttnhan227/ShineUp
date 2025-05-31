using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models;

public class Community
{
    [Key]
    public int CommunityID { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }


    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }


    [ForeignKey("CreatedByUserID")]
    public int CreatedByUserID { get; set; }
    public User CreatedBy { get; set; }

    public int? PrivacyID { get; set; }
    [ForeignKey("PrivacyID")]
    public Privacy? Privacy { get; set; }

    // Navigation properties
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<CommunityMember> Members { get; set; } = new List<CommunityMember>();
}
