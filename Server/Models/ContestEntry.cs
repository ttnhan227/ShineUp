using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models;

public class ContestEntry
{
    [Key]
    public int EntryID { get; set; }

    [ForeignKey("Contest")]
    public int ContestID { get; set; }

    public Contest Contest { get; set; }

    [ForeignKey("Video")] public Guid VideoID { get; set; }


    public Video? Video { get; set; }

    [ForeignKey("User")]
    public int UserID { get; set; }

    public User User { get; set; }

    public DateTime SubmissionDate { get; set; }
}