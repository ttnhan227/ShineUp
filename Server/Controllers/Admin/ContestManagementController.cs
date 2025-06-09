using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.DTOs.Admin;
using Server.Interfaces.Admin;
using System.Security.Claims;
using Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Server.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/[controller]")]
    [ApiController]
    public class ContestManagementController : ControllerBase
    {
        private readonly IContestManagementRepository _repository;
        private readonly ILogger<ContestManagementController> _logger;

        public ContestManagementController(
            IContestManagementRepository repository,
            ILogger<ContestManagementController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // GET: api/admin/ContestManagement
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AdminContestDTO>>> GetContests()
        {
            try
            {
                var contests = await _repository.GetAllContestsAsync();
                return Ok(contests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all contests");
                return StatusCode(500, "Internal server error while retrieving contests");
            }
        }

        // GET: api/admin/ContestManagement/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AdminContestDTO>> GetContest(int id)
        {
            try
            {
                var contest = await _repository.GetContestByIdAsync(id);
                if (contest == null)
                {
                    return NotFound();
                }
                return Ok(contest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting contest with id {id}");
                return StatusCode(500, "Internal server error while retrieving contest");
            }
        }

        // POST: api/admin/ContestManagement
        [HttpPost]
        public async Task<ActionResult<AdminContestDTO>> CreateContest(AdminCreateContestDTO createContestDto)
        {
            try
            {
                if (createContestDto.StartDate >= createContestDto.EndDate)
                {
                    return BadRequest("End date must be after start date");
                }

                if (createContestDto.EndDate < DateTime.UtcNow)
                {
                    return BadRequest("End date must be in the future");
                }


                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var contest = await _repository.CreateContestAsync(createContestDto, userId);
                
                return CreatedAtAction(nameof(GetContest), new { id = contest.ContestID }, contest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contest");
                return StatusCode(500, "Internal server error while creating contest");
            }
        }

        // PUT: api/admin/ContestManagement/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateContest(int id, AdminUpdateContestDTO updateContestDto)
        {
            try
            {
                if (id != updateContestDto.ContestID)
                {
                    return BadRequest("ID in URL does not match ID in request body");
                }

                if (updateContestDto.StartDate >= updateContestDto.EndDate)
                {
                    return BadRequest("End date must be after start date");
                }

                var existingContest = await _repository.GetContestByIdAsync(id);
                if (existingContest == null)
                {
                    return NotFound();
                }


                var updatedContest = await _repository.UpdateContestAsync(id, updateContestDto);
                if (updatedContest == null)
                {
                    return NotFound();
                }

                return Ok(updatedContest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating contest with id {id}");
                return StatusCode(500, "Internal server error while updating contest");
            }
        }

        // DELETE: api/admin/ContestManagement/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContest(int id)
        {
            try
            {
                var result = await _repository.DeleteContestAsync(id);
                if (!result)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting contest with id {id}");
                return StatusCode(500, "Internal server error while deleting contest");
            }
        }

        // GET: api/admin/ContestManagement/5/entries
        [HttpGet("{id}/entries")]
        public async Task<ActionResult<IEnumerable<ContestEntry>>> GetContestEntries(int id)
        {
            try
            {
                var entries = await _repository.GetContestEntriesAsync(id);
                return Ok(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting entries for contest with id {id}");
                return StatusCode(500, "Internal server error while retrieving contest entries");
            }
        }

        // DELETE: api/admin/ContestManagement/entries/5
        [HttpDelete("entries/{id}")]
        public async Task<IActionResult> DeleteContestEntry(int id)
        {
            try
            {
                var result = await _repository.DeleteContestEntryAsync(id);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contest entry with ID {ContestEntryId}", id);
                return StatusCode(500, "Internal server error while deleting contest entry");
            }
        }
        
        // POST: api/admin/ContestManagement/entries/5/declare-winner
        [HttpPost("entries/{entryId}/declare-winner")]
        public async Task<IActionResult> DeclareWinner(int entryId)
        {
            try
            {
                var result = await _repository.DeclareWinnerAsync(entryId);
                if (!result)
                    return NotFound("Entry or contest not found");
                    
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error declaring winner for entry {EntryId}", entryId);
                return StatusCode(500, "Internal server error while declaring winner");
            }
        }

        // GET: api/admin/ContestManagement/5/stats
        [HttpGet("{id}/stats")]
        public async Task<ActionResult<ContestStatsDTO>> GetContestStats(int id)
        {
            try
            {
                var stats = await _repository.GetContestStatsAsync(id);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting stats for contest with id {id}");
                return StatusCode(500, "Internal server error while retrieving contest stats");
            }
        }
    }
}
