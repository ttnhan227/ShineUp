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

        return MapToOpportunityDTO(opportunity);
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
            PostedByUser = opportunity.PostedByUser != null ? new UserDTO
            {
                UserID = opportunity.PostedByUser.UserID,
                Username = opportunity.PostedByUser.Username,
                FullName = opportunity.PostedByUser.FullName,
                Email = opportunity.PostedByUser.Email,
                Bio = opportunity.PostedByUser.Bio,
                ProfileImageURL = opportunity.PostedByUser.ProfileImageURL,
                RoleID = opportunity.PostedByUser.RoleID,
                TalentArea = opportunity.PostedByUser.TalentArea,
                CreatedAt = opportunity.PostedByUser.CreatedAt,
                IsActive = opportunity.PostedByUser.IsActive,
                Verified = opportunity.PostedByUser.Verified,
                LastLoginTime = opportunity.PostedByUser.LastLoginTime,
                ProfilePrivacy = opportunity.PostedByUser.ProfilePrivacy
            } : null,
            CategoryId = opportunity.CategoryId,
            Category = opportunity.Category != null ? new CategoryDTO 
            { 
                CategoryID = opportunity.Category.CategoryID,
                CategoryName = opportunity.Category.CategoryName,
                Description = opportunity.Category.Description 
            } : null,
            TalentArea = opportunity.TalentArea,
            ApplicationCount = opportunity.Applications?.Count ?? 0
        };
    }

    private OpportunityApplicationDTO MapToApplicationDTO(OpportunityApplication application)
    {
        var dto = new OpportunityApplicationDTO
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

        // Map user data if available
        if (application.User != null)
        {
            dto.User = new UserDTO
            {
                UserID = application.User.UserID,
                Username = application.User.Username,
                FullName = application.User.FullName,
                Email = application.User.Email,
                ProfileImageURL = application.User.ProfileImageURL,
                // Map other user properties as needed
                RoleID = application.User.RoleID,
                CreatedAt = application.User.CreatedAt,
                IsActive = application.User.IsActive,
                Verified = application.User.Verified
            };
        }

        return dto;
    }

    public async Task<IEnumerable<OpportunityDTO>> GetAllOpportunitiesAsync()
    {
        var opportunities = await _context.TalentOpportunities
            .Where(o => o.Status != OpportunityStatus.Closed && o.Status != OpportunityStatus.Cancelled)
            .Include(o => o.PostedByUser)
            .Include(o => o.Category)
            .Include(o => o.Applications)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return opportunities.Select(MapToOpportunityDTO);
    }

    public async Task<IEnumerable<OpportunityDTO>> GetOpportunitiesByTalentAreaAsync(string talentArea)
    {
        // Map legacy numeric values to their corresponding names for backward compatibility
        var talentAreas = new Dictionary<string, string>
        {
            { "1", "Performing Arts" },
            { "2", "Visual Arts" },
            { "3", "Media Production" },
            { "4", "Design & Creative" },
            { "5", "Writing & Translation" }
        };

        // If the talentArea is a number, try to get the corresponding name
        if (talentAreas.TryGetValue(talentArea, out var mappedTalentArea))
        {
            talentArea = mappedTalentArea;
        }

        var opportunities = await _context.TalentOpportunities
            .Where(o => o.TalentArea == talentArea && 
                      o.Status != OpportunityStatus.Closed && 
                      o.Status != OpportunityStatus.Cancelled)
            .Include(o => o.PostedByUser)
            .Include(o => o.Category)
            .Include(o => o.Applications)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return opportunities.Select(MapToOpportunityDTO);
    }
    
    public async Task<IEnumerable<OpportunityDTO>> GetOpportunitiesByCategoryAsync(int categoryId)
    {
        var opportunities = await _context.TalentOpportunities
            .Where(o => o.CategoryId == categoryId && 
                      o.Status != OpportunityStatus.Closed && 
                      o.Status != OpportunityStatus.Cancelled)
            .Include(o => o.PostedByUser)
            .Include(o => o.Category)
            .Include(o => o.Applications)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return opportunities.Select(MapToOpportunityDTO);
    }

    public async Task<IEnumerable<OpportunityDTO>> GetOpportunitiesByUserAsync(int userId)
    {
        var opportunities = await _context.TalentOpportunities
            .Where(o => o.PostedByUserId == userId)
            .Include(o => o.Category)
            .Include(o => o.Applications)
            .OrderByDescending(o => o.CreatedAt)
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
            ApplicationDeadline = opportunityDto.ApplicationDeadline.HasValue ? 
                opportunityDto.ApplicationDeadline.Value.Kind == DateTimeKind.Unspecified ?
                    DateTime.SpecifyKind(opportunityDto.ApplicationDeadline.Value, DateTimeKind.Utc).ToUniversalTime() :
                    opportunityDto.ApplicationDeadline.Value.ToUniversalTime() :
                null,
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
        {
            // Ensure the DateTime is in UTC before saving to PostgreSQL
            var deadline = opportunityDto.ApplicationDeadline.Value;
            if (deadline.Kind == DateTimeKind.Unspecified)
            {
                // If the DateTime is Unspecified, assume it's in the local timezone
                opportunity.ApplicationDeadline = DateTime.SpecifyKind(deadline, DateTimeKind.Utc).ToUniversalTime();
            }
            else
            {
                // If it's already Local or Utc, convert to UTC
                opportunity.ApplicationDeadline = deadline.ToUniversalTime();
            }
        }
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

    public async Task<IEnumerable<OpportunityApplicationDTO>> GetUserApplicationsAsync(int userId)
    {
        var applications = await _context.OpportunityApplications
            .Where(a => a.UserID == userId)
            .Include(a => a.TalentOpportunity)
                .ThenInclude(o => o.PostedByUser)
            .Include(a => a.TalentOpportunity)
                .ThenInclude(o => o.Category)
            .OrderByDescending(a => a.AppliedAt)
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
            .Include(a => a.TalentOpportunity)
                .ThenInclude(o => o.PostedByUser)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync();

        return applications.Select(MapToApplicationDTO);
    }

    public async Task<OpportunityApplicationDTO> UpdateApplicationStatusAsync(int applicationId, UpdateOpportunityApplicationDTO updateDto, int userId)
    {
        // Validate status
        if (string.IsNullOrEmpty(updateDto.Status) || !updateDto.IsValidStatus())
        {
            throw new ArgumentException("Invalid status value");
        }

        var application = await _context.OpportunityApplications
            .Include(a => a.TalentOpportunity)
                .ThenInclude(o => o.PostedByUser)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.ApplicationID == applicationId);

        if (application == null)
        {
            return null;
        }

        // Check if the current user is the opportunity owner
        if (application.TalentOpportunity?.PostedByUserId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to update this application");
        }

        // Validate status transition
        if (application.Status == ApplicationStatus.Withdrawn)
        {
            throw new InvalidOperationException("Cannot update status of a withdrawn application");
        }

        // Parse the status string to enum
        if (!Enum.TryParse<ApplicationStatus>(updateDto.Status, true, out var newStatus))
        {
            throw new ArgumentException($"Invalid status value: {updateDto.Status}");
        }

        // Update application
        application.Status = newStatus;
        application.ReviewNotes = updateDto.ReviewNotes;
        application.ReviewedAt = DateTime.UtcNow;

        _context.OpportunityApplications.Update(application);
        await _context.SaveChangesAsync();

        // Reload related data for the DTO
        await _context.Entry(application)
            .Reference(a => a.TalentOpportunity)
            .LoadAsync();
            
        await _context.Entry(application)
            .Reference(a => a.User)
            .LoadAsync();

        return MapToApplicationDTO(application);
    }

    public async Task<OpportunityApplicationDTO> ApplyForOpportunityAsync(CreateOpportunityApplicationDTO applicationDto, int userId)
    {
        // Check if the opportunity exists
        var opportunity = await _context.TalentOpportunities.FindAsync(applicationDto.TalentOpportunityID);
        if (opportunity == null)
        {
            throw new KeyNotFoundException("Opportunity not found");
        }

        // Check if the application deadline has passed
        if (opportunity.ApplicationDeadline.HasValue && opportunity.ApplicationDeadline < DateTime.UtcNow)
        {
            throw new InvalidOperationException("The application deadline for this opportunity has passed");
        }

        // Check if user has already applied
        var existingApplication = await _context.OpportunityApplications
            .FirstOrDefaultAsync(a => a.TalentOpportunityID == applicationDto.TalentOpportunityID && a.UserID == userId);

        if (existingApplication != null)
        {
            throw new InvalidOperationException("You have already applied to this opportunity");
        }

        // Create new application
        var application = new OpportunityApplication
        {
            TalentOpportunityID = applicationDto.TalentOpportunityID,
            UserID = userId,
            CoverLetter = applicationDto.CoverLetter,
            Status = ApplicationStatus.Pending,
            AppliedAt = DateTime.UtcNow
        };

        _context.OpportunityApplications.Add(application);
        await _context.SaveChangesAsync();

        // Load related data for the DTO
        await _context.Entry(application)
            .Reference(a => a.TalentOpportunity)
            .LoadAsync();

        return new OpportunityApplicationDTO
        {
            ApplicationID = application.ApplicationID,
            UserID = application.UserID,
            TalentOpportunityID = application.TalentOpportunityID,
            TalentOpportunityTitle = application.TalentOpportunity?.Title ?? string.Empty,
            TalentOpportunityDescription = application.TalentOpportunity?.Description ?? string.Empty,
            CoverLetter = application.CoverLetter,
            Status = application.Status,
            AppliedAt = application.AppliedAt,
            ReviewedAt = application.ReviewedAt,
            ReviewNotes = application.ReviewNotes
        };
    }
}