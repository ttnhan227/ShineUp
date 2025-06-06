using Server.DTOs.Admin;

namespace Server.Interfaces.Admin;

public interface IPostManagementRepository
{
    Task<IEnumerable<AdminPostDTO>> GetAllPostsAsync();
    Task<AdminPostDTO> GetPostByIdAsync(int postId);
    Task<bool> UpdatePostStatusAsync(int postId, bool isActive);
    Task<bool> DeletePostAsync(int postId);
}
