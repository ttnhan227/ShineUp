using Server.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Server.Interfaces;

public interface IOpportunityApplicationRepository
{
    Task<OpportunityApplication> CreateAsync(OpportunityApplication application);
    Task<IEnumerable<OpportunityApplication>> GetByUserIdAsync(int userId);
    Task<OpportunityApplication?> GetByIdAsync(int applicationId);
    Task<bool> DeleteAsync(int applicationId);
}