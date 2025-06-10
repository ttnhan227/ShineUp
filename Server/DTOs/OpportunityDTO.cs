using Server.Models;

namespace Server.DTOs;

public class OpportunityDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsRemote { get; set; }
    public OpportunityType Type { get; set; }
    public OpportunityStatus Status { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int PostedByUserId { get; set; }
    public UserDTO? PostedByUser { get; set; }
    public int? CategoryId { get; set; }
    public CategoryDTO? Category { get; set; }
    public string? TalentArea { get; set; }
    public int ApplicationCount { get; set; }
}

public class CreateOpportunityDTO
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsRemote { get; set; }
    public OpportunityType Type { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public int? CategoryId { get; set; }
    public string? TalentArea { get; set; }
}

public class UpdateOpportunityDTO
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public bool? IsRemote { get; set; }
    public OpportunityType? Type { get; set; }
    public OpportunityStatus? Status { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public int? CategoryId { get; set; }
    public string? TalentArea { get; set; }
}