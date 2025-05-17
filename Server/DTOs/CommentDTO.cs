namespace Client.DTOs;

public class CommentDTO
{
    public int CommentID { get; set; }
    public int VideoID { get; set; }
    public int UserID { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
}