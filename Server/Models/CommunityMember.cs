using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models;

public class CommunityMember
{
    public int UserID { get; set; }
    public User User { get; set; } = null!;

    public int CommunityID { get; set; }
    public Community Community { get; set; } = null!;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastActiveAt { get; set; }

    public CommunityRole Role { get; set; } = CommunityRole.Member;
}

