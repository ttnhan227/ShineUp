namespace Server.DTOs;

public class CompleteGoogleProfileDTO
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string TalentArea { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}