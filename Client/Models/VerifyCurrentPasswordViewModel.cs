using System.ComponentModel.DataAnnotations;

namespace Client.Models
{
    public class VerifyCurrentPasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; }
    }
} 