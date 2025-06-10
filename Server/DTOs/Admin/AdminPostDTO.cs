namespace Server.DTOs.Admin;

public class AdminPostDTO
{
    public int PostID { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int UserID { get; set; }
    public string UserName { get; set; }
    public string UserEmail { get; set; }
    public int? CategoryID { get; set; }
    public string CategoryName { get; set; }
    public int? PrivacyID { get; set; }
    public string PrivacyName { get; set; }
    public int? CommunityID { get; set; }
    public string CommunityName { get; set; }
    public int CommentCount { get; set; }
    public int LikeCount { get; set; }
    public bool IsActive { get; set; }
}

public class UpdatePostStatusDTO
{
    public int PostID { get; set; }
    public bool IsActive { get; set; }
}