namespace Server.DTOs;

public class LikeDTO
{
    public int LikeID { get; set; }
    public string VideoID { get; set; }
    public int UserID { get; set; }
    public DateTime CreatedAt { get; set; }
}