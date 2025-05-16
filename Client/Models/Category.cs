using System.ComponentModel.DataAnnotations;

namespace Client.Models;

public class Category
{
    [Key] public int CategoryID { get; set; }

    public string CategoryName { get; set; }
    public string Description { get; set; }

    // Navigation property
    public ICollection<Video> Videos { get; set; }
}