using Server.DTOs.Admin;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Server.Interfaces.Admin
{
    public interface ICategoryManagementRepository
    {
        Task<IEnumerable<AdminCategoryDTO>> GetAllCategoriesAsync();
        Task<AdminCategoryDTO> GetCategoryByIdAsync(int id);
        Task<bool> CreateCategoryAsync(CreateAdminCategoryDTO categoryDto);
        Task<bool> UpdateCategoryAsync(int id, CreateAdminCategoryDTO categoryDto);
        Task<bool> ToggleCategoryStatusAsync(int id, bool isActive);
    }
}
