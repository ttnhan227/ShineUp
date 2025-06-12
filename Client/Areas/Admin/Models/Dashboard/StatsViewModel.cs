namespace Client.Areas.Admin.Models.Dashboard
{
    public class StatsViewModel
    {
        // User Statistics
        public int TotalUsers { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int ActiveUsersToday { get; set; }
        public double UserGrowthPercentage { get; set; }
        
        // Opportunity Statistics
        public int TotalOpportunities { get; set; }
        public int OpenOpportunities { get; set; }
        public int ClosedOpportunities { get; set; }
        public int ApplicationsThisMonth { get; set; }
        public double ApplicationGrowthPercentage { get; set; }
        
        // Contest Statistics
        public int TotalContests { get; set; }
        public int ActiveContests { get; set; }
        public int TotalContestEntries { get; set; }
        public int ContestEntriesThisMonth { get; set; }
        
        // Content Statistics
        public int TotalPosts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalComments { get; set; }
        public int TotalLikes { get; set; }
    }
}
