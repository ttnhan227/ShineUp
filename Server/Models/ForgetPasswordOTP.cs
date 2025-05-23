using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models;

public class ForgetPasswordOTP
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Email { get; set; }

    [Required]
    public string OTPCode { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }

    // Optional: Add relation to User if you want to track which user requested the OTP
    [ForeignKey("User")]
    public int? UserID { get; set; }
    public User? User { get; set; }
}
