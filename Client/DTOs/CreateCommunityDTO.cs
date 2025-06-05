using System.ComponentModel.DataAnnotations;

namespace Client.DTOs;

public class CreateCommunityDTO
{
    [Required, MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public int? PrivacyID { get; set; }

    public IFormFile? CoverImage { get; set; }
}
