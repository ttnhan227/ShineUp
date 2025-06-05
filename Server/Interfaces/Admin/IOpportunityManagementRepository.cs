using Server.DTOs.Admin;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Server.Interfaces.Admin
{
    public interface IOpportunityManagementRepository
    {
        // Opportunity CRUD operations
        Task<IEnumerable<AdminOpportunityDTO>> GetAllOpportunitiesAsync();
        Task<AdminOpportunityDetailDTO> GetOpportunityByIdAsync(int id);
        Task<bool> CreateOpportunityAsync(CreateUpdateOpportunityDTO opportunityDto);
        Task<bool> UpdateOpportunityAsync(int id, CreateUpdateOpportunityDTO opportunityDto);
        Task<bool> DeleteOpportunityAsync(int id);
        Task<bool> UpdateOpportunityStatusAsync(int id, string status);

        // Application management
        Task<IEnumerable<AdminApplicationDTO>> GetOpportunityApplicationsAsync(int opportunityId);
        Task<AdminApplicationDTO> GetApplicationByIdAsync(int applicationId);
        Task<bool> UpdateApplicationStatusAsync(int applicationId, UpdateApplicationStatusDTO statusDto);
        
        // Stats and reporting
        Task<int> GetOpportunityCountAsync();
        Task<int> GetApplicationCountAsync(int opportunityId);
    }
}
