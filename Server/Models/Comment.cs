using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models;

public class Comment
{
    [Key] public int CommentID { get; set; }

    [ForeignKey("Video")] public string? VideoID { get; set; }

    public Video? Video { get; set; }

    [ForeignKey("Post")]
    public int? PostID { get; set; }

    public Post? Post { get; set; }

    [ForeignKey("User")] public int UserID { get; set; }

    public User User { get; set; }

    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
}