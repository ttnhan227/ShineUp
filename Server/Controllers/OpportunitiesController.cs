using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;
using System.Security.Claims;

namespace Server.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OpportunitiesController : ControllerBase
{
    private readonly IOpportunityRepository _opportunityRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<OpportunitiesController> _logger;

    public OpportunitiesController(
        IOpportunityRepository opportunityRepository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<OpportunitiesController> logger)
    {
        _opportunityRepository = opportunityRepository;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                        ?? _httpContextAccessor.HttpContext?.User.FindFirst("id")?.Value
                        ?? throw new UnauthorizedAccessException("User ID claim not found");
        
        if (!int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID format");
        }
        
        return userId;
    }

    // GET: api/opportunities
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<OpportunityDTO>>> GetOpportunities()
    {
        var opportunities = await _opportunityRepository.GetAllOpportunitiesAsync();
        return Ok(opportunities);
    }
    
    // GET: api/opportunities/category/5
    [HttpGet("category/{categoryId}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<OpportunityDTO>>> GetOpportunitiesByCategory(int categoryId)
    {
        var opportunities = await _opportunityRepository.GetOpportunitiesByCategoryAsync(categoryId);
        return Ok(opportunities);
    }

    // GET: api/opportunities/5
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<OpportunityDTO>> GetOpportunity(int id)
    {
        var opportunity = await _opportunityRepository.GetOpportunityByIdAsync(id);
        if (opportunity == null)
        {
            return NotFound();
        }
        return Ok(opportunity);
    }

    // GET: api/opportunities/user
    [HttpGet("user")]
    public async Task<ActionResult<IEnumerable<OpportunityDTO>>> GetUserOpportunities()
    {
        var userId = GetCurrentUserId();
        var opportunities = await _opportunityRepository.GetOpportunitiesByUserAsync(userId);
        return Ok(opportunities);
    }

    // GET: api/opportunities/talent/music
    [HttpGet("talent/{talentArea}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<OpportunityDTO>>> GetOpportunitiesByTalentArea(string talentArea)
    {
        var opportunities = await _opportunityRepository.GetOpportunitiesByTalentAreaAsync(talentArea);
        return Ok(opportunities);
    }

    // POST: api/opportunities
    [HttpPost]
    public async Task<ActionResult<OpportunityDTO>> CreateOpportunity(CreateOpportunityDTO opportunityDto)
    {
        var userId = GetCurrentUserId();
        var opportunity = await _opportunityRepository.CreateOpportunityAsync(opportunityDto, userId);
        return CreatedAtAction(nameof(GetOpportunity), new { id = opportunity.Id }, opportunity);
    }

    // PUT: api/opportunities/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOpportunity(int id, UpdateOpportunityDTO opportunityDto)
    {
        var userId = GetCurrentUserId();
        var result = await _opportunityRepository.UpdateOpportunityAsync(id, opportunityDto, userId);
        
        if (result == null)
        {
            return NotFound();
        }
        
        return NoContent();
    }

    // DELETE: api/opportunities/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOpportunity(int id)
    {
        var userId = GetCurrentUserId();
        var success = await _opportunityRepository.DeleteOpportunityAsync(id, userId);
        
        if (!success)
        {
            return NotFound();
        }
        
        return NoContent();
    }

    // POST: api/opportunities/5/apply
    [HttpPost("{opportunityId}/apply")]
    public async Task<ActionResult<OpportunityApplicationDTO>> ApplyForOpportunity(
        int opportunityId, 
        [FromBody] CreateOpportunityApplicationDTO applicationDto)
    {
        if (opportunityId != applicationDto.TalentOpportunityID)
        {
            return BadRequest("Opportunity ID mismatch");
        }

        var userId = GetCurrentUserId();
        var application = await _opportunityRepository.ApplyForOpportunityAsync(applicationDto, userId);
        
        if (application == null)
        {
            return NotFound("Opportunity not found");
        }
        
        return CreatedAtAction(
            nameof(GetApplication),
            new { opportunityId, applicationId = application.ApplicationID },
            application);
    }

    // GET: api/opportunities/5/applications
    [HttpGet("{opportunityId}/applications")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<OpportunityApplicationDTO>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOpportunityApplications(int opportunityId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var applications = await _opportunityRepository.GetOpportunityApplicationsAsync(opportunityId, userId);
            
            if (applications == null)
            {
                return NotFound(new { message = "Opportunity not found or you don't have permission to view applications" });
            }
            
            return Ok(applications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting opportunity applications");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving applications" });
        }
    }

    // GET: api/opportunities/applications/me
    [HttpGet("applications/me")]
    public async Task<ActionResult<IEnumerable<OpportunityApplicationDTO>>> GetMyApplications()
    {
        var userId = GetCurrentUserId();
        var applications = await _opportunityRepository.GetUserApplicationsAsync(userId);
        return Ok(applications);
    }

    // GET: api/opportunities/5/applications/3
    [HttpGet("{opportunityId}/applications/{applicationId}")]
    public async Task<ActionResult<OpportunityApplicationDTO>> GetApplication(int opportunityId, int applicationId)
    {
        var userId = GetCurrentUserId();
        var applications = await _opportunityRepository.GetOpportunityApplicationsAsync(opportunityId, userId);
        
        if (applications == null)
        {
            return NotFound("Opportunity not found or you don't have permission to view applications");
        }
        
        var application = applications.FirstOrDefault(a => a.ApplicationID == applicationId);
        if (application == null)
        {
            return NotFound("Application not found");
        }
        
        return Ok(application);
    }

    // PUT: api/opportunities/5/applications/3/status
    [HttpPut("{opportunityId}/applications/{applicationId}/status")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OpportunityApplicationDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateApplicationStatus(
        int opportunityId, 
        int applicationId, 
        [FromBody] UpdateOpportunityApplicationDTO updateDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // First verify the opportunity exists and user has access
            var opportunity = await _opportunityRepository.GetOpportunityByIdAsync(opportunityId);
            if (opportunity == null)
            {
                return NotFound(new { message = "Opportunity not found" });
            }
            
            // Update the application status
            var application = await _opportunityRepository.UpdateApplicationStatusAsync(applicationId, updateDto, userId);
            
            if (application == null)
            {
                return NotFound(new { message = "Application not found" });
            }
            
            return Ok(application);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating application status");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while updating the application status" });
        }
    }
    
    // POST: api/opportunities/5/update-status
    [HttpPost("{id}/update-status")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OpportunityDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateStatus(int id)
    {
        try
        {
            // Get status from query string
            var status = Request.Query["status"].FirstOrDefault();
            
            _logger.LogInformation($"UpdateStatus called with id: {id}, status: {status}");
            _logger.LogInformation($"Request content type: {Request.ContentType}");
            _logger.LogInformation($"Request query string: {Request.QueryString}");
            
            if (string.IsNullOrEmpty(status))
            {
                _logger.LogWarning("Status parameter is missing or empty");
                return BadRequest(new { message = "Status is required. Valid values are: Open, Closed, Draft, Paused" });
            }
            
            // Normalize the status value (trim and capitalize first letter)
            status = status.Trim();
            if (status.Length > 0)
            {
                status = char.ToUpper(status[0]) + (status.Length > 1 ? status[1..].ToLower() : string.Empty);
            }

            var userId = GetCurrentUserId();
            var opportunity = await _opportunityRepository.GetOpportunityByIdAsync(id);
            
            if (opportunity == null)
            {
                return NotFound(new { message = "Opportunity not found" });
            }
            
            // Verify the current user is the owner of the opportunity
            if (opportunity.PostedByUserId != userId)
            {
                return Forbid();
            }
            
            // Map the status string to the enum
            if (!Enum.TryParse<OpportunityStatus>(status, true, out var statusEnum))
            {
                _logger.LogWarning($"Invalid status value provided: {status}");
                var validValues = string.Join(", ", Enum.GetNames(typeof(OpportunityStatus)));
                return BadRequest(new { 
                    message = $"Invalid status value: '{status}'. Valid values are: {validValues}",
                    validValues = Enum.GetNames(typeof(OpportunityStatus))
                });
            }
            
            // Create an update DTO with just the status
            var updateDto = new UpdateOpportunityDTO
            {
                Status = statusEnum
            };
            
            var result = await _opportunityRepository.UpdateOpportunityAsync(id, updateDto, userId);
            
            if (result == null)
            {
                _logger.LogError("Error updating opportunity status. UpdateOpportunityAsync returned null.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating opportunity status" });
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating opportunity status");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while updating the opportunity status" });
        }
    }
}
