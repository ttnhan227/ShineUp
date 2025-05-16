using Client.DTOs;
using Microsoft.AspNetCore.Mvc;
using Server.Models;
using Server.Repository;
using System.Text;

namespace Client.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VotesController : ControllerBase
    {
        private readonly IVoteRepository _repository;

        public VotesController(IVoteRepository repository)
        {
            _repository = repository;
        }

        [HttpPost]
        public async Task<IActionResult> Vote([FromBody] VoteDTO dto)
        {
            if (await _repository.HasVotedAsync(dto.EntryID, dto.UserID))
                return BadRequest("Already voted.");

            var vote = new Vote
            {
                EntryID = dto.EntryID,
                UserID = dto.UserID,
                VotedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(vote);
            dto.VoteID = vote.VoteID;
            dto.VotedAt = vote.VotedAt;
            return Ok(dto);
        }

        [HttpGet("contest/{contestId}")]
        public async Task<IActionResult> Results(int contestId)
        {
            var results = await _repository.GetVoteResultsAsync(contestId);
            return Ok(results);
        }
    }
}
