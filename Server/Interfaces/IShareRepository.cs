using Server.Models;

namespace Server.Interfaces;

public interface IShareRepository
{
    Task AddAsync(Share share);
    Task<bool> HasSharedAsync(Guid videoId, int userId);
    Task<List<Share>> GetByVideoIdAsync(Guid videoId);
}