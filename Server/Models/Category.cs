using System.ComponentModel.DataAnnotations;

namespace Server.Models;

public class Category
{
    [Key] public int CategoryID { get; set; }

    public string CategoryName { get; set; }
    public string Description { get; set; }

    // Navigation properties
    public ICollection<Video> Videos { get; set; }
    public ICollection<Post> Posts { get; set; }
    public ICollection<Image> Images { get; set; }
}