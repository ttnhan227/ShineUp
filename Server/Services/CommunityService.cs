using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;

namespace Server.Services;

public class CommunityService : ICommunityService
{
    private readonly DatabaseContext _db;
    private readonly IWebHostEnvironment _env;

    public CommunityService(DatabaseContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
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

        // Upload cover image if provided
        if (dto.CoverImage != null && dto.CoverImage.Length > 0)
        {
            var ext = Path.GetExtension(dto.CoverImage.FileName);
            var allowed = new[] { ".jpg", ".jpeg", ".png" };
            if (!allowed.Contains(ext.ToLower()))
                throw new ArgumentException("Unsupported image format.");

            var fileName = $"{Guid.NewGuid()}{ext}";
            var uploadPath = Path.Combine(_env.WebRootPath, "uploads");

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var path = Path.Combine(uploadPath, fileName);
            using var stream = new FileStream(path, FileMode.Create);
            await dto.CoverImage.CopyToAsync(stream);

            community.CoverImageUrl = $"/uploads/{fileName}";
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

        // Add creator as first member
        _db.CommunityMembers.Add(new CommunityMember
        {
            UserID = userId,
            Community = community,
            JoinedAt = DateTime.UtcNow,
            Role = CommunityRole.Admin // set creator as admin
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
            MemberUserIds = new List<int> { userId }
            
        };
    }

    public async Task<List<CommunityDTO>> GetAllCommunitiesAsync()
    {
        return await _db.Communities
            .AsNoTracking()
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

    public async Task JoinCommunityAsync(int communityId, int userId)
    {
        var exists = await _db.Communities.AnyAsync(c => c.CommunityID == communityId);
        if (!exists) throw new KeyNotFoundException("Community not found");

        var alreadyJoined = await _db.CommunityMembers.AnyAsync(m => m.CommunityID == communityId && m.UserID == userId);
        if (alreadyJoined) throw new InvalidOperationException("You are already a member of this community");

        _db.CommunityMembers.Add(new CommunityMember
        {
            CommunityID = communityId,
            UserID = userId,
            JoinedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    public async Task LeaveCommunityAsync(int communityId, int userId)
    {
        var community = await _db.Communities
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.CommunityID == communityId);

        if (community == null)
            throw new KeyNotFoundException("Community not found");

        var member = community.Members.FirstOrDefault(m => m.UserID == userId);
        if (member == null)
            throw new InvalidOperationException("You are not a member of this community");

        if (community.CreatedByUserID == userId)
            throw new InvalidOperationException("Admin cannot leave the community without transferring admin rights");

        _db.CommunityMembers.Remove(member);
        await _db.SaveChangesAsync();
    }

    public async Task TransferAdminAsync(int communityId, int currentAdminId, int newAdminId)
    {
        var community = await _db.Communities
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.CommunityID == communityId);

        if (community == null)
            throw new Exception("Community not found");

        var currentAdmin = community.Members.FirstOrDefault(m => m.UserID == currentAdminId);
        if (currentAdmin == null || currentAdmin.Role != CommunityRole.Admin)
            throw new Exception("Only current admin can transfer ownership");

        var newAdmin = community.Members.FirstOrDefault(m => m.UserID == newAdminId);
        if (newAdmin == null)
            throw new Exception("New admin must be a member");

        currentAdmin.Role = CommunityRole.Member;
        newAdmin.Role = CommunityRole.Admin;

        community.CreatedByUserID = newAdminId;

        await _db.SaveChangesAsync();
    }


    public async Task<List<CommunityMemberDTO>> GetCommunityMembersAsync(int communityId)
    {
        // Kiểm tra Community có tồn tại không
        var communityExists = await _db.Communities
            .AnyAsync(c => c.CommunityID == communityId);

        if (!communityExists)
            throw new Exception("Community not found");

        // Lấy danh sách thành viên của cộng đồng
        var members = await _db.CommunityMembers
            .Where(m => m.CommunityID == communityId)
            .Include(m => m.User) // Eager loading thông tin người dùng
            .Select(m => new CommunityMemberDTO
            {
                UserID    = m.UserID,
                FullName  = m.User.FullName,
                Username  = m.User.Username,
                Email     = m.User.Email,
                Role      = m.Role.ToString(),        // Enum được ép thành chuỗi "Admin" / "Member"
                JoinedAt  = m.JoinedAt
            })
            .OrderBy(m => m.FullName) // Sắp xếp theo tên cho dễ hiển thị
            .ToListAsync();

        return members;
    }

    public async Task RemoveMemberAsync(int communityId, int userId, int requesterId)
    {
        var community = await _db.Communities
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.CommunityID == communityId);

        if (community == null)
            throw new Exception("Community not found.");

        var member = community.Members.FirstOrDefault(m => m.UserID == userId);
        if (member == null)
            throw new Exception("This user is not a member of the community.");

        if (community.CreatedByUserID == userId)
            throw new Exception("Admin cannot remove themselves. Please transfer admin role first.");

        if (requesterId != userId && community.CreatedByUserID != requesterId)
            throw new Exception("Only the community admin can remove other members.");

        _db.CommunityMembers.Remove(member);
        await _db.SaveChangesAsync();

        bool hasMembers = await _db.CommunityMembers.AnyAsync(m => m.CommunityID == communityId);
        if (!hasMembers)
        {
            _db.Communities.Remove(community);
            await _db.SaveChangesAsync();
        }
    }



    public async Task<List<Post>> GetCommunityPostsAsync(int communityId)
    {
        var exists = await _db.Communities.AnyAsync(c => c.CommunityID == communityId);
        if (!exists) throw new KeyNotFoundException("Community not found");

        return await _db.Posts
            .Where(p => p.CommunityID == communityId)
            .Include(p => p.User)
            .Include(p => p.Comments)
            .Include(p => p.Likes)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}
