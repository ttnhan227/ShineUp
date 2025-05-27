using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // Added for IFormFile

namespace Client.Models
{
    public class ProfileViewModel
    {
        [Required]
        public string? Username { get; set; }
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
    }
}
