using Microsoft.AspNetCore.Http; // Added for IFormFile
using Server.Models;

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
        public ProfilePrivacy? ProfilePrivacy { get; set; }
    }

}
