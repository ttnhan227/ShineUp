using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs.Admin;
using Server.Interfaces.Admin;
using Server.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Repositories.Admin
{
    public class CategoryManagementRepository : ICategoryManagementRepository
    {
        private readonly DatabaseContext _context;

        public CategoryManagementRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AdminCategoryDTO>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                .Select(c => new AdminCategoryDTO
                {
                    CategoryID = c.CategoryID,
                    CategoryName = c.CategoryName,
                    Description = c.Description
                })
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }

        public async Task<AdminCategoryDTO> GetCategoryByIdAsync(int id)
        {
            var category = await _context.Categories
                .Where(c => c.CategoryID == id)
                .Select(c => new AdminCategoryDTO
                {
                    CategoryID = c.CategoryID,
                    CategoryName = c.CategoryName,
                    Description = c.Description
                })
                .FirstOrDefaultAsync();

            return category;
        }

        public async Task<bool> CreateCategoryAsync(CreateAdminCategoryDTO categoryDto)
        {
            var category = new Category
            {
                CategoryName = categoryDto.CategoryName,
                Description = categoryDto.Description
            };

            _context.Categories.Add(category);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateCategoryAsync(int id, CreateAdminCategoryDTO categoryDto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return false;

            category.CategoryName = categoryDto.CategoryName;
            category.Description = categoryDto.Description;

            _context.Categories.Update(category);
            return await _context.SaveChangesAsync() > 0;
        }

        public Task<bool> ToggleCategoryStatusAsync(int id, bool isActive)
        {
            // Status toggle not supported as Category model doesn't have IsActive property
            return Task.FromResult(true);
        }
    }
}
