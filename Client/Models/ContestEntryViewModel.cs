using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Client.Models;

public class ContestEntryViewModel
{
    public int EntryID { get; set; }
    
    [Required]
    public int ContestID { get; set; }
    
    public int UserID { get; set; }
    
    [Display(Name = "Video File")]
    public IFormFile? VideoFile { get; set; }
    
    [Display(Name = "Image File")]
    public IFormFile? ImageFile { get; set; }
    
    public string? VideoID { get; set; }
    public string? ImageID { get; set; }
    
    public string? UserName { get; set; }
    public string? UserAvatar { get; set; }
    public DateTime SubmissionDate { get; set; }
    public string? MediaUrl { get; set; }
    
    [Required]
    [Display(Name = "Media Type")]
    public string? MediaType { get; set; } // "video" or "image"
    
    [Required]
    [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters.")]
    public string? Title { get; set; }
    
    [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters.")]
    public string? Description { get; set; }
    
    // Validation method to ensure only one file is uploaded
    public bool HasValidMedia()
    {
        if (MediaType == "video" && VideoFile != null && VideoFile.Length > 0)
            return true;
            
        if (MediaType == "image" && ImageFile != null && ImageFile.Length > 0)
            return true;
            
        return false;
    }
}