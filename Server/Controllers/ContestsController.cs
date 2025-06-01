using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;

namespace Server.Controllers;

// Quản lý các cuộc thi: tạo mới, lấy danh sách, chi tiết cuộc thi
[ApiController]
[Route("api/[controller]")]
public class ContestsController : ControllerBase
{
    // 1. Constructor
    private readonly IContestRepository _repo;

    public ContestsController(IContestRepository repo)
    {
        _repo = repo;
    }

    // 2. [HttpGet] GetAll() – Lấy danh sách tất cả contest
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllContestsAsync());
    // Trả về danh sách toàn bộ các cuộc thi dưới dạng List<ContestDTO>.

    // 3. [HttpGet("{id}")] Get(int id) – Lấy chi tiết 1 contest theo ID
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var contest = await _repo.GetContestByIdAsync(id);
        return contest == null ? NotFound() : Ok(contest);
    }
    //Trả về chi tiết 1 contest theo id (được truyền trong URL).
    //Dùng khi bấm vào 1 cuộc thi để xem chi tiết.

    // 4. [HttpPost] Create() – Tạo mới 1 contest
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContestDTO dto)
    {
        var contest = new Contest
        {
            Title = dto.Title,
            Description = dto.Description,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };
        var created = await _repo.CreateContestAsync(contest);
        return CreatedAtAction(nameof(Get), new { id = created.ContestID }, created);
    }
    //Nhận dữ liệu từ body qua CreateContestDTO
    //Tạo object Contest mới và lưu vào DB qua repository.
}