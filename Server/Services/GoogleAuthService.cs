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
    private readonly ILogger<GoogleAuthService> _logger;

    public GoogleAuthService(DatabaseContext context, IConfiguration configuration, ILogger<GoogleAuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
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
        _logger.LogInformation($"[GoogleAuthService] Handling Google user with email: {payload.Email}, Subject: {payload.Subject}");
        
        // First try to find by Google ID
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(x => x.GoogleId == payload.Subject);
            
        _logger.LogInformation($"[GoogleAuthService] User found by Google ID: {user != null}");

        // If not found by Google ID, try by email
        if (user == null && !string.IsNullOrEmpty(payload.Email))
        {
            user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(x => x.Email == payload.Email);
                
            _logger.LogInformation($"[GoogleAuthService] User found by email: {user != null}");
        }

        if (user == null)
        {
            _logger.LogInformation("[GoogleAuthService] Creating new user");
            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            string uniqueUsername = await GenerateUniqueUsername(payload.Name);
            
            user = new User
            {
                GoogleId = payload.Subject,
                Email = payload.Email,
                Username = uniqueUsername,
                FullName = payload.Name,
                ProfileImageURL = payload.Picture,
                Bio = "",
                RoleID = defaultRole?.RoleID ?? 1,
                TalentArea = "",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Verified = true
            };
            await _context.Users.AddAsync(user);
        }
        else
        {
            _logger.LogInformation($"[GoogleAuthService] Updating existing user with ID: {user.UserID}");
            
            // Check if the account is active
            if (!user.IsActive)
            {
                _logger.LogWarning($"[GoogleAuthService] User account is inactive: {user.UserID}");
                throw new InvalidOperationException("Your account is inactive. Please contact support for assistance.");
            }

            // Update Google ID if not set
            if (string.IsNullOrEmpty(user.GoogleId))
            {
                _logger.LogInformation($"[GoogleAuthService] Adding Google ID to existing user");
                user.GoogleId = payload.Subject;
            }
            
            // Only update FullName if it's not already set
            if (string.IsNullOrEmpty(user.FullName))
            {
                user.FullName = payload.Name;
            }
            
            // Always update the profile picture from Google
            user.ProfileImageURL = payload.Picture;
            user.Verified = true;
            _context.Users.Update(user);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation($"[GoogleAuthService] User handled successfully. UserID: {user.UserID}, Email: {user.Email}");
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
