using System.ComponentModel.DataAnnotations;

namespace Server.DTOs;

// DTO for creating a new post
public class CreatePostDto
{
    [Required]
    [StringLength(100)]
    public string Title { get; set; }

    [Required]
    public string Content { get; set; }

    public int? CategoryID { get; set; }
    public int? CommunityID { get; set; }
    public int? PrivacyID { get; set; }

    public List<MediaFileDTO> MediaFiles { get; set; } = new();
}

// DTO for updating an existing post
public class UpdatePostDto
{
    [StringLength(100)]
    public string? Title { get; set; }

    public string? Content { get; set; }

    public int? CategoryID { get; set; }

    public int? PrivacyID { get; set; }

    public List<MediaFileDTO> MediaFiles { get; set; } = new();
}

// DTO for returning post data
public class PostResponseDto
{
    public int PostID { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // User info
    public int UserID { get; set; }
    public string Username { get; set; }

    public string FullName { get; set; }

    // Category info
    public int? CategoryID { get; set; }
    public string? CategoryName { get; set; }

    // Community info
    public int? CommunityID { get; set; }
    public string? CommunityName { get; set; }

    // Privacy info
    public int? PrivacyID { get; set; }
    public string? PrivacyName { get; set; }

    // Social features
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public bool HasLiked { get; set; }

    public List<MediaFileDTO> MediaFiles { get; set; } = new();
}

// DTO for returning a list of posts
public class PostListResponseDto
{
    public int PostID { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }

    // User info
    public int UserID { get; set; }
    public string UserName { get; set; }
    public string FullName { get; set; }
    public string? ProfileImageURL { get; set; }

    // Category info
    public string? CategoryName { get; set; }

    // Community info
    public int? CommunityID { get; set; }
    public string? CommunityName { get; set; }

    // Social features
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public List<CommentDTO> Comments { get; set; } = new();
    public List<MediaFileDTO> MediaFiles { get; set; } = new();
}

public class MediaFileDTO
{
    public string Url { get; set; }
    public string Type { get; set; } // "image" or "video"
    public string PublicId { get; set; }
}