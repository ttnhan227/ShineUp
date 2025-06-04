using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.DTOs;
using Server.Interfaces;
using System.Security.Claims;

namespace Server.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OpportunitiesController : ControllerBase
{
    private readonly IOpportunityRepository _opportunityRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OpportunitiesController(
        IOpportunityRepository opportunityRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _opportunityRepository = opportunityRepository;
        _httpContextAccessor = httpContextAccessor;
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
    public async Task<ActionResult<IEnumerable<OpportunityApplicationDTO>>> GetOpportunityApplications(int opportunityId)
    {
        var userId = GetCurrentUserId();
        var applications = await _opportunityRepository.GetOpportunityApplicationsAsync(opportunityId, userId);
        
        if (applications == null)
        {
            return NotFound("Opportunity not found or you don't have permission to view applications");
        }
        
        return Ok(applications);
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
    public async Task<IActionResult> UpdateApplicationStatus(
        int opportunityId, 
        int applicationId, 
        [FromBody] UpdateOpportunityApplicationDTO updateDto)
    {
        var userId = GetCurrentUserId();
        var application = await _opportunityRepository.UpdateApplicationStatusAsync(applicationId, updateDto, userId);
        
        if (application == null)
        {
            return NotFound("Application not found or you don't have permission to update it");
        }
        
        return NoContent();
    }
}
