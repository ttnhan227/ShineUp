using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Client.Models;

public class TransferModeratorViewModel
{
    public int CommunityID { get; set; }
    
    public string CommunityName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Please select a new Moderator")]
    public int NewModeratorID { get; set; }
    
    public SelectList? Members { get; set; }
}
