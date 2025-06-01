namespace Client.Models;

public class OpportunityApplicationViewModel
{
    public int ApplicationID { get; set; }
    public int UserID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string OpportunityTitle { get; set; } = string.Empty;
    public string OpportunityDescription { get; set; } = string.Empty;
    public string AppliedAt { get; set; } = string.Empty; // Formatted date string
}