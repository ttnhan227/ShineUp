using System.ComponentModel.DataAnnotations;
using Server.Models;

namespace Server.DTOs;

public class UpdateCommunityDTO
{
    [Required(ErrorMessage = "Community Id is required")]
    public int Id { get; set; }

    [MaxLength(100, ErrorMessage = "Community name must not exceed 100 characters")]
    public string? Name { get; set; }

    [MaxLength(500, ErrorMessage = "Description must not exceed 500 characters")]
    public string? Description { get; set; }

    public IFormFile? CoverImage { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Invalid PrivacyID")]
    public int? PrivacyID { get; set; }
    
}