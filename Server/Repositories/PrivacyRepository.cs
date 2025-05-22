using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class PrivacyRepository : IPrivacyRepository
{
    private readonly DatabaseContext _context;

    public PrivacyRepository(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Privacy?> GetByIdAsync(int id)
    {
        return await _context.Privacies.FindAsync(id);
    }

    public async Task<List<Privacy>> GetAllAsync()
    {
        return await _context.Privacies.ToListAsync();
    }

    public async Task AddAsync(Privacy privacy)
    {
        _context.Privacies.Add(privacy);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Privacy privacy)
    {
        _context.Entry(privacy).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var privacyToDelete = await _context.Privacies.FindAsync(id);
        if (privacyToDelete != null)
        {
            _context.Privacies.Remove(privacyToDelete);
            await _context.SaveChangesAsync();
        }
    }
}