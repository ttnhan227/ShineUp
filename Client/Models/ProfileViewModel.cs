using System.ComponentModel.DataAnnotations;

// Added for IFormFile

namespace Client.Models;

public class ProfileViewModel
{
    [Required]
    public string? Username { get; set; }

    public string? FullName { get; set; }

    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    public string? Bio { get; set; }

    [Display(Name = "Profile Image URL")]
    public string? ProfileImageURL { get; set; }

    [Display(Name = "Talent Area")]
    public string? TalentArea { get; set; }

    [Display(Name = "Upload New Profile Image")]
    public IFormFile? ProfileImageFile { get; set; } // Added for image upload

    [Display(Name = "Profile Privacy")]
    public int ProfilePrivacy { get; set; }

    // Social Media Links
    [Url(ErrorMessage = "Please enter a valid URL")]
    [Display(Name = "Instagram URL")]
    public string? InstagramUrl { get; set; }

    [Url(ErrorMessage = "Please enter a valid URL")]
    [Display(Name = "YouTube URL")]
    public string? YouTubeUrl { get; set; }

    [Url(ErrorMessage = "Please enter a valid URL")]
    [Display(Name = "Twitter URL")]
    public string? TwitterUrl { get; set; }

    // Cover Photo
    [Display(Name = "Cover Photo URL")]
    public string? CoverPhotoUrl { get; set; }

    [Display(Name = "Upload New Cover Photo")]
    public IFormFile? CoverPhotoFile { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "New Password must be at least 8 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage =
            "New Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; }

    [Required(ErrorMessage = "Please confirm your new password")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; }
}

public class VerifyCurrentPasswordViewModel
{
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; }
}