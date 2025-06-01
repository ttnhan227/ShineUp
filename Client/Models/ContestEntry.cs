using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Client.Models;

public class ContestEntry
{
    [Key]
    public int EntryID { get; set; } // PK
    [ForeignKey("Contest")]
    public int ContestID { get; set; } // FK
    [ForeignKey("Video")]
    public Guid VideoID { get; set; } // FK
    [ForeignKey("User")]
    public int UserID { get; set; } // FK
    public string Caption { get; set; }
    public DateTime SubmittedAt { get; set; }

    // Navigation properties
    public Contest Contest { get; set; }
    public Video Video { get; set; }
    public User User { get; set; }

    public ICollection<Vote> Votes { get; set; }
}