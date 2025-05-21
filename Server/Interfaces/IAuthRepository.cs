using Server.Models;
using System.Threading.Tasks;

namespace Server.Interfaces
{
    public interface IAuthRepository
    {
        Task<User> Register(User user, string password);
        Task<User> Login(string email, string password);
        Task<bool> UserExists(string email, string username);
        Task<User?> GetUserByGoogleId(string googleId);
        Task<User> CreateUser(User user);
        Task<User?> GetUserById(int userId);
    }
}
