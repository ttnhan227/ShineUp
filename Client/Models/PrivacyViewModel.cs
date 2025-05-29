using System.ComponentModel.DataAnnotations;

namespace Client.Models;

public class PrivacyViewModel
{
    public int PrivacyID { get; set; }
    
    [Display(Name = "Privacy Level")]
    public string Name { get; set; }
} 