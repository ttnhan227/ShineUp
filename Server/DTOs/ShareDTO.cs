namespace Server.DTOs;

public class ShareDTO
{
    public int ShareID { get; set; }
    public int UserID { get; set; }
    public Guid VideoID { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Platform { get; set; }
}