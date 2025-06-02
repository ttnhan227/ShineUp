using System.ComponentModel.DataAnnotations;

namespace Server.Models;

public class Privacy
{
    [Key] public int PrivacyID { get; set; }

    public string Name { get; set; }

    // Navigation properties
    public ICollection<Video> Videos { get; set; }
    public ICollection<Post> Posts { get; set; }
    public ICollection<Image> Images { get; set; }
    public ICollection<Community> Communities { get; set; }
}