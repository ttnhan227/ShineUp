using System.ComponentModel.DataAnnotations;

namespace Client.Models.Admin
{
    public class EditUserRoleViewModel
    {
        public int UserID { get; set; }
        [Required]
        public int RoleID { get; set; }
    }
}
