using Server.DTOs;
using Server.Models;

namespace Server.Interfaces;


public interface ICommunityRepository
{
    /// Tạo mới một cộng đồng (Moderator mặc định là người tạo)

    Task<CommunityDTO> CreateCommunityAsync(CreateCommunityDTO dto, int userId);

    /// Lấy danh sách tất cả cộng đồng (có thể lọc theo vai trò sau này)
    Task<List<Community>> GetAllCommunitiesAsync();

    /// Tìm cộng đồng theo từ khóa
    Task<List<CommunityDTO>> SearchCommunitiesAsync(string keyword);
    
    /// Lấy chi tiết 1 cộng đồng (khong bao gồm thông tin quyền của user)
    Task<CommunityDTO> GetCommunityDetailsAsync(int communityId, int userId);

    /// Tham gia cộng đồng (role mặc định là Member)
    Task JoinCommunityAsync(int communityId, int userId);

    /// Rời khỏi cộng đồng (nếu là Moderator duy nhất thì cấm)
    Task LeaveCommunityAsync(int communityId, int userId);

    /// Chuyển quyền Moderator cho thành viên khác
    Task TransferModeratorAsync(int communityId, int currentModeratorId, int newModeratorId);

    /// Xoá thành viên ra khỏi cộng đồng (chỉ Moderator mới được làm)
    Task RemoveMemberAsync(int communityId, int userId, int requesterId);

    /// Lấy danh sách thành viên trong cộng đồng
    Task<List<CommunityMemberDTO>> GetCommunityMembersAsync(int communityId);

    /// Lấy danh sách bài viết trong cộng đồng
    /// <param name="communityId">ID của cộng đồng</param>
    /// <param name="userId">ID của người dùng hiện tại (nếu có)</param>
    /// <returns>Danh sách bài viết trong cộng đồng</returns>
    Task<IEnumerable<Post>> GetCommunityPostsAsync(int communityId, int? userId = null);

   
    /// Cập nhật thông tin cộng đồng (chỉ Moderator được làm)
  
    Task<CommunityDTO> UpdateCommunityAsync(int communityId, UpdateCommunityDTO dto, int requesterId);


    /// Xoá cộng đồng (chỉ Moderator được làm)
  
    Task DeleteCommunityAsync(int communityId, int requesterId);

  
    /// Kiểm tra user có phải thành viên không
   
    Task<bool> IsUserMemberAsync(int communityId, int userId);

   
    /// Kiểm tra user có phải Moderator không
  
    Task<bool> IsUserModeratorAsync(int communityId, int userId);


    /// Lấy vai trò hiện tại của user trong cộng đồng ("Moderator" / "Member" / null nếu không tham gia)

    Task<string?> GetUserRoleAsync(int communityId, int userId);
    
    /// Lấy danh sách cộng đồng mà user là thành viên
    Task<IEnumerable<CommunityDTO>> GetUserCommunitiesAsync(int userId);
}
