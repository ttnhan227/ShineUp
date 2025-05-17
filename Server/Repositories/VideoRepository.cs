using Server.Data;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class VideoRepository : IVideoRepository
{
    private readonly DatabaseContext _context;

    public VideoRepository(DatabaseContext context)
    {
        this._context = context;
    }
    
    public async Task<Video> GetVideoById(string id) => await _context.Videos.FindAsync(id);
 

    public  void Add(Video video)=> _context.Videos.Add(video);
 

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
 
}