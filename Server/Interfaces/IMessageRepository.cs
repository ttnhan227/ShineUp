using Server.Models;

namespace Server.Interfaces;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(int id);
    Task<List<Message>> GetAllAsync();
    Task AddAsync(Message message);
    Task UpdateAsync(Message message);
    Task DeleteAsync(int id);
}