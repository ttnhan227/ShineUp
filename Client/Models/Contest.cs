using System.ComponentModel.DataAnnotations;

namespace Client.Models;

public class Contest
{
    [Key]
    public int ContestID { get; set; } // PK
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Navigation property để hiển thị các bài dự thi (Entries)
    public ICollection<ContestEntry> ContestEntries { get; set; }
}