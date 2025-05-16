using Microsoft.AspNetCore.Mvc;
using Client.DTOs;
using Server.Repository;
using Server.Models;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContestsController : ControllerBase
    {
        private readonly IContestRepository _repository;

        public ContestsController(IContestRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var contests = await _repository.GetAllAsync();
            return Ok(contests);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var contest = await _repository.GetByIdAsync(id);
            return contest == null ? NotFound() : Ok(contest);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ContestDTO dto)
        {
            var entity = new Contest
            {
                Title = dto.Title,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate
            };
            await _repository.AddAsync(entity);
            dto.ContestID = entity.ContestID;
            return CreatedAtAction(nameof(GetById), new { id = entity.ContestID }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ContestDTO dto)
        {
            if (id != dto.ContestID) return BadRequest();
            var contest = await _repository.GetByIdAsync(id);
            if (contest == null) return NotFound();
            contest.Title = dto.Title;
            contest.Description = dto.Description;
            contest.StartDate = dto.StartDate;
            contest.EndDate = dto.EndDate;
            await _repository.UpdateAsync(contest);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repository.DeleteAsync(id);
            return NoContent();
        }
    }
}
