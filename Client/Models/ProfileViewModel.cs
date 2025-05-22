using System.ComponentModel.DataAnnotations;

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
    }
}
