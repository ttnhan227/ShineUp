using Server.Models;

namespace Server.Interfaces;

public interface IAuthRepository
{
    Task<User> Register(User user, string password);
    Task<User> Login(string email, string password);
    Task<bool> UserExists(string email, string username);
    Task<User> CreateUser(User user);
    Task<User?> GetUserById(int userId);
    Task<string> GenerateOTP(string email);
    Task<bool> ValidateOTP(string email, string otp);
    Task<bool> ResetPassword(string email, string newPassword);
    Task<bool> SaveOTP(int userId, string otp);
    Task<bool> VerifyOTP(int userId, string otp);
    Task<bool> VerifyEmail(string email, string otp);
}