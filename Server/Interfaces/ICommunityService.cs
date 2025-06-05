using Server.DTOs;
using Server.Models;

namespace Server.Interfaces;


public interface ICommunityService
{
    /// Tạo mới một cộng đồng (Admin mặc định là người tạo)

    Task<CommunityDTO> CreateCommunityAsync(CreateCommunityDTO dto, int userId);

    /// Lấy danh sách tất cả cộng đồng (có thể lọc theo vai trò sau này)
    Task<List<Community>> GetAllCommunitiesAsync();

    /// Tìm cộng đồng theo từ khóa
    Task<List<CommunityDTO>> SearchCommunitiesAsync(string keyword);
    
    /// Lấy chi tiết 1 cộng đồng (khong bao gồm thông tin quyền của user)
    Task<CommunityDTO> GetCommunityDetailsAsync(int communityId, int userId);

    /// Tham gia cộng đồng (role mặc định là Member)
    Task JoinCommunityAsync(int communityId, int userId);

    /// Rời khỏi cộng đồng (nếu là admin duy nhất thì cấm)
    Task LeaveCommunityAsync(int communityId, int userId);

    /// Chuyển quyền admin cho thành viên khác
    Task TransferAdminAsync(int communityId, int currentAdminId, int newAdminId);

    /// Xoá thành viên ra khỏi cộng đồng (chỉ admin mới được làm)
    Task RemoveMemberAsync(int communityId, int userId, int requesterId);

    /// Lấy danh sách thành viên trong cộng đồng
    Task<List<CommunityMemberDTO>> GetCommunityMembersAsync(int communityId);

    /// Lấy danh sách bài viết trong cộng đồng

    Task<List<Post>> GetCommunityPostsAsync(int communityId);

   
    /// Cập nhật thông tin cộng đồng (chỉ admin được làm)
  
    Task<CommunityDTO> UpdateCommunityAsync(int communityId, UpdateCommunityDTO dto, int requesterId);


    /// Xoá cộng đồng (chỉ admin được làm)
  
    Task DeleteCommunityAsync(int communityId, int requesterId);

  
    /// Kiểm tra user có phải thành viên không
   
    Task<bool> IsUserMemberAsync(int communityId, int userId);

   
    /// Kiểm tra user có phải admin không
  
    Task<bool> IsUserAdminAsync(int communityId, int userId);


    /// Lấy vai trò hiện tại của user trong cộng đồng (\"Admin\" / \"Member\" / null nếu không tham gia)

    Task<string?> GetUserRoleAsync(int communityId, int userId);
}
