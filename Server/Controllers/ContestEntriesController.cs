using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;
using System.Security.Claims;

namespace Server.Controllers;

// Quản lý các bài dự thi: gửi bài, xem bài theo contest
[ApiController]
[Route("api/[controller]")]
public class ContestEntriesController : ControllerBase
{
    // 1. Constructor
    private readonly IContestEntryRepository _repo;

    public ContestEntriesController(IContestEntryRepository repo)
    {
        _repo = repo;
    }

    // 2. [HttpGet("bycontest/{contestId}")] – Lấy tất cả bài dự thi của một cuộc thi theo contest
    [HttpGet("bycontest/{contestId}")]
    public async Task<IActionResult> GetEntries(int contestId)
        => Ok(await _repo.GetEntriesByContestAsync(contestId));
    //Gọi IContestEntryRepository.GetEntriesByContestAsync()
    //Trả về danh sách các entry thuộc contest có contestId
    //Trả về dạng DTO để đảm bảo bảo mật và dễ dùng ở frontend

    // 3. [HttpGet("{entryId}")] – Lấy chi tiết một bài dự thi theo EntryID, Gửi bài dự thi
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitEntryDTO dto) //DTO này không chứa UserID vì controller tự lấy từ token
    {
        var userId = GetUserIdFromClaims(); // Lấy từ token

        // Kiểm tra xem người dùng đã gửi bài dự thi cho cuộc thi này chưa(bảo mật, Tránh để người dùng giả mạo UserID từ client gửi lên)
        var entry = new ContestEntry
        {
            Caption = dto.Caption,
            ContestID = dto.ContestID,
            VideoID = dto.VideoID,
            UserID = userId,
            SubmittedAt = DateTime.UtcNow
        };
        var created = await _repo.SubmitEntryAsync(entry);
        return Ok(created);
    }

    // 4.[HttpGet("hasuser/{contestId}")] – Kiểm tra người dùng đã gửi bài dự thi cho cuộc thi này chưa
    // Lấy ID người dùng từ token trong Claims (token chứa ClaimTypes.NameIdentifier là UserID)
    private int GetUserIdFromClaims()
    {
        // Tìm claim có type là NameIdentifier (UserID)
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Nếu có claim => ép kiểu sang int, ngược lại => ném lỗi truy cập
        return userIdClaim != null
            ? int.Parse(userIdClaim)
            : throw new UnauthorizedAccessException();
    }
}