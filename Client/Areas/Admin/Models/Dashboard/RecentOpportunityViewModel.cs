using System;

namespace Client.Areas.Admin.Models.Dashboard
{
    public class RecentOpportunityViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsRemote { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ApplicationDeadline { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CategoryName { get; set; } = "Uncategorized";
        public string TalentArea { get; set; } = string.Empty;
        public string PostedBy { get; set; } = string.Empty;
        public int ApplicationCount { get; set; }
    }
}
