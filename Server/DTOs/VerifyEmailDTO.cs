using System.ComponentModel.DataAnnotations;

namespace Server.DTOs;

public class VerifyEmailDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string OTP { get; set; }
} 