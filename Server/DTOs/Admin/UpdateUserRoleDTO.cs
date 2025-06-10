using System.ComponentModel.DataAnnotations;

namespace Server.DTOs.Admin;

public class UpdateUserRoleDTO
{
    [Required]
    public int RoleID { get; set; }
}