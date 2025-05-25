using System.ComponentModel.DataAnnotations;

namespace Client.Models;

public class VerifyEmailViewModel
{
    [Required(ErrorMessage = "Verification code is required")]
    [Display(Name = "Verification Code")]
    public string OTP { get; set; }
} 