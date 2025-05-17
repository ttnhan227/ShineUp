using Server.Data;
using Server.Interfaces;
using Server.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Server.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DatabaseContext _context;

        public AuthRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<User> Register(User user, string password)
        {
            // Hash the password
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.PasswordHash = passwordHash;

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<User> Login(string email, string password)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .SingleOrDefaultAsync(x => x.Email.Equals(email));

            if (user == null)
                return null;

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            return user;
        }

        public async Task<bool> UserExists(string email, string username)
        {
            if (await _context.Users.AnyAsync(u => u.Email == email || u.Username == username))
                return true;

            return false;
        }
    }
}
