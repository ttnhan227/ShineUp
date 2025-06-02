using System.ComponentModel.DataAnnotations;

namespace Server.DTOs;

public class UpdateCommunityDTO
{
    [Required]
    public int Id { get; set; }

    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public IFormFile? CoverImage { get; set; }
}