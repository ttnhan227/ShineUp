namespace Client.DTOs;

public class LikeDTO
{
    public int LikeID { get; set; }
    public int VideoID { get; set; }
    public int UserID { get; set; }
    public DateTime CreatedAt { get; set; }
}