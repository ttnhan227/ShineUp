using System.ComponentModel.DataAnnotations;

namespace Server.DTOs;

public class CreateCommunityDTO
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }

    public IFormFile? CoverImage { get; set; }
    
    public int? PrivacyID { get; set; }
}