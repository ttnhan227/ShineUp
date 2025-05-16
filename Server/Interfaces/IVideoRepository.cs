using Server.Models;

namespace Server.Interfaces;

public interface IVideoRepository
{
    
    // Seperate data access layer from business logic, for services to depend on repositories rather than DbContext
    Task<Video> GetVideoById(string id);
    void Add(Video video);
    Task SaveChangesAsync();
}