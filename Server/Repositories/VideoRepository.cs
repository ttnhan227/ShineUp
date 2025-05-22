using Server.Data;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class VideoRepository : IVideoRepository
{
    private readonly DatabaseContext _context;

    public VideoRepository(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Video> GetVideoById(string id)
    {
        return await _context.Videos.FindAsync(id);
    }


    public void Add(Video video)
    {
        _context.Videos.Add(video);
    }


    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}