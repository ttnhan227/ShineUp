using Google;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;

namespace Server.Repositories;

public class ContestEntryRepository : IContestEntryRepository
{
    private readonly DatabaseContext _context;

    public ContestEntryRepository(DatabaseContext context)
    {
        _context = context;
    }

    // Lấy tất cả bài dự thi theo ContestID
    public async Task<IEnumerable<ContestEntry>> GetEntriesByContestAsync(int contestId)
        => await _context.ContestEntries.Where(e => e.ContestID == contestId).ToListAsync();

    // Lấy chi tiết một bài dự thi theo EntryID
    public async Task<ContestEntry> GetEntryByIdAsync(int entryId)
        => await _context.ContestEntries.FindAsync(entryId);

    // Gửi bài dự thi mới
    public async Task<ContestEntry> SubmitEntryAsync(ContestEntry entry)
    {
        _context.ContestEntries.Add(entry);
        await _context.SaveChangesAsync();
        return entry;
    }

    // Kiểm tra xem người dùng đã gửi bài cho contest chưa (1 người chỉ submit 1 bài)
    // Giúp tránh spam và đảm bảo mỗi người chỉ có 1 bài dự thi
    public async Task<bool> HasUserSubmittedAsync(int contestId, int userId)
        => await _context.ContestEntries.AnyAsync(e => e.ContestID == contestId && e.UserID == userId);
}