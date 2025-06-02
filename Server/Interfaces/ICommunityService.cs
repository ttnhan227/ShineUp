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
}