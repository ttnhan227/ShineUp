using System.ComponentModel.DataAnnotations;

namespace Client.Models;

public class Role
{
    [Key] public int RoleID { get; set; }

    public string Name { get; set; }

    // Navigation property
    public ICollection<User> Users { get; set; }
}