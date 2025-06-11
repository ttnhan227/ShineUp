namespace Client.Models;

public class ContestViewModel
{
    public int ContestID { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsClosed { get; set; }
    
    public string Status
    {
        get
        {
            if (IsClosed) return "Closed";
            if (DateTime.UtcNow < StartDate) return "Upcoming";
            if (DateTime.UtcNow > EndDate) return "Ended";
            return "Active";
        }
    }
    
    public bool IsActive => !IsClosed && DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate;
}