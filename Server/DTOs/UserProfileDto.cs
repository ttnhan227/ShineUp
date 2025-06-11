using Server.Models;
using System.ComponentModel.DataAnnotations;

// Added for IFormFile

namespace Server.DTOs;

public class UpdateProfileDto
{
    public string? Username { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? TalentArea { get; set; }
    public IFormFile? ProfileImageFile { get; set; } // For profile image upload
    public IFormFile? CoverPhotoFile { get; set; } // For cover photo upload
    public ProfilePrivacy? ProfilePrivacy { get; set; }

    // Social Media Links
    [Url]
    public string? InstagramUrl { get; set; }

    [Url]
    public string? YouTubeUrl { get; set; }

    [Url]
    public string? TwitterUrl { get; set; }

    // Cover Photo
    public string? CoverPhotoUrl { get; set; }
}

public class ChangePasswordDTO
{
    [Required]
    public string CurrentPassword { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; }
}