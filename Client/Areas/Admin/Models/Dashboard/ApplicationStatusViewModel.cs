namespace Client.Areas.Admin.Models.Dashboard
{
    public class ApplicationStatusViewModel
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
}
