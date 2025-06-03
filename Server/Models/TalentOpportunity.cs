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
    Draft,
    Open,
    InProgress,
    Closed,
    Cancelled
}

public class TalentOpportunity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Requirements { get; set; }
    public string? Responsibilities { get; set; }
    public string? Benefits { get; set; }
    public string? Location { get; set; }
    public bool IsRemote { get; set; } = false;
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string? SalaryCurrency { get; set; }
    public string? SalaryPeriod { get; set; } // e.g., "per hour", "per month", "per project"
    
    public OpportunityType Type { get; set; }
    public OpportunityStatus Status { get; set; } = OpportunityStatus.Draft;
    
    // Application details
    public DateTime? ApplicationDeadline { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? MaxApplicants { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int ViewCount { get; set; } = 0;
    public int ApplicationCount { get; set; } = 0;
    
    // Relationships
    public int PostedByUserId { get; set; }
    public User PostedByUser { get; set; } // Talent scout, recruiter, or agency
    public string? CompanyName { get; set; } // Optional company name as text
    
    // Category relationship
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    
    // Talent area for this opportunity (matches User.TalentArea)
    public string? TalentArea { get; set; }
    
    // Applications
    public ICollection<OpportunityApplication> Applications { get; set; } = new List<OpportunityApplication>();
}


