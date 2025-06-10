namespace Server.DTOs.Admin;

public class AdminOpportunityDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsRemote { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ApplicationDeadline { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CategoryName { get; set; }
    public string? TalentArea { get; set; }
    public string PostedBy { get; set; } = string.Empty;
    public int ApplicationCount { get; set; }
}

public class AdminOpportunityDetailDTO : AdminOpportunityDTO
{
    public string Description { get; set; } = string.Empty;
    public string? PostedByEmail { get; set; }
    public int PostedByUserId { get; set; }
}