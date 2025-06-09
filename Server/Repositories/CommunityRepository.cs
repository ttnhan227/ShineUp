using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class CommunityRepository : ICommunityRepository
{
    private readonly DatabaseContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<CommunityRepository> _logger;

    private readonly IPostRepository _postRepository;

    public CommunityRepository(
        DatabaseContext db, 
        IWebHostEnvironment env, 
        ICloudinaryService cloudinaryService,
        ILogger<CommunityRepository> logger,
        IPostRepository postRepository)
    {
        _db = db;
        _env = env;
        _cloudinaryService = cloudinaryService;
        _logger = logger;
        _postRepository = postRepository;
    }

    public async Task<IEnumerable<Post>> GetCommunityPostsAsync(int communityId, int? userId = null)
    {
        var posts = await _db.Posts
            .Where(p => p.CommunityID == communityId)
            .Include(p => p.User)
            .Include(p => p.Privacy)  // Include the Privacy navigation property
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Include(p => p.Images)
            .Include(p => p.Videos)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        // Filter posts based on privacy settings if user is not the owner
        if (userId.HasValue)
        {
            return posts.Where(p => p.Privacy != null && (p.Privacy.Name == "Public" || p.UserID == userId.Value));
        }
        
        return posts.Where(p => p.Privacy != null && p.Privacy.Name == "Public");
    }

    public async Task<CommunityDTO> UpdateCommunityAsync(int communityId, UpdateCommunityDTO dto, int requesterId)
    {
        var community = await _db.Communities
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.CommunityID == communityId);

        if (community == null)
            throw new KeyNotFoundException("Community not found.");

        // Check if requester is an Moderator of this community
        var isModerator = await IsUserModeratorAsync(communityId, requesterId);
        if (!isModerator)
            throw new UnauthorizedAccessException("You must be an Moderator to update this community.");

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
            Role = CommunityRole.Moderator
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
            IsCurrentUserModerator = true,
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
                FullName = m.User.FullName,
                Email = m.User.Email,
                ProfileImageUrl = m.User.ProfileImageURL,
                Role = m.Role.ToString(),
                JoinedAt = m.JoinedAt,
                LastActiveAt = m.LastActiveAt
            }).ToList(),
            IsCurrentUserMember = community.Members.Any(m => m.UserID == userId),
            IsCurrentUserModerator = community.Members.Any(m => m.UserID == userId && m.Role == CommunityRole.Moderator)
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
            throw new InvalidOperationException("Moderator must transfer Moderator rights before leaving");

        _db.CommunityMembers.Remove(member);
        await _db.SaveChangesAsync();
    }

    public async Task TransferModeratorAsync(int communityId, int currentModeratorId, int newModeratorId)
    {
        _logger.LogInformation("Starting moderator transfer for community {CommunityId} from {CurrentModeratorId} to {NewModeratorId}", 
            communityId, currentModeratorId, newModeratorId);
            
        using var transaction = await _db.Database.BeginTransactionAsync();
        
        try
        {
            // Get the community with members
            var community = await _db.Communities
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.CommunityID == communityId);

            if (community == null)
            {
                _logger.LogWarning("Community {CommunityId} not found", communityId);
                throw new KeyNotFoundException("Community not found");
            }

            _logger.LogDebug("Found community: {CommunityName} with {MemberCount} members", 
                community.Name, community.Members?.Count ?? 0);

            // Verify current user is a moderator
            var currentModerator = community.Members
                .FirstOrDefault(m => m.UserID == currentModeratorId);
                
            if (currentModerator == null)
            {
                _logger.LogWarning("Current user {UserId} is not a member of community {CommunityId}", 
                    currentModeratorId, communityId);
                throw new UnauthorizedAccessException("You are not a member of this community");
            }

            if (currentModerator.Role != CommunityRole.Moderator)
            {
                _logger.LogWarning("User {UserId} attempted to transfer moderator role but is not a moderator", currentModeratorId);
                throw new UnauthorizedAccessException("Only the current moderator can transfer moderator rights");
            }

            // Verify new moderator is a member and not the same as current moderator
            if (currentModeratorId == newModeratorId)
            {
                _logger.LogWarning("User {UserId} attempted to transfer moderator role to themselves", currentModeratorId);
                throw new ArgumentException("You are already the moderator of this community");
            }

            var newModerator = community.Members
                .FirstOrDefault(m => m.UserID == newModeratorId);
                
            if (newModerator == null)
            {
                _logger.LogWarning("New moderator {UserId} is not a member of community {CommunityId}", 
                    newModeratorId, communityId);
                throw new ArgumentException("The specified user is not a member of this community");
            }

            _logger.LogDebug("Current moderator: {CurrentModeratorId}, New moderator: {NewModeratorId}", 
                currentModeratorId, newModeratorId);

            // Update roles
            currentModerator.Role = CommunityRole.Member;
            newModerator.Role = CommunityRole.Moderator;
            
            // Update community's CreatedByUserID to the new moderator
            community.CreatedByUserID = newModeratorId;
            community.UpdatedAt = DateTime.UtcNow;

            // Save changes
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            
            _logger.LogInformation("Successfully transferred moderator role for community {CommunityId} from {CurrentModeratorId} to {NewModeratorId}", 
                communityId, currentModeratorId, newModeratorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring moderator role for community {CommunityId}", communityId);
            await transaction.RollbackAsync();
            throw; // Re-throw to be handled by the controller
        }
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
            throw new Exception("Moderator cannot remove themselves");

        if (requesterId != userId && community.CreatedByUserID != requesterId)
            throw new Exception("Only Moderator can remove other members");

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
                ProfileImageUrl = m.User.ProfileImageURL,
                Role = m.Role.ToString(),
                JoinedAt = m.JoinedAt,
                LastActiveAt = m.LastActiveAt
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
        var member = await _db.CommunityMembers
            .FirstOrDefaultAsync(cm => cm.CommunityID == communityId && cm.UserID == userId);

        return member?.Role.ToString();
    }
    
    public async Task<IEnumerable<CommunityDTO>> GetUserCommunitiesAsync(int userId)
    {
        _logger.LogInformation($"[DEBUG] Getting communities for user ID: {userId}");
        
        try 
        {
            // First, get the community IDs where the user is a member
            var communityIds = await _db.CommunityMembers
                .Where(cm => cm.UserID == userId)
                .Select(cm => cm.CommunityID)
                .ToListAsync();
                
            _logger.LogInformation($"[DEBUG] Found {communityIds.Count} community memberships for user {userId}");
            
            if (!communityIds.Any())
            {
                return new List<CommunityDTO>();
            }
            
            // Then get the communities with only the necessary includes
            var communities = await _db.Communities
                .Where(c => communityIds.Contains(c.CommunityID))
                .Include(c => c.Privacy)
                .Include(c => c.Members)
                    .ThenInclude(m => m.User)
                .AsNoTracking()
                .ToListAsync();
                
            _logger.LogInformation($"[DEBUG] Retrieved {communities.Count} communities");
            
            var result = new List<CommunityDTO>();
            
            foreach (var community in communities)
            {
                try 
                {
                    _logger.LogInformation($"[DEBUG] Processing community: {community.Name} (ID: {community.CommunityID})");
                    
                    var member = community.Members?.FirstOrDefault(m => m.UserID == userId);
                    if (member == null)
                    {
                        _logger.LogWarning($"[DEBUG] Could not find membership for user {userId} in community {community.CommunityID}");
                        continue;
                    }
                    
                    var dto = new CommunityDTO
                    {
                        CommunityID = community.CommunityID,
                        Name = community.Name,
                        Description = community.Description,
                        CoverImageUrl = community.CoverImageUrl,
                        CreatedAt = community.CreatedAt,
                        PrivacyID = community.PrivacyID,
                        IsCurrentUserModerator = member.Role == CommunityRole.Moderator,
                        IsCurrentUserMember = true,
                        MemberUserIds = community.Members?.Select(m => m.UserID).ToList() ?? new List<int>(),
                        Members = community.Members?.Select(m => new CommunityMemberDTO
                        {
                            UserID = m.UserID,
                            Username = m.User?.Username ?? "Unknown",
                            FullName = m.User?.FullName,
                            Email = m.User?.Email,
                            ProfileImageUrl = m.User?.ProfileImageURL,
                            Role = m.Role.ToString(),
                            JoinedAt = m.JoinedAt,
                            LastActiveAt = m.LastActiveAt
                        }).ToList() ?? new List<CommunityMemberDTO>()
                    };
                    
                    _logger.LogInformation($"[DEBUG] Created DTO for community: {dto.Name} with {dto.Members.Count} members");
                    result.Add(dto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[DEBUG] Error processing community {community.CommunityID}");
                    // Continue with the next community
                }
            }
            
            _logger.LogInformation($"[DEBUG] Returning {result.Count} communities");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[DEBUG] Error in GetUserCommunitiesAsync for user {userId}");
            throw new Exception("An error occurred while retrieving your communities. Please try again later.", ex);
        }
    }

    public async Task<bool> IsUserMemberAsync(int communityId, int userId)
    {
        return await _db.CommunityMembers
            .AnyAsync(m => m.CommunityID == communityId && m.UserID == userId);
    }

    public async Task<bool> IsUserModeratorAsync(int communityId, int userId)
    {
        var member = await _db.CommunityMembers
            .FirstOrDefaultAsync(m => m.CommunityID == communityId && m.UserID == userId);
            
        return member != null && member.Role == CommunityRole.Moderator;
    }

    public async Task DeleteCommunityAsync(int communityId, int requesterId)
    {
        var community = await _db.Communities.Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.CommunityID == communityId);

        if (community == null)
            throw new KeyNotFoundException("Community not found");

        if (community.CreatedByUserID != requesterId)
            throw new UnauthorizedAccessException("Only Moderator can delete");

        _db.CommunityMembers.RemoveRange(community.Members);
        _db.Communities.Remove(community);
        await _db.SaveChangesAsync();
    }
}
