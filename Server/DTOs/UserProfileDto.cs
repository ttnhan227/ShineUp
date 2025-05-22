using Microsoft.AspNetCore.Http; // Added for IFormFile

namespace Server.DTOs
{
    public class UpdateProfileDto
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? TalentArea { get; set; }
        public IFormFile? ProfileImageFile { get; set; } // Added for image upload
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
