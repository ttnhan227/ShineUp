using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models;

public class Post
{
    [Key]
    public int PostID { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; }

    [Required]
    public string Content { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("UpdateAt")]
    public DateTime? UpdatedAt { get; set; }

    public int UserID { get; set; }

    [ForeignKey("UserID")]
    public User User { get; set; }

    public int? CategoryID { get; set; }

    [ForeignKey("CategoryID")]
    public Category Category { get; set; }

    public int? PrivacyID { get; set; }

    [ForeignKey("PrivacyID")]
    public Privacy Privacy { get; set; }

    public int? CommunityID { get; set; }

    [ForeignKey("CommunityID")]
    public Community? Community { get; set; }

    public ICollection<Comment> Comments { get; set; }
    public ICollection<Like> Likes { get; set; }
    public ICollection<Image> Images { get; set; }
    public ICollection<Video> Videos { get; set; }
}