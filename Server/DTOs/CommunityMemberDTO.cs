namespace Server.DTOs;

public class CommunityMemberDTO
{
    public int UserID { get; set; }
    public string FullName { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? ProfileImageUrl { get; set; }
    public string Role { get; set; } = null!;
    public DateTime JoinedAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
}