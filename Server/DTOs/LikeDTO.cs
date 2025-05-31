namespace Server.DTOs;

public class LikeDTO
{
    public int LikeID { get; set; }
    public string? VideoID { get; set; }
    public int? PostID { get; set; }
    public int UserID { get; set; }
    public string Username { get; set; }
    public string? ProfileImageURL { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateLikeDTO
{
    public int? PostID { get; set; }
    public string? VideoID { get; set; }
}