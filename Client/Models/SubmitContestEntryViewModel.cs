using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Client.Models;

public class SubmitContestEntryViewModel
{
    public int ContestID { get; set; }
    public int UserID { get; set; }

    [Required(ErrorMessage = "Please select a media type")]
    [Display(Name = "Media Type")]
    public string MediaType { get; set; } = "video"; // Default to video

    [Display(Name = "Video File")]
    [DataType(DataType.Upload)]
    [MaxFileSize(50 * 1024 * 1024)] // 50MB
    [AllowedExtensions(new[] { ".mp4", ".webm", ".mov" })]
    public IFormFile? VideoFile { get; set; }

    [Display(Name = "Image File")]
    [DataType(DataType.Upload)]
    [MaxFileSize(10 * 1024 * 1024)] // 10MB
    [AllowedExtensions(new[] { ".jpg", ".jpeg", ".png", ".gif" })]
    public IFormFile? ImageFile { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters")]
    public string? Title { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
    public string? Description { get; set; }

    // Helper property to check if the model has valid media
    [NotMapped]
    public bool HasValidMedia =>
        MediaType == "video" && VideoFile != null && VideoFile.Length > 0 ||
        MediaType == "image" && ImageFile != null && ImageFile.Length > 0;
}

// Custom validation attribute for file size
public class MaxFileSizeAttribute : ValidationAttribute
{
    private readonly int _maxFileSize;

    public MaxFileSizeAttribute(int maxFileSize)
    {
        _maxFileSize = maxFileSize;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is IFormFile file)
        {
            if (file.Length > _maxFileSize)
            {
                return new ValidationResult(GetErrorMessage());
            }
        }

        return ValidationResult.Success;
    }

    public string GetErrorMessage()
    {
        return $"Maximum allowed file size is {_maxFileSize / (1024 * 1024)}MB.";
    }
}

// Custom validation attribute for allowed file extensions
public class AllowedExtensionsAttribute : ValidationAttribute
{
    private readonly string[] _extensions;

    public AllowedExtensionsAttribute(string[] extensions)
    {
        _extensions = extensions;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_extensions.Contains(extension))
            {
                return new ValidationResult(GetErrorMessage());
            }
        }

        return ValidationResult.Success;
    }

    public string GetErrorMessage()
    {
        return $"This file type is not allowed. Allowed types: {string.Join(", ", _extensions)}";
    }
}