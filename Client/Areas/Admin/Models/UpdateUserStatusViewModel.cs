using System.ComponentModel.DataAnnotations;

namespace Client.Areas.Admin.Models;

public class UpdateUserStatusViewModel
{
    [Required]
    public string Field { get; set; }

    [Required]
    public bool Value { get; set; }
}