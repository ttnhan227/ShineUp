using System;
using System.ComponentModel.DataAnnotations;

namespace Client.Areas.Admin.Models
{
    public class ContestEntryViewModel
    {
        public int EntryID { get; set; }
        public int ContestID { get; set; }
        public string? VideoID { get; set; }
        public string? ImageID { get; set; }
        public int UserID { get; set; }
        
        [Display(Name = "User")]
        public string UserName { get; set; } = string.Empty;
        
        [Display(Name = "User Avatar")]
        public string? UserAvatar { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Display(Name = "Submitted")]
        public DateTime SubmissionDate { get; set; }
        
        [Display(Name = "Media URL")]
        public string? MediaUrl { get; set; }
        
        [Display(Name = "Media Type")]
        public string? MediaType { get; set; } // "image" or "video"
        
        [Display(Name = "Votes")]
        public int VoteCount { get; set; }
        
        [Display(Name = "Has Voted")]
        public bool HasVoted { get; set; }
        
        [Display(Name = "Is Winner")]
        public bool IsWinner { get; set; }
        
        // Helper properties for UI
        [Display(Name = "Time Ago")]
        public string TimeAgo 
        {
            get
            {
                var timeSpan = DateTime.Now - SubmissionDate;
                
                if (timeSpan.TotalSeconds < 60)
                    return "just now";
                    
                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes} minute{(timeSpan.TotalMinutes >= 2 ? "s" : "")} ago";
                    
                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours} hour{(timeSpan.TotalHours >= 2 ? "s" : "")} ago";
                    
                return $"{(int)timeSpan.TotalDays} day{(timeSpan.TotalDays >= 2 ? "s" : "")} ago";
            }
        }
        
        [Display(Name = "Media")]
        public string MediaHtml
        {
            get
            {
                if (string.IsNullOrEmpty(MediaUrl))
                    return string.Empty;
                    
                return MediaType?.ToLower() == "video" 
                    ? $"<video class='img-fluid' controls><source src='{MediaUrl}' type='video/mp4'>Your browser does not support the video tag.</video>"
                    : $"<img src='{MediaUrl}' class='img-fluid' alt='Entry media' />";
            }
        }
    }
}
