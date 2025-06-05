using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Client.Models;

public class TransferAdminViewModel
{
    public int CommunityID { get; set; }
    
    public string CommunityName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Please select a new admin")]
    public int NewAdminID { get; set; }
    
    public SelectList? Members { get; set; }
}
