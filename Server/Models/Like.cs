using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models;

public class Like
{
    [Key] public int LikeID { get; set; }

    [ForeignKey("Video")] public string VideoID { get; set; }

    public Video? Video { get; set; }

    [ForeignKey("User")] public int UserID { get; set; }

    public User User { get; set; }

    public DateTime CreatedAt { get; set; }
}