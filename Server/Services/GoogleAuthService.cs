using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Interfaces;
using Server.Models;

namespace Server.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly DatabaseContext _context;
    private readonly IConfiguration _configuration;

    public GoogleAuthService(DatabaseContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(string idToken)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings()
        {
            Audience = new[] { _configuration["Authentication:Google:ClientId"] }
        };

        return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
    }

    public async Task<User> HandleGoogleUser(GoogleJsonWebSignature.Payload payload)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(x => x.GoogleId == payload.Subject || x.Email == payload.Email);

        if (user == null)
        {
            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            string uniqueUsername = await GenerateUniqueUsername(payload.Name);
            
            user = new User
            {
                GoogleId = payload.Subject,
                Email = payload.Email,
                Username = uniqueUsername,
                ProfileImageURL = payload.Picture ?? "",
                Bio = "",
                RoleID = defaultRole?.RoleID ?? 1,
                TalentArea = "",
                CreatedAt = DateTime.UtcNow,
                IsActive = true, // New users are active by default
                Verified = true // Google accounts are verified by default
            };
            await _context.Users.AddAsync(user);
        }
        else
        {
            // Check if the account is active
            if (!user.IsActive)
            {
                throw new InvalidOperationException("Your account is inactive. Please contact support for assistance.");
            }

            // Always update GoogleId and ProfileImageURL for Google users
            user.GoogleId = payload.Subject;
            user.ProfileImageURL = payload.Picture ?? user.ProfileImageURL;
            user.Verified = true; // Ensure Google accounts are marked as verified
            _context.Users.Update(user);
        }

        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<string> GenerateUniqueUsername(string baseUsername)
    {
        string username = baseUsername;
        int counter = 1;

        while (await _context.Users.AnyAsync(u => u.Username == username))
        {
            username = $"{baseUsername}{counter++}";
        }

        return username;
    }
}
