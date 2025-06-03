using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Client.Models
{
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

    public enum ApplicationStatus
    {
        Pending,
        UnderReview,
        Shortlisted,
        Rejected,
        Accepted,
        Withdrawn
    }

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
        public string? PostedByUserImage { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? TalentArea { get; set; }
        public int ApplicationCount { get; set; }
    }

    public class CreateOpportunityViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot be longer than 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        public string? Location { get; set; }
        
        public bool IsRemote { get; set; }
        
        [Required(ErrorMessage = "Opportunity type is required")]
        public OpportunityType Type { get; set; }
        
        public DateTime? ApplicationDeadline { get; set; }
        
        [Display(Name = "Category")]
        public int? CategoryId { get; set; }
        
        [Display(Name = "Talent Area")]
        public string? TalentArea { get; set; }
    }

    public class UpdateOpportunityViewModel
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot be longer than 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        public string? Location { get; set; }
        
        public bool IsRemote { get; set; }
        
        [Required(ErrorMessage = "Opportunity type is required")]
        public OpportunityType Type { get; set; }
        
        public DateTime? ApplicationDeadline { get; set; }
        
        [Display(Name = "Status")]
        public OpportunityStatus Status { get; set; }
        
        [Display(Name = "Category")]
        public int? CategoryId { get; set; }
        
        [Display(Name = "Talent Area")]
        public string? TalentArea { get; set; }
    }

    public class OpportunityApplicationViewModel
    {
        public int ApplicationID { get; set; }
        public int UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserImage { get; set; } = string.Empty;
        public string OpportunityTitle { get; set; } = string.Empty;
        public string OpportunityDescription { get; set; } = string.Empty;
        public int? TalentOpportunityID { get; set; }
        public string CoverLetter { get; set; } = string.Empty;
        public ApplicationStatus Status { get; set; }
        public DateTime AppliedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
    }

    public class CreateOpportunityApplicationViewModel
    {
        [Required]
        public int TalentOpportunityID { get; set; }
        
        [Required(ErrorMessage = "Cover letter is required")]
        [StringLength(2000, ErrorMessage = "Cover letter cannot be longer than 2000 characters")]
        public string CoverLetter { get; set; } = string.Empty;
        
        // These fields will be populated from the opportunity
        public string? OpportunityTitle { get; set; }
        public string? OpportunityDescription { get; set; }
    }

    public class UpdateOpportunityApplicationViewModel
    {
        [Required]
        public int ApplicationID { get; set; }
        
        [Required]
        public ApplicationStatus Status { get; set; }
        
        [StringLength(1000, ErrorMessage = "Review notes cannot be longer than 1000 characters")]
        public string? ReviewNotes { get; set; }
    }
}
