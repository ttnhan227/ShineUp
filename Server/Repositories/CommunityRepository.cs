using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class CommunityRepository : ICommunityService
{
    private readonly DatabaseContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<CommunityRepository> _logger;

    public CommunityRepository(
        DatabaseContext db, 
        IWebHostEnvironment env, 
        ICloudinaryService cloudinaryService,
        ILogger<CommunityRepository> logger)
    {
        _db = db;
        _env = env;
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    public async Task<CommunityDTO> UpdateCommunityAsync(int communityId, UpdateCommunityDTO dto, int requesterId)
    {
        var community = await _db.Communities
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.CommunityID == communityId);

        if (community == null)
            throw new KeyNotFoundException("Community not found.");

        // Check if requester is an admin of this community
        var isAdmin = await IsUserAdminAsync(communityId, requesterId);
        if (!isAdmin)
            throw new UnauthorizedAccessException("You must be an admin to update this community.");

        // Update properties if they are provided in the DTO
        if (!string.IsNullOrWhiteSpace(dto.Name))
            community.Name = dto.Name.Trim();

        if (dto.Description != null)
            community.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

        if (dto.PrivacyID.HasValue)
        {
            if (dto.PrivacyID != 1 && dto.PrivacyID != 3)
                throw new ArgumentException("Only 'Public' (1) or 'Private' (3) privacy levels are allowed.");
            
            var privacyExists = await _db.Privacies.AnyAsync(p => p.PrivacyID == dto.PrivacyID.Value);
            if (!privacyExists)
                throw new ArgumentException("Invalid PrivacyID.");
                
            community.PrivacyID = dto.PrivacyID.Value;
        }

        // Handle cover image upload if provided
        if (dto.CoverImage != null && dto.CoverImage.Length > 0)
        {
            var ext = Path.GetExtension(dto.CoverImage.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png" };
            
            if (!allowed.Contains(ext))
                throw new ArgumentException("Unsupported image format. Only JPG, JPEG, and PNG are allowed.");

            try
            {
                // Upload new cover image to Cloudinary
                var uploadResult = await _cloudinaryService.UploadImgAsync(dto.CoverImage);
                
                if (uploadResult.Error != null)
                {
                    throw new Exception($"Failed to upload image to Cloudinary: {uploadResult.Error.Message}");
                }


                // Store the secure URL from Cloudinary
                community.CoverImageUrl = uploadResult.SecureUrl.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading community cover image to Cloudinary");
                throw new Exception("Failed to upload cover image. Please try again.");
            }
        }

        community.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Return the updated community DTO
        return await GetCommunityDetailsAsync(community.CommunityID, requesterId);
    }

    public async Task<CommunityDTO> CreateCommunityAsync(CreateCommunityDTO dto, int userId)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > 100)
            throw new ArgumentException("Community name is invalid.");

        var community = new Community
        {
            Name = dto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedByUserID = userId,
            PrivacyID = dto.PrivacyID
        };

        if (dto.CoverImage != null && dto.CoverImage.Length > 0)
        {
            var ext = Path.GetExtension(dto.CoverImage.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png" };
            if (!allowed.Contains(ext))
                throw new ArgumentException("Unsupported image format. Only JPG, JPEG, and PNG are allowed.");

            try
            {
                // Upload to Cloudinary
                var uploadResult = await _cloudinaryService.UploadImgAsync(dto.CoverImage);
                
                if (uploadResult.Error != null)
                {
                    throw new Exception($"Failed to upload image to Cloudinary: {uploadResult.Error.Message}");
                }

                // Store the secure URL from Cloudinary
                community.CoverImageUrl = uploadResult.SecureUrl.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading community cover image to Cloudinary");
                throw new Exception("Failed to upload cover image. Please try again.");
            }
        }

        if (dto.PrivacyID.HasValue)
        {
            var privacyExists = await _db.Privacies.AnyAsync(p => p.PrivacyID == dto.PrivacyID.Value);
            if (!privacyExists)
                throw new Exception("Invalid PrivacyID.");
        }
        if (dto.PrivacyID != 1 && dto.PrivacyID != 3)
        {
            throw new Exception("Only 'Public' (1) or 'Private' (3) privacy levels are allowed.");
        }

        _db.Communities.Add(community);

        _db.CommunityMembers.Add(new CommunityMember
        {
            UserID = userId,
            Community = community,
            JoinedAt = DateTime.UtcNow,
            Role = CommunityRole.Admin
        });

        await _db.SaveChangesAsync();

        return new CommunityDTO
        {
            CommunityID = community.CommunityID,
            Name = community.Name,
            Description = community.Description,
            CoverImageUrl = community.CoverImageUrl,
            CreatedAt = community.CreatedAt,
            CreatedByUserID = userId,
            PrivacyID = community.PrivacyID,
            MemberUserIds = new List<int> { userId },
            IsCurrentUserAdmin = true,
            IsCurrentUserMember = true,
            Members = new List<CommunityMemberDTO>(),
            Posts = new List<PostListResponseDto>()
        };
    }

    public async Task<List<Community>> GetAllCommunitiesAsync() =>
        await _db.Communities
            .AsNoTracking()
            .Include(c => c.Members)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new Community
            {
                CommunityID = c.CommunityID,
                Name = c.Name,
                Description = c.Description,
                CoverImageUrl = c.CoverImageUrl,
                CreatedAt = c.CreatedAt,
                CreatedByUserID = c.CreatedByUserID,
                PrivacyID = c.PrivacyID,
                Members = c.Members.Select(m => new CommunityMember
                {
                    UserID = m.UserID,
                    Role = m.Role
                }).ToList()
            })
            .ToListAsync();

    public async Task<List<CommunityDTO>> SearchCommunitiesAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return new();
        keyword = keyword.Trim().ToLower();

        return await _db.Communities
            .AsNoTracking()
            .Where(c => c.Name.ToLower().Contains(keyword) ||
                        (c.Description != null && c.Description.ToLower().Contains(keyword)))
            .Include(c => c.Members)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommunityDTO
            {
                CommunityID = c.CommunityID,
                Name = c.Name,
                Description = c.Description,
                CoverImageUrl = c.CoverImageUrl,
                CreatedAt = c.CreatedAt,
                CreatedByUserID = c.CreatedByUserID,
                PrivacyID = c.PrivacyID,
                MemberUserIds = c.Members.Select(m => m.UserID).ToList()
            })
            .ToListAsync();
    }

    public async Task<CommunityDTO> GetCommunityDetailsAsync(int communityId, int userId)
    {
        var community = await _db.Communities
            .Include(c => c.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(c => c.CommunityID == communityId);

        if (community == null)
            throw new KeyNotFoundException("Community not found.");

        return new CommunityDTO
        {
            CommunityID = community.CommunityID,
            Name = community.Name,
            Description = community.Description,
            CoverImageUrl = community.CoverImageUrl,
            CreatedAt = community.CreatedAt,
            CreatedByUserID = community.CreatedByUserID,
            PrivacyID = community.PrivacyID,
            MemberUserIds = community.Members.Select(m => m.UserID).ToList(),
            Members = community.Members.Select(m => new CommunityMemberDTO
            {
                UserID = m.UserID,
                Username = m.User.Username,
                Email = m.User.Email,
                Role = m.Role.ToString(),
                JoinedAt = m.JoinedAt,
                LastActiveAt = m.LastActiveAt
            }).ToList(),
            IsCurrentUserMember = community.Members.Any(m => m.UserID == userId),
            IsCurrentUserAdmin = community.Members.Any(m => m.UserID == userId && m.Role == CommunityRole.Admin)
        };
    }

    public async Task JoinCommunityAsync(int communityId, int userId)
    {
        if (!await _db.Communities.AnyAsync(c => c.CommunityID == communityId))
            throw new KeyNotFoundException("Community not found");

        if (await _db.CommunityMembers.AnyAsync(m => m.CommunityID == communityId && m.UserID == userId))
            throw new InvalidOperationException("Already a member");

        _db.CommunityMembers.Add(new CommunityMember
        {
            CommunityID = communityId,
            UserID = userId,
            JoinedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow,
            Role = CommunityRole.Member
        });
        await _db.SaveChangesAsync();
    }

    public async Task LeaveCommunityAsync(int communityId, int userId)
    {
        var community = await _db.Communities.Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.CommunityID == communityId);

        if (community == null)
            throw new KeyNotFoundException("Community not found");

        var member = community.Members.FirstOrDefault(m => m.UserID == userId);
        if (member == null)
            throw new InvalidOperationException("Not a member");

        if (community.CreatedByUserID == userId)
            throw new InvalidOperationException("Admin must transfer Admin rights before leaving");

        _db.CommunityMembers.Remove(member);
        await _db.SaveChangesAsync();
    }

    public async Task TransferAdminAsync(int communityId, int currentAdminId, int newAdminId)
    {
        var community = await _db.Communities.Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.CommunityID == communityId);

        if (community == null)
            throw new Exception("Community not found");

        var currentAdmin = community.Members.FirstOrDefault(m => m.UserID == currentAdminId);
        if (currentAdmin == null || currentAdmin.Role != CommunityRole.Admin)
            throw new Exception("Only current Admin can transfer Admin rights");

        var newAdmin = community.Members.FirstOrDefault(m => m.UserID == newAdminId);
        if (newAdmin == null)
            throw new Exception("New Admin must be a member");

        currentAdmin.Role = CommunityRole.Member;
        newAdmin.Role = CommunityRole.Admin;
        community.CreatedByUserID = newAdminId;

        await _db.SaveChangesAsync();
    }

    public async Task RemoveMemberAsync(int communityId, int userId, int requesterId)
    {
        var community = await _db.Communities.Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.CommunityID == communityId);

        if (community == null)
            throw new Exception("Community not found");

        var member = community.Members.FirstOrDefault(m => m.UserID == userId);
        if (member == null)
            throw new Exception("User is not a member");

        if (userId == community.CreatedByUserID)
            throw new Exception("Admin cannot remove themselves");

        if (requesterId != userId && community.CreatedByUserID != requesterId)
            throw new Exception("Only Admin can remove other members");

        _db.CommunityMembers.Remove(member);
        await _db.SaveChangesAsync();
    }

    public async Task<List<CommunityMemberDTO>> GetCommunityMembersAsync(int communityId)
    {
        if (!await _db.Communities.AnyAsync(c => c.CommunityID == communityId))
            throw new Exception("Community not found");

        return await _db.CommunityMembers
            .Where(m => m.CommunityID == communityId)
            .Include(m => m.User)
            .Select(m => new CommunityMemberDTO
            {
                UserID = m.UserID,
                FullName = m.User.FullName,
                Username = m.User.Username,
                Email = m.User.Email,
                Role = m.Role.ToString(),
                JoinedAt = m.JoinedAt
            })
            .ToListAsync();
    }

    public async Task<List<Post>> GetCommunityPostsAsync(int communityId)
    {
        if (!await _db.Communities.AnyAsync(c => c.CommunityID == communityId))
            throw new KeyNotFoundException("Community not found");

        return await _db.Posts
            .Where(p => p.CommunityID == communityId)
            .Include(p => p.User)
            .Include(p => p.Comments).ThenInclude(c => c.User)
            .Include(p => p.Likes).ThenInclude(l => l.User)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<string?> GetUserRoleAsync(int communityId, int userId)
    {
        var member = await _db.CommunityMembers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.CommunityID == communityId && m.UserID == userId);
        return member?.Role.ToString();
    }

    public async Task<bool> IsUserMemberAsync(int communityId, int userId) =>
        await _db.CommunityMembers.AnyAsync(m => m.CommunityID == communityId && m.UserID == userId);

    public async Task<bool> IsUserAdminAsync(int communityId, int userId)
    {
        var role = await GetUserRoleAsync(communityId, userId);
        return role == CommunityRole.Admin.ToString();
    }

    public async Task DeleteCommunityAsync(int communityId, int requesterId)
    {
        var community = await _db.Communities.Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.CommunityID == communityId);

        if (community == null)
            throw new KeyNotFoundException("Community not found");

        if (community.CreatedByUserID != requesterId)
            throw new UnauthorizedAccessException("Only Admin can delete");

        _db.CommunityMembers.RemoveRange(community.Members);
        _db.Communities.Remove(community);
        await _db.SaveChangesAsync();
    }
}
