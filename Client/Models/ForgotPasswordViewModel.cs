using System.ComponentModel.DataAnnotations;

namespace Client.Models;

public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}

public class ResetPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string OTP { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; }
}
