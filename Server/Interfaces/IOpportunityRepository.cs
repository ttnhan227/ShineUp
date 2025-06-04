using Server.DTOs;

namespace Server.Interfaces;

public interface IOpportunityRepository
{
    Task<OpportunityDTO> GetOpportunityByIdAsync(int id);
    Task<IEnumerable<OpportunityDTO>> GetAllOpportunitiesAsync();
    Task<IEnumerable<OpportunityDTO>> GetOpportunitiesByTalentAreaAsync(string talentArea);
    Task<IEnumerable<OpportunityDTO>> GetOpportunitiesByCategoryAsync(int categoryId);
    Task<IEnumerable<OpportunityDTO>> GetOpportunitiesByUserAsync(int userId);
    Task<OpportunityDTO> CreateOpportunityAsync(CreateOpportunityDTO opportunityDto, int userId);
    Task<OpportunityDTO> UpdateOpportunityAsync(int id, UpdateOpportunityDTO opportunityDto, int userId);
    Task<bool> DeleteOpportunityAsync(int id, int userId);
    Task<bool> UserOwnsOpportunity(int opportunityId, int userId);
    
    // Application related methods
    Task<OpportunityApplicationDTO> ApplyForOpportunityAsync(CreateOpportunityApplicationDTO applicationDto, int userId);
    Task<IEnumerable<OpportunityApplicationDTO>> GetUserApplicationsAsync(int userId);
    Task<IEnumerable<OpportunityApplicationDTO>> GetOpportunityApplicationsAsync(int opportunityId, int userId);
    Task<OpportunityApplicationDTO> UpdateApplicationStatusAsync(int applicationId, UpdateOpportunityApplicationDTO updateDto, int userId);
}
