namespace Server.Models;

public enum OpportunityType
{
    Job,
    Gig,
    Freelance,
    Audition,
    CastingCall,
    Collaboration,
    Internship,
    Volunteer
}

public enum OpportunityStatus
{
    Draft = 0,
    Open = 1,
    InProgress = 2,
    Closed = 3,
    Cancelled = 4
}

public class TalentOpportunity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsRemote { get; set; } = false;
    public OpportunityType Type { get; set; }
    public OpportunityStatus Status { get; set; } = OpportunityStatus.Draft;
    public DateTime? ApplicationDeadline { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int PostedByUserId { get; set; }
    public User PostedByUser { get; set; }
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    
    // Talent area for this opportunity (matches User.TalentArea)
    public string? TalentArea { get; set; }
    
    // Applications
    public ICollection<OpportunityApplication> Applications { get; set; } = new List<OpportunityApplication>();
}


