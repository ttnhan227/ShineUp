namespace Server.DTOs;

public class OpportunityApplicationCreateDTO
{
    public int UserID { get; set; }
    public string OpportunityTitle { get; set; } = string.Empty;
    public string OpportunityDescription { get; set; } = string.Empty;
}

public class OpportunityApplicationDTO
{
    public int ApplicationID { get; set; }
    public int UserID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string OpportunityTitle { get; set; } = string.Empty;
    public string OpportunityDescription { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
}