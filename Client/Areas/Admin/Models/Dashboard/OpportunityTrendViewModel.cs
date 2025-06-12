namespace Client.Areas.Admin.Models.Dashboard
{
    public class OpportunityTrendViewModel
    {
        public string Period { get; set; } = string.Empty;
        public int Opportunities { get; set; }
        public int Applications { get; set; }
    }
}
