using System.ComponentModel.DataAnnotations;

namespace Client.Areas.Admin.Models;

public class EditUserRoleViewModel
{
    public int UserID { get; set; }

    [Required]
    public int RoleID { get; set; }
}