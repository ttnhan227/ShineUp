using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Client.Models;

public class CreatePostViewModel
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters")]
    [Display(Name = "Title")]
    public string Title { get; set; }
    
    [Required(ErrorMessage = "Content is required")]
    [Display(Name = "Content")]
    public string Content { get; set; }
    
    [Display(Name = "Image")]
    public IFormFile? Image { get; set; }
    
    [Display(Name = "Video")]
    public IFormFile? Video { get; set; }
    
    [Display(Name = "Category")]
    public int? CategoryID { get; set; }
    
    [Display(Name = "Privacy")]
    public int? PrivacyID { get; set; }

    public List<IFormFile>? Images { get; set; }
    public List<IFormFile>? Videos { get; set; }

    public IEnumerable<SelectListItem>? Categories { get; set; }
    public IEnumerable<SelectListItem>? PrivacyOptions { get; set; }
} 