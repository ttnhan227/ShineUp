namespace Server.DTOs;

public class ContestEntryDTO
{
    public int EntryID { get; set; }
    public int ContestID { get; set; }
    public string VideoID { get; set; }
    public int UserID { get; set; }
    public DateTime SubmissionDate { get; set; }
}