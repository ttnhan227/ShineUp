using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models;

public class Video
{
    [Key] public int VideoID { get; set; }

    [ForeignKey("User")] 
    public int UserID { get; set; }

    public User User { get; set; }

    [ForeignKey("Category")] 
    public int CategoryID { get; set; }

    public Category Category { get; set; }

    public string Title { get; set; }
    public string Description { get; set; }
    public string VideoURL { get; set; }
    public string ThumbnailURL { get; set; }

    [ForeignKey("Privacy")] 
    public int PrivacyID { get; set; }

    public Privacy Privacy { get; set; }

    public DateTime UploadDate { get; set; }

    // Navigation properties
    public ICollection<Comment> Comments { get; set; }
    public ICollection<Like> Likes { get; set; }
    public ICollection<ContestEntry> ContestEntries { get; set; }
}