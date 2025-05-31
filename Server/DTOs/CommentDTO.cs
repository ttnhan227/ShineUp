namespace Server.DTOs;

public class CommentDTO
{
    public int CommentID { get; set; }
    public string? VideoID { get; set; }
    public int? PostID { get; set; }
    public int UserID { get; set; }
    public string Username { get; set; }
    public string FullName { get; set; }
    public string? ProfileImageURL { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCommentDTO
{
    public string Content { get; set; }
    public int? PostID { get; set; }
    public string? VideoID { get; set; }
}