using System.ComponentModel.DataAnnotations;

namespace Client.Models;

public class VerifyEmailViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Verification code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 characters")]
    [Display(Name = "Verification Code")]
    public string OTP { get; set; } = string.Empty;
} 