using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Server.DTOs.Admin;
using Server.Interfaces.Admin;

namespace Server.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/opportunities")]
    [ApiController]
    public class OpportunityManagementController : ControllerBase
    {
        private readonly IOpportunityManagementRepository _repository;
        private readonly ILogger<OpportunityManagementController> _logger;
        private const int ADMIN_ROLE_ID = 2;

        public OpportunityManagementController(
            IOpportunityManagementRepository repository,
            ILogger<OpportunityManagementController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        private bool IsAdmin()
        {
            var roleIdClaim = User.FindFirst("RoleID");
            return roleIdClaim != null && int.TryParse(roleIdClaim.Value, out int roleId) && roleId == ADMIN_ROLE_ID;
        }

        // GET: api/admin/opportunities
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AdminOpportunityDTO>>> GetOpportunities()
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            try
            {
                var opportunities = await _repository.GetAllOpportunitiesAsync();
                return Ok(opportunities);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all opportunities: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error while retrieving opportunities" });
            }
        }

        // GET: api/admin/opportunities/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AdminOpportunityDetailDTO>> GetOpportunity(int id)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            try
            {
                var opportunity = await _repository.GetOpportunityByIdAsync(id);
                if (opportunity == null)
                {
                    return NotFound(new { message = "Opportunity not found" });
                }
                return Ok(opportunity);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting opportunity with ID {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error while retrieving the opportunity" });
            }
        }

        // POST: api/admin/opportunities
        [HttpPost]
        public async Task<IActionResult> CreateOpportunity([FromBody] CreateUpdateOpportunityDTO opportunityDto)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _repository.CreateOpportunityAsync(opportunityDto);
                if (!result)
                {
                    return BadRequest(new { message = "Failed to create opportunity" });
                }

                return Ok(new { message = "Opportunity created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating opportunity: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error while creating the opportunity" });
            }
        }

        // PUT: api/admin/opportunities/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOpportunity(int id, [FromBody] CreateUpdateOpportunityDTO opportunityDto)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _repository.UpdateOpportunityAsync(id, opportunityDto);
                if (!result)
                {
                    return NotFound(new { message = "Opportunity not found or could not be updated" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating opportunity with ID {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error while updating the opportunity" });
            }
        }

        // DELETE: api/admin/opportunities/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOpportunity(int id)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            try
            {
                var result = await _repository.DeleteOpportunityAsync(id);
                if (!result)
                {
                    return NotFound(new { message = "Opportunity not found or could not be deleted" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting opportunity with ID {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error while deleting the opportunity" });
            }
        }

        // PUT: api/admin/opportunities/5/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOpportunityStatus(int id, [FromBody] string status)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            if (string.IsNullOrEmpty(status))
            {
                return BadRequest(new { message = "Status is required" });
            }

            try
            {
                var result = await _repository.UpdateOpportunityStatusAsync(id, status);
                if (!result)
                {
                    return BadRequest(new { message = "Invalid status or opportunity not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating status for opportunity with ID {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error while updating the opportunity status" });
            }
        }

        // GET: api/admin/opportunities/5/applications
        [HttpGet("{id}/applications")]
        public async Task<ActionResult<IEnumerable<AdminApplicationDTO>>> GetOpportunityApplications(int id)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            try
            {
                var applications = await _repository.GetOpportunityApplicationsAsync(id);
                return Ok(applications);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting applications for opportunity {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error while retrieving applications" });
            }
        }

        // GET: api/admin/applications/5
        [HttpGet("applications/{id}")]
        public async Task<ActionResult<AdminApplicationDTO>> GetApplication(int id)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            try
            {
                var application = await _repository.GetApplicationByIdAsync(id);
                if (application == null)
                {
                    return NotFound(new { message = "Application not found" });
                }
                return Ok(application);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting application with ID {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error while retrieving the application" });
            }
        }

        // PUT: api/admin/applications/5/status
        [HttpPut("applications/{id}/status")]
        public async Task<IActionResult> UpdateApplicationStatus(int id, [FromBody] UpdateApplicationStatusDTO statusDto)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _repository.UpdateApplicationStatusAsync(id, statusDto);
                if (!result)
                {
                    return BadRequest(new { message = "Invalid status or application not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating status for application {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error while updating the application status" });
            }
        }

        // GET: api/admin/opportunities/count
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetOpportunityCount()
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            try
            {
                var count = await _repository.GetOpportunityCountAsync();
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting opportunity count: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error while getting opportunity count" });
            }
        }
    }
}
