using Server.DTOs;
using Server.Models;

namespace Server.Interfaces;

public interface IVoteRepository
{
    // Tiến hành vote (nếu hợp lệ)
    Task<Vote> CastVoteAsync(Vote vote);

    // Kiểm tra user đã vote cho entry này chưa (tránh vote trùng)
    Task<bool> HasUserVotedAsync(int entryId, int userId);

    // Lấy kết quả vote (đếm số lượt vote theo từng entry của một contest)
    Task<List<VoteResultDTO>> GetVoteResultsByContestAsync(int contestId);
}