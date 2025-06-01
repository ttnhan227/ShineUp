using Server.DTOs;
using Server.Models;

namespace Server.Interfaces;

public interface IContestRepository
{
    // Lấy danh sách tất cả các cuộc thi
    Task<IEnumerable<Contest>> GetAllContestsAsync();

    // Lấy chi tiết 1 cuộc thi theo ID
    Task<Contest> GetContestByIdAsync(int id);

    // Tạo một cuộc thi mới
    Task<Contest> CreateContestAsync(Contest contest);

    // Kiểm tra xem cuộc thi có tồn tại hay không
    Task<bool> ContestExistsAsync(int id);
}
//Interface              | Chức năng chính
//IContestRepository     | Xử lý các cuộc thi
//IContestEntryRepository| Xử lý việc gửi bài, kiểm tra đã submit
//IVoteRepository        | Xử lý vote, kiểm tra trùng vote, thống kê kết quả