using System.ComponentModel.DataAnnotations;

namespace Server.DTOs;

public class ImageDTO
{
    public string ImageID { get; set; }
    public int UserID { get; set; }
    public int? CategoryID { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string ImageURL { get; set; }
    public int? PrivacyID { get; set; }
    public DateTime UploadDate { get; set; }
}

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