using Google;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class VoteRepository : IVoteRepository
{
    private readonly DatabaseContext _context;

    public VoteRepository(DatabaseContext context)
    {
        _context = context;
    }

    // Ghi nhận một lượt vote mới
    public async Task<Vote> CastVoteAsync(Vote vote)
    {
        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();
        return vote;
    }

    // Kiểm tra xem người dùng đã vote cho entry này chưa (tránh spam vote)
    public async Task<bool> HasUserVotedAsync(int entryId, int userId)
        => await _context.Votes.AnyAsync(v => v.EntryID == entryId && v.UserID == userId);

    // Lấy kết quả vote của một cuộc thi (đếm số lượt vote mỗi entry)
    public async Task<List<VoteResultDTO>> GetVoteResultsByContestAsync(int contestId)
    {
        var query = from entry in _context.ContestEntries
                    where entry.ContestID == contestId
                    join vote in _context.Votes on entry.EntryID equals vote.EntryID into voteGroup
                    select new VoteResultDTO
                    {
                        EntryID = entry.EntryID,
                        VideoTitle = entry.Video.Title,
                        Caption = entry.Caption,
                        VoteCount = voteGroup.Count()
                    };

        return await query.ToListAsync();
    }
}