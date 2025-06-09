using System.ComponentModel.DataAnnotations;

namespace Server.DTOs;

public class ContestEntryDTO
{
    public int EntryID { get; set; }
    public int ContestID { get; set; }
    public string? VideoID { get; set; }
    public string? ImageID { get; set; }
    public int UserID { get; set; }
    public string UserName { get; set; }
    public string? UserAvatar { get; set; }
    public DateTime SubmissionDate { get; set; }
    public string MediaUrl { get; set; }
    public string MediaType { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    // Voting properties
    public int VoteCount { get; set; }
    public bool HasVoted { get; set; }
    
    // Winner status
    public bool IsWinner { get; set; }
}