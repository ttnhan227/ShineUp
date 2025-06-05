using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Server.Data;
using Server.DTOs.Admin;
using Server.Interfaces.Admin;
using Server.Models;

namespace Server.Repositories.Admin
{
    public class OpportunityManagementRepository : IOpportunityManagementRepository
    {
        private readonly DatabaseContext _context;
        private readonly ILogger<OpportunityManagementRepository> _logger;

        public OpportunityManagementRepository(DatabaseContext context, ILogger<OpportunityManagementRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<AdminOpportunityDTO>> GetAllOpportunitiesAsync()
        {
            try
            {
                return await _context.TalentOpportunities
                    .Include(o => o.Category)
                    .Include(o => o.PostedByUser)
                    .Select(o => new AdminOpportunityDTO
                    {
                        Id = o.Id,
                        Title = o.Title,
                        Location = o.Location,
                        IsRemote = o.IsRemote,
                        Type = o.Type.ToString(),
                        Status = o.Status.ToString(),
                        ApplicationDeadline = o.ApplicationDeadline,
                        CreatedAt = o.CreatedAt,
                        UpdatedAt = o.UpdatedAt,
                        CategoryName = o.Category != null ? o.Category.CategoryName : null,
                        TalentArea = o.TalentArea,
                        PostedBy = o.PostedByUser.Username,
                        ApplicationCount = o.Applications != null ? o.Applications.Count : 0
                    })
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all opportunities");
                throw;
            }
        }

        public async Task<AdminOpportunityDetailDTO> GetOpportunityByIdAsync(int id)
        {
            try
            {
                var opportunity = await _context.TalentOpportunities
                    .Include(o => o.Category)
                    .Include(o => o.PostedByUser)
                    .Where(o => o.Id == id)
                    .Select(o => new AdminOpportunityDetailDTO
                    {
                        Id = o.Id,
                        Title = o.Title,
                        Description = o.Description,
                        Location = o.Location,
                        IsRemote = o.IsRemote,
                        Type = o.Type.ToString(),
                        Status = o.Status.ToString(),
                        ApplicationDeadline = o.ApplicationDeadline,
                        CreatedAt = o.CreatedAt,
                        UpdatedAt = o.UpdatedAt,
                        CategoryName = o.Category != null ? o.Category.CategoryName : null,
                        TalentArea = o.TalentArea,
                        PostedBy = o.PostedByUser.Username,
                        PostedByEmail = o.PostedByUser.Email,
                        PostedByUserId = o.PostedByUserId,
                        ApplicationCount = o.Applications != null ? o.Applications.Count : 0
                    })
                    .FirstOrDefaultAsync();

                return opportunity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting opportunity with ID {id}");
                throw;
            }
        }

        public async Task<bool> CreateOpportunityAsync(CreateUpdateOpportunityDTO opportunityDto)
        {
            try
            {
                if (!Enum.TryParse<OpportunityType>(opportunityDto.Type, out var type) ||
                    !Enum.TryParse<OpportunityStatus>(opportunityDto.Status, out var status))
                {
                    return false;
                }

                var opportunity = new TalentOpportunity
                {
                    Title = opportunityDto.Title,
                    Description = opportunityDto.Description,
                    Location = opportunityDto.Location,
                    IsRemote = opportunityDto.IsRemote,
                    Type = type,
                    Status = status,
                    ApplicationDeadline = opportunityDto.ApplicationDeadline,
                    CategoryId = opportunityDto.CategoryId,
                    TalentArea = opportunityDto.TalentArea,
                    PostedByUserId = 1, // TODO: Get from current user
                    CreatedAt = DateTime.UtcNow
                };

                _context.TalentOpportunities.Add(opportunity);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating opportunity");
                return false;
            }
        }

        public async Task<bool> UpdateOpportunityAsync(int id, CreateUpdateOpportunityDTO opportunityDto)
        {
            try
            {
                if (!Enum.TryParse<OpportunityType>(opportunityDto.Type, out var type) ||
                    !Enum.TryParse<OpportunityStatus>(opportunityDto.Status, out var status))
                {
                    return false;
                }

                var opportunity = await _context.TalentOpportunities.FindAsync(id);
                if (opportunity == null)
                    return false;

                opportunity.Title = opportunityDto.Title;
                opportunity.Description = opportunityDto.Description;
                opportunity.Location = opportunityDto.Location;
                opportunity.IsRemote = opportunityDto.IsRemote;
                opportunity.Type = type;
                opportunity.Status = status;
                opportunity.ApplicationDeadline = opportunityDto.ApplicationDeadline;
                opportunity.CategoryId = opportunityDto.CategoryId;
                opportunity.TalentArea = opportunityDto.TalentArea;
                opportunity.UpdatedAt = DateTime.UtcNow;

                _context.TalentOpportunities.Update(opportunity);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating opportunity with ID {id}");
                return false;
            }
        }

        public async Task<bool> DeleteOpportunityAsync(int id)
        {
            try
            {
                var opportunity = await _context.TalentOpportunities.FindAsync(id);
                if (opportunity == null)
                    return false;

                _context.TalentOpportunities.Remove(opportunity);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting opportunity with ID {id}");
                return false;
            }
        }

        public async Task<bool> UpdateOpportunityStatusAsync(int id, string status)
        {
            try
            {
                if (!Enum.TryParse<OpportunityStatus>(status, out var statusEnum))
                    return false;

                var opportunity = await _context.TalentOpportunities.FindAsync(id);
                if (opportunity == null)
                    return false;

                opportunity.Status = statusEnum;
                opportunity.UpdatedAt = DateTime.UtcNow;

                _context.TalentOpportunities.Update(opportunity);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating status for opportunity with ID {id}");
                return false;
            }
        }

        public async Task<IEnumerable<AdminApplicationDTO>> GetOpportunityApplicationsAsync(int opportunityId)
        {
            try
            {
                return await _context.OpportunityApplications
                    .Include(a => a.User)
                    .Include(a => a.TalentOpportunity)
                    .Where(a => a.TalentOpportunityID == opportunityId)
                    .Select(a => new AdminApplicationDTO
                    {
                        ApplicationId = a.ApplicationID,
                        OpportunityId = a.TalentOpportunityID,
                        OpportunityTitle = a.TalentOpportunity.Title,
                        UserId = a.UserID,
                        UserName = a.User.Username,
                        UserEmail = a.User.Email,
                        Status = a.Status.ToString(),
                        AppliedAt = a.AppliedAt,
                        ReviewedAt = a.ReviewedAt,
                        CoverLetter = a.CoverLetter,
                        ReviewNotes = a.ReviewNotes
                    })
                    .OrderByDescending(a => a.AppliedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting applications for opportunity {opportunityId}");
                throw;
            }
        }

        public async Task<AdminApplicationDTO> GetApplicationByIdAsync(int applicationId)
        {
            try
            {
                return await _context.OpportunityApplications
                    .Include(a => a.User)
                    .Include(a => a.TalentOpportunity)
                    .Where(a => a.ApplicationID == applicationId)
                    .Select(a => new AdminApplicationDTO
                    {
                        ApplicationId = a.ApplicationID,
                        OpportunityId = a.TalentOpportunityID,
                        OpportunityTitle = a.TalentOpportunity.Title,
                        UserId = a.UserID,
                        UserName = a.User.Username,
                        UserEmail = a.User.Email,
                        Status = a.Status.ToString(),
                        AppliedAt = a.AppliedAt,
                        ReviewedAt = a.ReviewedAt,
                        CoverLetter = a.CoverLetter,
                        ReviewNotes = a.ReviewNotes
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting application with ID {applicationId}");
                throw;
            }
        }

        public async Task<bool> UpdateApplicationStatusAsync(int applicationId, UpdateApplicationStatusDTO statusDto)
        {
            try
            {
                if (!Enum.TryParse<ApplicationStatus>(statusDto.Status, out var status))
                    return false;

                var application = await _context.OpportunityApplications.FindAsync(applicationId);
                if (application == null)
                    return false;

                application.Status = status;
                application.ReviewNotes = statusDto.ReviewNotes;
                application.ReviewedAt = DateTime.UtcNow;

                _context.OpportunityApplications.Update(application);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating status for application {applicationId}");
                return false;
            }
        }

        public async Task<int> GetOpportunityCountAsync()
        {
            try
            {
                return await _context.TalentOpportunities.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting opportunity count");
                return 0;
            }
        }

        public async Task<int> GetApplicationCountAsync(int opportunityId)
        {
            try
            {
                return await _context.OpportunityApplications
                    .CountAsync(a => a.TalentOpportunityID == opportunityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting application count for opportunity {opportunityId}");
                return 0;
            }
        }
    }
}
