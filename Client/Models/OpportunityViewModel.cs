using System;
using System.Text.Json.Serialization;
using Client.Models;
using System.ComponentModel.DataAnnotations;

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
        
        [JsonPropertyName("categoryName")]
        public string? CategoryName { get; set; } // For JSON deserialization
        
        [JsonIgnore]
        public CategoryViewModel? Category { get; set; } // For view usage
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
        [JsonPropertyName("applicationID")]
        public int ApplicationID { get; set; }
        
        [JsonPropertyName("userID")]
        public int UserID { get; set; }
        
        [JsonPropertyName("userName")]
        public string? UserName { get; set; } // For backward compatibility
        
        [JsonPropertyName("user")]
        public UserViewModel? User { get; set; }
        
        [JsonPropertyName("talentOpportunityID")]
        public int TalentOpportunityID { get; set; }
        
        [JsonPropertyName("talentOpportunityTitle")]
        public string TalentOpportunityTitle { get; set; } = string.Empty;
        
        [JsonPropertyName("talentOpportunityDescription")]
        public string TalentOpportunityDescription { get; set; } = string.Empty;
        
        [JsonPropertyName("talentArea")]
        public string? TalentArea { get; set; }
        
        [JsonPropertyName("location")]
        public string? Location { get; set; }
        
        [JsonPropertyName("isRemote")]
        public bool IsRemote { get; set; }
        
        [JsonPropertyName("coverLetter")]
        public string CoverLetter { get; set; } = string.Empty;
        
        [JsonPropertyName("status")]
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
        
        [JsonPropertyName("appliedAt")]
        public DateTime AppliedAt { get; set; }
        
        [JsonPropertyName("reviewedAt")]
        public DateTime? ReviewedAt { get; set; }
        
        [JsonPropertyName("reviewNotes")]
        public string? ReviewNotes { get; set; }
        
        // Navigation property for the related opportunity
        public OpportunityViewModel? Opportunity { get; set; }
        
        // Computed property for backward compatibility
        public string PostedByUserId => Opportunity?.PostedByUserId.ToString() ?? string.Empty;
    }

    public class CreateOpportunityApplicationViewModel
    {
        public int TalentOpportunityID { get; set; }
        public string CoverLetter { get; set; } = string.Empty;
    }

    public class UpdateOpportunityApplicationViewModel
    {
        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; } = string.Empty;
        
        public string? ReviewNotes { get; set; }
        
        // Helper method to convert string to ApplicationStatus
        public bool TryGetStatus(out ApplicationStatus status)
        {
            return Enum.TryParse(Status, true, out status);
        }
    }
}
