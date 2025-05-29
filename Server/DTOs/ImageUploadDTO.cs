using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Server.DTOs;

public class ImageUploadDTO
{
    [Required(ErrorMessage = "Image file is required.")]
    public IFormFile ImageFile { get; set; }

    [StringLength(100)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    public int? CategoryID { get; set; }

    public int? PrivacyID { get; set; }
} 