using System.Collections.Generic;
using Client.Areas.Admin.Models;

namespace Client.Areas.Admin.Models.Dashboard
{
    public class IndexViewModel
    {
        public StatsViewModel Stats { get; set; } = new();
        public List<RecentActivityViewModel> RecentActivities { get; set; } = new();
        public List<ApplicationStatusViewModel> ApplicationStatuses { get; set; } = new();
        public List<OpportunityTrendViewModel> OpportunityTrends { get; set; } = new();
        public List<RecentOpportunityViewModel> RecentOpportunities { get; set; } = new();
        public List<ContestViewModel> ActiveContests { get; set; } = new();
    }
}
