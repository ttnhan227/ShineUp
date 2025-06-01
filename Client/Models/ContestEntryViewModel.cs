namespace Client.Models;

public class ContestEntryViewModel
{
    public int EntryID { get; set; }
    public int ContestID { get; set; }
    public int UserID { get; set; }
    public string VideoTitle { get; set; }
    public string VideoURL { get; set; }
    public string UserName { get; set; }
    public DateTime SubmissionDate { get; set; }
}