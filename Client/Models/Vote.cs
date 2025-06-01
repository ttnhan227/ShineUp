using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Client.Models;

public class Vote
{
    [Key]
    public int VoteID { get; set; } // PK
    [ForeignKey("ContestEntry")]
    public int EntryID { get; set; } // FK
    [ForeignKey("User")]
    public int UserID { get; set; } // FK
    public DateTime VotedAt { get; set; }

    // Navigation
    public ContestEntry ContestEntry { get; set; }
    public User User { get; set; }
}