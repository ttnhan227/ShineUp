using System;

namespace Client.Areas.Admin.Models.Dashboard
{
    public class RecentActivityViewModel
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty; // "User", "Opportunity", "Contest", "Application"
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserAvatar { get; set; } = string.Empty;
    }
}
