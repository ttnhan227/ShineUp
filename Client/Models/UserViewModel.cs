namespace Client.Models;

public class UserViewModel
{
    public int UserID { get; set; }
    public string Username { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Bio { get; set; }
    public string ProfileImageURL { get; set; }
    public string TalentArea { get; set; }
    public int RoleID { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public bool Verified { get; set; }
    public DateTime? LastLoginTime { get; set; }
    public int ProfilePrivacy { get; set; }
    public int ProfileCompletionPercentage { get; set; }
    public bool IsGoogleAccount { get; set; }
    public bool IsPrivate { get; set; }

    // Social Media Links
    public string? InstagramUrl { get; set; }
    public string? YouTubeUrl { get; set; }
    public string? TwitterUrl { get; set; }

    // Profile Media
    public string? CoverPhotoUrl { get; set; }

    public List<PostDetailsViewModel> Posts { get; set; } = new();
}