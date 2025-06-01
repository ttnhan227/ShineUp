using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Interfaces;
using Server.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Server.Repositories;

public class OpportunityApplicationRepository : IOpportunityApplicationRepository
{
    private readonly DatabaseContext _context;

    public OpportunityApplicationRepository(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<OpportunityApplication> CreateAsync(OpportunityApplication application)
    {
        _context.OpportunityApplications.Add(application);
        await _context.SaveChangesAsync();
        return application;
    }

    public async Task<IEnumerable<OpportunityApplication>> GetByUserIdAsync(int userId)
    {
        return await _context.OpportunityApplications
            .Include(oa => oa.User)
            .Where(oa => oa.UserID == userId)
            .ToListAsync();
    }

    public async Task<OpportunityApplication?> GetByIdAsync(int applicationId)
    {
        return await _context.OpportunityApplications
            .Include(oa => oa.User)
            .FirstOrDefaultAsync(oa => oa.ApplicationID == applicationId);
    }

    public async Task<bool> DeleteAsync(int applicationId)
    {
        var application = await _context.OpportunityApplications.FindAsync(applicationId);
        if (application == null)
            return false;

        _context.OpportunityApplications.Remove(application);
        await _context.SaveChangesAsync();
        return true;
    }
}