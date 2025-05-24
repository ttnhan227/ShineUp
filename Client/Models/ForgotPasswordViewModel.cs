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

    [Required(ErrorMessage = "New Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "New Password must be at least 8 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "New Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation new password do not match.")]
    public string ConfirmNewPassword { get; set; }
}
