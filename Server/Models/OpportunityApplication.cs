namespace Server.Models;

public enum ApplicationStatus
{
    Pending,
    UnderReview,
    Shortlisted,
    Rejected,
    Accepted,
    Withdrawn
}

public class OpportunityApplication
{
    public int ApplicationID { get; set; }
    public int UserID { get; set; }
    public User User { get; set; } // Link to applicant
    public int TalentOpportunityID { get; set; }
    public TalentOpportunity TalentOpportunity { get; set; } // Link to opportunity
    public string CoverLetter { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
}