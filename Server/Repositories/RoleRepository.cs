using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly DatabaseContext _context;

        public RoleRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Role?> GetByIdAsync(int id) => await _context.Roles.FindAsync(id);
        public async Task<List<Role>> GetAllAsync() => await _context.Roles.ToListAsync();
        public async Task AddAsync(Role role)
        {
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(Role role)
        {
            _context.Entry(role).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(int id)
        {
            var roleToDelete = await _context.Roles.FindAsync(id);
            if (roleToDelete != null)
            {
                _context.Roles.Remove(roleToDelete);
                await _context.SaveChangesAsync();
            }
        }
    }
}
