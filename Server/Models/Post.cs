using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models;

public class Post
{
    [Key]
    public int PostID { get; set; }

    [ForeignKey("User")]
    public int UserID { get; set; }
    public User User { get; set; }

    public string Title { get; set; }
    public string Content { get; set; }
    public string? ImageURL { get; set; }  // Optional image for the post

    [ForeignKey("Category")]
    public int? CategoryID { get; set; }  // Optional category
    public Category? Category { get; set; }

    [ForeignKey("Privacy")]
    public int? PrivacyID { get; set; }
    public Privacy? Privacy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties for social features
    public ICollection<Comment> Comments { get; set; }
    public ICollection<Like> Likes { get; set; }
} 