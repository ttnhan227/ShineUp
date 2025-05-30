using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Client.Models;

public class EditPostViewModel
{
    public int PostID { get; set; }
    
    [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters")]
    [Display(Name = "Title")]
    public string? Title { get; set; }
    
    [Display(Name = "Content")]
    public string? Content { get; set; }
    
    [Display(Name = "Image")]
    public IFormFile? Image { get; set; }

    [Display(Name = "Video")]
    public IFormFile? Video { get; set; }
    
    public string? CurrentImageURL { get; set; }
    public string? CurrentVideoURL { get; set; }
    public string? MediaType { get; set; }
    
    [Display(Name = "Category")]
    public int? CategoryID { get; set; }
    
    [Display(Name = "Privacy")]
    public int? PrivacyID { get; set; }

    public List<IFormFile>? Images { get; set; }
    public List<IFormFile>? Videos { get; set; }
} 