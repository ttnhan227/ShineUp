namespace Client.DTOs;

public class CommunityMemberDTO
{
    public int UserID { get; set; }
    public string FullName { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public DateTime JoinedAt { get; set; }
}