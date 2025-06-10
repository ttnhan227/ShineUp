using System.ComponentModel.DataAnnotations;

namespace Client.Models;

public class PrivacyViewModel
{
    public int PrivacyID { get; set; }

    [Required(ErrorMessage = "Privacy name is required")]
    [StringLength(50, ErrorMessage = "Privacy name cannot exceed 50 characters")]
    [Display(Name = "Privacy Name")]
    public string Name { get; set; } = string.Empty;
}