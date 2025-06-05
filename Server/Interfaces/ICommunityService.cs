using Server.DTOs;
using Server.Models;

namespace Server.Interfaces;

public interface ICommunityService
{
    Task<CommunityDTO> CreateCommunityAsync(CreateCommunityDTO dto, int userId);

    Task<List<CommunityDTO>> GetAllCommunitiesAsync();

    Task JoinCommunityAsync(int communityId, int userId);

    Task LeaveCommunityAsync(int communityId, int userId);

    Task TransferAdminAsync(int communityId, int currentAdminId, int newAdminId);

    Task RemoveMemberAsync(int communityId, int userId, int requesterId);
    Task<List<CommunityMemberDTO>> GetCommunityMembersAsync(int communityId);

    Task<List<Post>> GetCommunityPostsAsync(int communityId);
    
    
    Task<CommunityDTO> GetCommunityByIdAsync(int communityId);
    
    Task<CommunityDTO> UpdateCommunityAsync(int communityId, UpdateCommunityDTO dto, int requesterId);
    
    Task<string> CheckUserRoleAsync(int communityId, int userId);
    
    Task<bool> IsUserMemberAsync(int communityId, int userId);

    
    Task<List<CommunityDTO>> SearchCommunitiesAsync(string keyword);

    
    Task<List<CommunityMemberDTO>> GetCommunityAdminsAsync(int communityId);

    
    Task DeleteCommunityAsync(int communityId, int requesterId);
    Task<bool> IsUserAdminAsync(int communityId, int userId);





}
