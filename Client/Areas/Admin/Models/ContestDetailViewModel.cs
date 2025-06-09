using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Client.Areas.Admin.Models
{
    public class ContestDetailViewModel : ContestViewModel
    {
        // For create/edit form
        [Display(Name = "Contest Image")]
        public IFormFile? ImageFile { get; set; }
        
        public string? ImageUrl { get; set; }
        
        // For filtering/sorting entries
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; } = "newest";
        
        // For pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        
        // Computed properties
        public bool HasEnded => DateTime.Now > EndDate;
        public int TotalPages => (int)Math.Ceiling(TotalEntries / (double)PageSize);
        
        // Alias for ContestEntries for better readability in views
        public IEnumerable<ContestEntryViewModel> Entries 
        { 
            get => ContestEntries; 
            set => ContestEntries = value?.ToList() ?? new List<ContestEntryViewModel>(); 
        }
        
        // Helper to get paginated entries
        public IEnumerable<ContestEntryViewModel> PaginatedEntries => 
            Entries.Skip((CurrentPage - 1) * PageSize).Take(PageSize);
    }
}
