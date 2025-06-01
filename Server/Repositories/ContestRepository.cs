using Google;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Core.Types;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class ContestRepository : IContestRepository
{
    private readonly DatabaseContext _context;

    public ContestRepository(DatabaseContext context)
    {
        _context = context;
    }

    // Lấy toàn bộ danh sách cuộc thi
    public async Task<IEnumerable<Contest>> GetAllContestsAsync()
        => await _context.Contests.ToListAsync();

    // Lấy chi tiết một cuộc thi theo ID
    public async Task<Contest> GetContestByIdAsync(int id)
        => await _context.Contests.FindAsync(id);

    // Tạo một cuộc thi mới
    public async Task<Contest> CreateContestAsync(Contest contest)
    {
        _context.Contests.Add(contest);
        await _context.SaveChangesAsync();
        return contest;
    }

    // Kiểm tra xem cuộc thi đã tồn tại hay chưa
    public async Task<bool> ContestExistsAsync(int id)
        => await _context.Contests.AnyAsync(c => c.ContestID == id);
}
//Repository             | Mục đích
//ContestRepository      | Quản lý cuộc thi(CRUD cơ bản)
//ContestEntryRepository | Quản lý bài dự thi, kiểm tra trùng
//VoteRepository         | Ghi nhận vote, kiểm tra spam, tổng hợp kết quả