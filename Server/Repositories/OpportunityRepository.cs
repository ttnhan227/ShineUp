// OpportunityRepository.cs
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class OpportunityRepository : IOpportunityRepository
{
    private readonly DatabaseContext _context;

    public OpportunityRepository(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<OpportunityDTO> GetOpportunityByIdAsync(int id)
    {
        var opportunity = await _context.TalentOpportunities
            .Include(o => o.PostedByUser)
            .Include(o => o.Category)
            .Include(o => o.Applications)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (opportunity == null) return null;

        return new OpportunityDTO
        {
            Id = opportunity.Id,
            Title = opportunity.Title,
            Description = opportunity.Description,
            Location = opportunity.Location,
            IsRemote = opportunity.IsRemote,
            Type = opportunity.Type,
            Status = opportunity.Status,
            ApplicationDeadline = opportunity.ApplicationDeadline,
            CreatedAt = opportunity.CreatedAt,
            UpdatedAt = opportunity.UpdatedAt,
            PostedByUserId = opportunity.PostedByUserId,
            CategoryId = opportunity.CategoryId,
            TalentArea = opportunity.TalentArea,
            ApplicationCount = opportunity.Applications?.Count ?? 0
        };
    }

    private OpportunityDTO MapToOpportunityDTO(TalentOpportunity opportunity)
    {
        return new OpportunityDTO
        {
            Id = opportunity.Id,
            Title = opportunity.Title,
            Description = opportunity.Description,
            Location = opportunity.Location,
            IsRemote = opportunity.IsRemote,
            Type = opportunity.Type,
            Status = opportunity.Status,
            ApplicationDeadline = opportunity.ApplicationDeadline,
            CreatedAt = opportunity.CreatedAt,
            UpdatedAt = opportunity.UpdatedAt,
            PostedByUserId = opportunity.PostedByUserId,
            CategoryId = opportunity.CategoryId,
            TalentArea = opportunity.TalentArea,
            ApplicationCount = opportunity.Applications?.Count ?? 0
        };
    }

    private OpportunityApplicationDTO MapToApplicationDTO(OpportunityApplication application)
    {
        return new OpportunityApplicationDTO
        {
            ApplicationID = application.ApplicationID,
            UserID = application.UserID,
            TalentOpportunityID = application.TalentOpportunityID,
            TalentOpportunityTitle = application.TalentOpportunity?.Title ?? "Opportunity not found",
            TalentOpportunityDescription = application.TalentOpportunity?.Description ?? string.Empty,
            CoverLetter = application.CoverLetter,
            Status = application.Status,
            AppliedAt = application.AppliedAt,
            ReviewedAt = application.ReviewedAt,
            ReviewNotes = application.ReviewNotes
        };
    }

    public async Task<IEnumerable<OpportunityDTO>> GetAllOpportunitiesAsync()
    {
        var opportunities = await _context.TalentOpportunities
            .Include(o => o.PostedByUser)
            .Include(o => o.Category)
            .Include(o => o.Applications)
            .ToListAsync();

        return opportunities.Select(MapToOpportunityDTO);
    }

    public async Task<IEnumerable<OpportunityDTO>> GetOpportunitiesByTalentAreaAsync(string talentArea)
    {
        var opportunities = await _context.TalentOpportunities
            .Where(o => o.TalentArea == talentArea)
            .Include(o => o.PostedByUser)
            .Include(o => o.Category)
            .Include(o => o.Applications)
            .ToListAsync();

        return opportunities.Select(MapToOpportunityDTO);
    }

    public async Task<IEnumerable<OpportunityDTO>> GetOpportunitiesByUserAsync(int userId)
    {
        var opportunities = await _context.TalentOpportunities
            .Where(o => o.PostedByUserId == userId)
            .Include(o => o.Category)
            .Include(o => o.Applications)
            .ToListAsync();

        return opportunities.Select(MapToOpportunityDTO);
    }

    public async Task<OpportunityDTO> CreateOpportunityAsync(CreateOpportunityDTO opportunityDto, int userId)
    {
        var opportunity = new TalentOpportunity
        {
            Title = opportunityDto.Title,
            Description = opportunityDto.Description,
            Location = opportunityDto.Location,
            IsRemote = opportunityDto.IsRemote,
            Type = opportunityDto.Type,
            Status = OpportunityStatus.Open,
            ApplicationDeadline = opportunityDto.ApplicationDeadline,
            PostedByUserId = userId,
            CategoryId = opportunityDto.CategoryId,
            TalentArea = opportunityDto.TalentArea,
            CreatedAt = DateTime.UtcNow
        };

        _context.TalentOpportunities.Add(opportunity);
        await _context.SaveChangesAsync();

        return MapToOpportunityDTO(opportunity);
    }

    public async Task<OpportunityDTO> UpdateOpportunityAsync(int id, UpdateOpportunityDTO opportunityDto, int userId)
    {
        var opportunity = await _context.TalentOpportunities
            .FirstOrDefaultAsync(o => o.Id == id && o.PostedByUserId == userId);

        if (opportunity == null)
            return null;

        // Update only the fields that were provided
        if (opportunityDto.Title != null)
            opportunity.Title = opportunityDto.Title;
        if (opportunityDto.Description != null)
            opportunity.Description = opportunityDto.Description;
        if (opportunityDto.Location != null)
            opportunity.Location = opportunityDto.Location;
        if (opportunityDto.IsRemote.HasValue)
            opportunity.IsRemote = opportunityDto.IsRemote.Value;
        if (opportunityDto.Type.HasValue)
            opportunity.Type = opportunityDto.Type.Value;
        if (opportunityDto.Status.HasValue)
            opportunity.Status = opportunityDto.Status.Value;
        if (opportunityDto.ApplicationDeadline.HasValue)
            opportunity.ApplicationDeadline = opportunityDto.ApplicationDeadline;
        if (opportunityDto.CategoryId.HasValue)
            opportunity.CategoryId = opportunityDto.CategoryId;
        if (opportunityDto.TalentArea != null)
            opportunity.TalentArea = opportunityDto.TalentArea;

        opportunity.UpdatedAt = DateTime.UtcNow;

        _context.TalentOpportunities.Update(opportunity);
        await _context.SaveChangesAsync();

        return MapToOpportunityDTO(opportunity);
    }

    public async Task<bool> DeleteOpportunityAsync(int id, int userId)
    {
        var opportunity = await _context.TalentOpportunities
            .FirstOrDefaultAsync(o => o.Id == id && o.PostedByUserId == userId);

        if (opportunity == null)
            return false;

        _context.TalentOpportunities.Remove(opportunity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UserOwnsOpportunity(int opportunityId, int userId)
    {
        return await _context.TalentOpportunities
            .AnyAsync(o => o.Id == opportunityId && o.PostedByUserId == userId);
    }

    public async Task<OpportunityApplicationDTO> ApplyForOpportunityAsync(CreateOpportunityApplicationDTO applicationDto, int userId)
    {
        var opportunity = await _context.TalentOpportunities
            .FirstOrDefaultAsync(o => o.Id == applicationDto.TalentOpportunityID);

        if (opportunity == null)
            return null;

        var application = new OpportunityApplication
        {
            UserID = userId,
            TalentOpportunityID = applicationDto.TalentOpportunityID,
            CoverLetter = applicationDto.CoverLetter,
            Status = ApplicationStatus.Pending,
            AppliedAt = DateTime.UtcNow
        };

        _context.OpportunityApplications.Add(application);
        await _context.SaveChangesAsync();

        return MapToApplicationDTO(application);
    }

    public async Task<IEnumerable<OpportunityApplicationDTO>> GetUserApplicationsAsync(int userId)
    {
        var applications = await _context.OpportunityApplications
            .Where(a => a.UserID == userId)
            .Include(a => a.TalentOpportunity)
            .ToListAsync();

        return applications.Select(MapToApplicationDTO);
    }

    public async Task<IEnumerable<OpportunityApplicationDTO>> GetOpportunityApplicationsAsync(int opportunityId, int userId)
    {
        var ownsOpportunity = await UserOwnsOpportunity(opportunityId, userId);
        if (!ownsOpportunity)
            return null;

        var applications = await _context.OpportunityApplications
            .Where(a => a.TalentOpportunityID == opportunityId)
            .Include(a => a.User)
            .ToListAsync();

        return applications.Select(MapToApplicationDTO);
    }

    public async Task<OpportunityApplicationDTO?> UpdateApplicationStatusAsync(int applicationId, UpdateOpportunityApplicationDTO updateDto, int userId)
    {
        var application = await _context.OpportunityApplications
            .Include(a => a.TalentOpportunity)
            .FirstOrDefaultAsync(a => a.ApplicationID == applicationId);

        if (application == null || application.TalentOpportunity?.PostedByUserId != userId)
            return null;

        application.Status = updateDto.Status;
        application.ReviewNotes = updateDto.ReviewNotes;
        application.ReviewedAt = DateTime.UtcNow;

        _context.OpportunityApplications.Update(application);
        await _context.SaveChangesAsync();

        return MapToApplicationDTO(application);
    }
}