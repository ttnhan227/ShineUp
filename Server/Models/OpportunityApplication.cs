namespace Server.Models;

public class OpportunityApplication
{
    public int ApplicationID { get; set; }
    public int UserID { get; set; }
    public User User { get; set; } // Link to applicant
    public string OpportunityTitle { get; set; } = string.Empty; // e.g., "Freelance Art Gig"
    public string OpportunityDescription { get; set; } = string.Empty; // Brief details
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}