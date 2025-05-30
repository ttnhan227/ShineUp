using System.ComponentModel.DataAnnotations;

namespace Client.Models;

public class PostDetailsViewModel
{
    public int PostID { get; set; }
    
    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters")]
    public string Title { get; set; }
    
    [Required(ErrorMessage = "Content is required")]
    public string Content { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // User info
    public int UserID { get; set; }
    public string Username { get; set; }
    public string FullName { get; set; }
    public string? ProfileImageURL { get; set; }
    
    // Category info
    public int? CategoryID { get; set; }
    public string? CategoryName { get; set; }
    
    // Privacy info
    public int? PrivacyID { get; set; }
    public string? PrivacyName { get; set; }
    
    // Media files
    public List<MediaFileViewModel> MediaFiles { get; set; } = new List<MediaFileViewModel>();
    
    // Social features
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
}