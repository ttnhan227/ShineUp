using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models;

public class Share
{
    [Key]
    public int ShareID { get; set; }

    [ForeignKey("User")]
    public int UserID { get; set; }

    public User User { get; set; }

    [ForeignKey("Video")]
    public Guid VideoID { get; set; }

    public Video Video { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Platform { get; set; } // vd., "Twitter", "Facebook"
}