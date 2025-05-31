using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models;

public class CommunityMember
{
    [Key, Column(Order = 0)]
    public int UserID { get; set; }
    [ForeignKey("UserID")]
    public User User { get; set; } = null!;

    [Key, Column(Order = 1)]
    public int CommunityID { get; set; }
    [ForeignKey("CommunityID")]
    public Community Community { get; set; } = null!;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastActiveAt { get; set; }

    public CommunityRole Role { get; set; } = CommunityRole.Member;
}
