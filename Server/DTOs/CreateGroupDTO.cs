namespace Server.DTOs;

public class CreateGroupDTO
{
    public string GroupName { get; set; }
    public List<int> UserIds { get; set; } // List of user IDs to add to the group chat
}