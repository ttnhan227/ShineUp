using Server.Models;

namespace Server.DTOs;

public class OpportunityApplicationDTO
{
    public int ApplicationID { get; set; }
    public int UserID { get; set; }
    public UserDTO? User { get; set; }
    public int TalentOpportunityID { get; set; }
    public string TalentOpportunityTitle { get; set; } = string.Empty;
    public string TalentOpportunityDescription { get; set; } = string.Empty;
    public string CoverLetter { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; }
    public DateTime AppliedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
}

public class CreateOpportunityApplicationDTO
{
    public int TalentOpportunityID { get; set; }
    public string CoverLetter { get; set; } = string.Empty;
}

public class UpdateOpportunityApplicationDTO
{
    public string Status { get; set; }
    public string? ReviewNotes { get; set; }

    public bool IsValidStatus()
    {
        if (string.IsNullOrEmpty(Status))
            return false;
            
        return Enum.TryParse<ApplicationStatus>(Status, true, out _);
    }
}
