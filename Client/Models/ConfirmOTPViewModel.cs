using System.ComponentModel.DataAnnotations;

namespace Client.Models
{
    public class ConfirmOTPViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "The code must be 6 digits")]
        public string OTP { get; set; }
    }
}
