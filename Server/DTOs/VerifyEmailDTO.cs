using System.ComponentModel.DataAnnotations;

namespace Server.DTOs;

public class VerifyEmailDTO
{
    [Required]
    public string OTP { get; set; }
} 