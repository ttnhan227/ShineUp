using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Client.Areas.Admin.Models
{
    public class OpportunityListViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Location { get; set; }
        public bool IsRemote { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public DateTime? ApplicationDeadline { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CategoryName { get; set; }
        public string TalentArea { get; set; }
        public string PostedBy { get; set; }
        public int ApplicationCount { get; set; }
    }

    public class OpportunityDetailViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot be longer than 200 characters")]
        public string Title { get; set; }
        
        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }
        
        [StringLength(100, ErrorMessage = "Location cannot be longer than 100 characters")]
        public string Location { get; set; }
        
        public bool IsRemote { get; set; }
        
        [Required(ErrorMessage = "Type is required")]
        public string Type { get; set; }
        
        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; }
        
        [Required(ErrorMessage = "Application deadline is required")]
        [DataType(DataType.DateTime)]
        public DateTime? ApplicationDeadline { get; set; }
        
        [Required(ErrorMessage = "Category is required")]
        public int? CategoryId { get; set; }
        
        [StringLength(100, ErrorMessage = "Talent area cannot be longer than 100 characters")]
        public string TalentArea { get; set; }
        
        public string CategoryName { get; set; }
        public string PostedBy { get; set; }
        public string PostedByEmail { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ApplicationCount { get; set; }
    }

    public class OpportunityCreateEditViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot be longer than 200 characters")]
        public string Title { get; set; }
        
        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }
        
        [StringLength(100, ErrorMessage = "Location cannot be longer than 100 characters")]
        public string Location { get; set; }
        
        public bool IsRemote { get; set; }
        
        [Required(ErrorMessage = "Type is required")]
        public string Type { get; set; }
        
        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; }
        
        [Required(ErrorMessage = "Application deadline is required")]
        [DataType(DataType.DateTime)]
        public DateTime? ApplicationDeadline { get; set; }
        
        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int? CategoryId { get; set; }
        
        [StringLength(100, ErrorMessage = "Talent area cannot be longer than 100 characters")]
        [Display(Name = "Talent Area")]
        public string TalentArea { get; set; }
        
        // For dropdown lists
        public List<CategoryViewModel> Categories { get; set; } = new();
        public List<string> OpportunityTypes { get; set; } = new();
        public List<string> OpportunityStatuses { get; set; } = new();
    }

    public class ApplicationListViewModel
    {
        public int ApplicationId { get; set; }
        public int OpportunityId { get; set; }
        public string OpportunityTitle { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string Status { get; set; }
        public DateTime AppliedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }

    public class ApplicationDetailViewModel : ApplicationListItemViewModel
    {
        public int UserId { get; set; }
        public string OpportunityDescription { get; set; }
        public string OpportunityType { get; set; }
        public string PortfolioUrl { get; set; }
        public string LinkedInUrl { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<string> AvailableStatuses { get; set; } = new();
        public List<ApplicationQuestionAnswer> AdditionalQuestions { get; set; } = new();
    }

    public class UpdateApplicationStatusViewModel
    {
        public int ApplicationId { get; set; }
        
        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; }
        
        [StringLength(1000, ErrorMessage = "Review notes cannot be longer than 1000 characters")]
        [Display(Name = "Review Notes")]
        public string ReviewNotes { get; set; }
        
        public List<string> StatusOptions { get; set; } = new();
    }

    public class OpportunityApplicationsViewModel
    {
        public int OpportunityId { get; set; }
        public string OpportunityTitle { get; set; }
        public List<ApplicationListItemViewModel> Applications { get; set; } = new();
        public Dictionary<string, int> StatusCounts { get; set; } = new();
        public List<string> ApplicationStatuses { get; set; } = new();
        public int TotalApplications => Applications?.Count ?? 0;
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public string CurrentFilter { get; set; }
    }

    public class ApplicationListItemViewModel
    {
        public int ApplicationId { get; set; } // Changed from Id to ApplicationId for consistency with server DTO
        public int OpportunityId { get; set; }
        public string OpportunityTitle { get; set; }
        public string ApplicantName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Status { get; set; }
        public DateTime AppliedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string CoverLetter { get; set; }
        public string ReviewNotes { get; set; }
    }



    public class ApplicationQuestionAnswer
    {
        public string Question { get; set; }
        public string Answer { get; set; }
    }
}

// Move StringExtensions to a top-level static class
namespace Client.Extensions
{
    public static class StringExtensions
    {
        public static string Truncate(this string value, int maxLength, string truncationSuffix = "...")
        {
            if (string.IsNullOrEmpty(value)) 
                return value;
                
            return value.Length <= maxLength ? 
                value : 
                value.Substring(0, maxLength) + truncationSuffix;
        }
    }
}
