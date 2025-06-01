using Server.DTOs;
using Server.Models;

namespace Server.Interfaces;

public interface IContestEntryRepository
{
    // Lấy tất cả các bài dự thi của một cuộc thi
    Task<IEnumerable<ContestEntry>> GetEntriesByContestAsync(int contestId);

    // Lấy chi tiết một bài dự thi theo EntryID
    Task<ContestEntry> GetEntryByIdAsync(int entryId);

    // Gửi một bài dự thi mới
    Task<ContestEntry> SubmitEntryAsync(ContestEntry entry);

    // Kiểm tra một user đã gửi bài cho contest chưa (để tránh spam)
    Task<bool> HasUserSubmittedAsync(int contestId, int userId);
}