using System.ComponentModel.DataAnnotations;

namespace Server.DTOs
{
    public class CompleteProfileDTO
    {
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }
        
        [Required]
        [StringLength(100)]
        public string TalentArea { get; set; }
        
        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }
        
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}