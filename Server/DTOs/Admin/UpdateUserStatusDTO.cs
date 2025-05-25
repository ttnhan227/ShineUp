using System.ComponentModel.DataAnnotations;

namespace Server.DTOs.Admin;

public class UpdateUserStatusDTO
{
    [Required]
    public string Field { get; set; } // "isActive" or "verified"
    
    [Required]
    public bool Value { get; set; }
} 