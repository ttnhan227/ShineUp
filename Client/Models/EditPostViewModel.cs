using System.ComponentModel.DataAnnotations;

namespace Client.Models;

public class EditPostViewModel
{
    public int PostID { get; set; }
    
    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters")]
    [Display(Name = "Title")]
    public string Title { get; set; }
    
    [Required(ErrorMessage = "Content is required")]
    [Display(Name = "Content")]
    public string Content { get; set; }
    
    [Required(ErrorMessage = "Category is required")]
    [Display(Name = "Category")]
    public int CategoryID { get; set; }
    
    [Required(ErrorMessage = "Privacy setting is required")]
    [Display(Name = "Privacy")]
    public int PrivacyID { get; set; }

    public List<MediaFileViewModel> CurrentMediaFiles { get; set; } = new List<MediaFileViewModel>();
} 