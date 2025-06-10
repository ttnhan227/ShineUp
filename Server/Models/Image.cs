using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models;

public class Image
{
    [Key]
    public string ImageID { get; set; } // Using publicId from Cloudinary as primary key

    [ForeignKey("Post")]
    public int? PostID { get; set; }

    public Post? Post { get; set; }

    [ForeignKey("User")]
    public int UserID { get; set; }

    public User User { get; set; }

    [ForeignKey("Category")]
    public int? CategoryID { get; set; } // Optional category

    public Category? Category { get; set; }

    [StringLength(100)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    [Required]
    public string ImageURL { get; set; }

    [Required]
    public string CloudPublicId { get; set; }

    [ForeignKey("Privacy")]
    public int? PrivacyID { get; set; }

    public Privacy? Privacy { get; set; }

    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
}