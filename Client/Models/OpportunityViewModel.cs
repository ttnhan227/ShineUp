using System;
using Client.Models;

namespace Client.Models
{
    public class OpportunityViewModel
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
        public string? PostedByUserName { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? TalentArea { get; set; }
        public int ApplicationCount { get; set; }
        public bool HasApplied { get; set; }
    }

    public class CreateOpportunityViewModel
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

    public class UpdateOpportunityViewModel
    {
        public int Id { get; set; }
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

    public class OpportunityApplicationViewModel
    {
        public int ApplicationID { get; set; }
        public int UserID { get; set; }
        public string? UserName { get; set; }
        public int TalentOpportunityID { get; set; }
        public string TalentOpportunityTitle { get; set; } = string.Empty;
        public string CoverLetter { get; set; } = string.Empty;
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
        public DateTime AppliedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
    }

    public class CreateOpportunityApplicationViewModel
    {
        public int TalentOpportunityID { get; set; }
        public string CoverLetter { get; set; } = string.Empty;
    }

    public class UpdateOpportunityApplicationViewModel
    {
        public ApplicationStatus Status { get; set; }
        public string? ReviewNotes { get; set; }
    }
}
