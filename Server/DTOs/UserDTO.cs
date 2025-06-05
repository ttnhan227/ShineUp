using Server.Models;

namespace Server.DTOs;

public class UserDTO
{
    public int UserID { get; set; }
    public string Username { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Bio { get; set; }
    public string ProfileImageURL { get; set; }
    public int RoleID { get; set; }
    public string TalentArea { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public bool Verified { get; set; }
    public DateTime? LastLoginTime { get; set; }
    public ProfilePrivacy ProfilePrivacy { get; set; }
    public int ProfileCompletionPercentage { get; set; }
    public bool IsGoogleAccount { get; set; }
    
    
}