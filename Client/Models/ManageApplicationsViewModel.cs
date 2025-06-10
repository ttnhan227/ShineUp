namespace Client.Models;

public class ManageApplicationsViewModel
{
    public int OpportunityId { get; set; }
    public string OpportunityTitle { get; set; } = string.Empty;
    public List<OpportunityApplicationViewModel> Applications { get; set; } = new();

    public bool HasApplications => Applications?.Any() == true;
}