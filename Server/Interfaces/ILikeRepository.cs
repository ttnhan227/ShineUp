using Server.DTOs;
using Server.Models;

namespace Server.Interfaces;

public interface ILikeRepository
{
    Task<Like?> GetByIdAsync(int id);
    Task<List<Like>> GetAllAsync();
    Task<List<Like>> GetLikesByPostIdAsync(int postId);
    Task<Like> AddLikeAsync(Like like);
    Task RemoveLikeAsync(Like like);
    Task UpdateAsync(Like like);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> UserLikedPostAsync(int userId, int postId);
}