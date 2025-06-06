using System;
using System.ComponentModel.DataAnnotations;

namespace Server.DTOs.Admin
{
    public class AdminApplicationDTO
    {
        public int ApplicationId { get; set; }
        public int OpportunityId { get; set; }
        public string OpportunityTitle { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? CoverLetter { get; set; }
        public string? ReviewNotes { get; set; }
    }

    public class UpdateApplicationStatusDTO
    {
        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; } = string.Empty;
        
        [StringLength(1000, ErrorMessage = "Review notes cannot be longer than 1000 characters")]
        public string? ReviewNotes { get; set; }
    }
}
