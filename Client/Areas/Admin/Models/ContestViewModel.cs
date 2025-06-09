using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Client.Areas.Admin.Models
{
    public class ContestViewModel
    {
        public int ContestID { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime StartDate { get; set; }
        
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime EndDate { get; set; }
        
        // Navigation property - this will be mapped from ContestEntries in the DTO
        public ICollection<ContestEntryViewModel> ContestEntries { get; set; } = new List<ContestEntryViewModel>();
        
        // Helper properties
        public int EntryCount => ContestEntries?.Count ?? 0;
        public int TotalEntries => EntryCount; // Alias for EntryCount for consistency
        public int TotalVotes { get; set; }
        
        // Method to calculate total votes
        public void CalculateTotalVotes()
        {
            TotalVotes = ContestEntries?.Sum(e => e.VoteCount) ?? 0;
        }
        public bool IsActive => DateTime.Now >= StartDate && DateTime.Now <= EndDate;
        public string Status => IsActive ? "Active" : (DateTime.Now < StartDate ? "Upcoming" : "Ended");
        
        [Display(Name = "Time Remaining")]
        public string TimeRemaining 
        {
            get
            {
                if (DateTime.Now < StartDate) 
                    return $"Starts in {(StartDate - DateTime.Now).Days} days";
                if (DateTime.Now > EndDate) 
                    return "Contest ended";
                return $"Ends in {(EndDate - DateTime.Now).Days} days";
            }
        }
    }
}
