using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;
using Server.Services;
using System.Security.Claims;

namespace Server.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OpportunitiesController : ControllerBase
{
    private readonly IOpportunityRepository _opportunityRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<OpportunitiesController> _logger;
    private readonly IEmailService _emailService;
    private readonly DatabaseContext _context;

    public OpportunitiesController(
        IOpportunityRepository opportunityRepository,
        INotificationRepository notificationRepository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<OpportunitiesController> logger,
        IEmailService emailService,
        DatabaseContext context)
    {
        _opportunityRepository = opportunityRepository;
        _notificationRepository = notificationRepository;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _emailService = emailService;
        _context = context;
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
            
            // Get the current application to get the user's email
            var currentApplication = await _context.OpportunityApplications
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.ApplicationID == applicationId);
                
            if (currentApplication == null)
            {
                return NotFound(new { message = "Application not found" });
            }
            
            var applicantEmail = currentApplication.User?.Email;
            var applicantName = currentApplication.User?.FullName ?? "Applicant";
            
            // Update the application status
            var application = await _opportunityRepository.UpdateApplicationStatusAsync(applicationId, updateDto, userId);
            
            if (application == null)
            {
                return NotFound(new { message = "Application not found" });
            }
            
            // Prepare notification and email content
            string notificationMessage;
            string emailSubject = "";
            string emailBody = "";
            
            var status = updateDto.Status.ToLower();
            var opportunityTitle = opportunity.Title;
            var recruiterName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Our recruitment team";
            var companyName = opportunity.PostedByUser?.FullName ?? "Our company";

            switch (status)
            {
                case "pending":
                    notificationMessage = $"Thank you for applying to '{opportunityTitle}'. We've received your application and will review it shortly.";
                    emailSubject = $"Application Received: {opportunityTitle}";
                    emailBody = $"""
                        <h2>Thank you for your application!</h2>
                        <p>Dear {applicantName},</p>
                        <p>We've received your application for the position of <strong>{opportunityTitle}</strong> at {companyName}.</p>
                        <p>Our team will review your application and get back to you as soon as possible. You can check the status of your application in your dashboard at any time.</p>
                        <p>If you have any questions, feel free to reply to this email.</p>
                        <p>Best regards,<br/>{companyName} Team</p>
                        """;
                    break;
                    
                case "reviewing":
                    notificationMessage = $"Great news! Your application for '{opportunityTitle}' is currently under review by our team.";
                    emailSubject = $"Application Update: {opportunityTitle} is Under Review";
                    emailBody = $"""
                        <h2>Your Application is Being Reviewed</h2>
                        <p>Dear {applicantName},</p>
                        <p>We're excited to let you know that your application for <strong>{opportunityTitle}</strong> at {companyName} is currently under review by our team.</p>
                        <p>We appreciate your patience during this process. We'll be in touch soon with an update.</p>
                        <p>Best regards,<br/>{companyName} Team</p>
                        """;
                    break;
                    
                case "shortlisted":
                    notificationMessage = $"Congratulations! Your application for '{opportunityTitle}' has been shortlisted. We'll be in touch soon with next steps.";
                    emailSubject = $"Congratulations! You've Been Shortlisted for {opportunityTitle}";
                    emailBody = $"""
                        <h2>Congratulations! You've Been Shortlisted</h2>
                        <p>Dear {applicantName},</p>
                        <p>We're pleased to inform you that your application for <strong>{opportunityTitle}</strong> at {companyName} has been shortlisted!</p>
                        <p>This is a significant achievement, and we were impressed by your qualifications and experience.</p>
                        <p>Our team will be in touch shortly with the next steps in the selection process.</p>
                        <p>Best regards,<br/>{companyName} Team</p>
                        """;
                    break;
                    
                case "interviewing":
                    notificationMessage = $"We're excited to move forward with your application! Let's schedule an interview for the '{opportunityTitle}' position.";
                    emailSubject = $"Interview Invitation: {opportunityTitle} at {companyName}";
                    emailBody = $"""
                        <h2>Interview Invitation</h2>
                        <p>Dear {applicantName},</p>
                        <p>We were very impressed with your application for <strong>{opportunityTitle}</strong> and would like to invite you for an interview.</p>
                        <p>Please let us know your availability for the next week, and we'll schedule a time that works for you.</p>
                        <p>If you have any questions or need to reschedule, please don't hesitate to contact us.</p>
                        <p>Best regards,<br/>{recruiterName}<br/>{companyName}</p>
                        """;
                    break;
                    
                case "accepted":
                    notificationMessage = $"Congratulations! We're thrilled to offer you the '{opportunityTitle}' position. Please check your email for the offer details.";
                    emailSubject = $"Congratulations! Offer for {opportunityTitle} at {companyName}";
                    emailBody = $"""
                        <h2>Congratulations! Job Offer</h2>
                        <p>Dear {applicantName},</p>
                        <p>We are absolutely thrilled to offer you the position of <strong>{opportunityTitle}</strong> at {companyName}!</p>
                        <p>Your skills and experience stood out among all applicants, and we believe you'll be a great addition to our team.</p>
                        <p>Please find attached the official offer letter with all the details about your compensation, benefits, and start date.</p>
                        <p>Please review the offer and let us know if you have any questions. We're excited to welcome you to the team!</p>
                        <p>Best regards,<br/>{recruiterName}<br/>{companyName}</p>
                        """;
                    break;
                    
                case "rejected":
                    notificationMessage = $"Thank you for applying to '{opportunityTitle}'. Unfortunately, we've decided to move forward with other candidates at this time.";
                    emailSubject = $"Update on Your Application for {opportunityTitle}";
                    emailBody = $"""
                        <h2>Update on Your Application</h2>
                        <p>Dear {applicantName},</p>
                        <p>Thank you for taking the time to apply for the <strong>{opportunityTitle}</strong> position at {companyName} and for sharing your experience with us.</p>
                        <p>After careful consideration, we've decided to move forward with other candidates whose qualifications more closely match our current needs.</p>
                        <p>We were impressed with your background and encourage you to apply for future openings that align with your skills and experience.</p>
                        <p>We appreciate your interest in {companyName} and wish you all the best in your job search.</p>
                        <p>Best regards,<br/>{companyName} Team</p>
                        """;
                    break;
                    
                default:
                    notificationMessage = $"There's been an update to your application for '{opportunityTitle}'. Status: {updateDto.Status}";
                    emailSubject = $"Update on Your Application for {opportunityTitle}";
                    emailBody = $"""
                        <h2>Application Status Update</h2>
                        <p>Dear {applicantName},</p>
                        <p>This is to inform you that there has been an update to your application for <strong>{opportunityTitle}</strong> at {companyName}.</p>
                        <p><strong>Status:</strong> {updateDto.Status}</p>
                        <p>You can check the latest status of your application by logging into your account.</p>
                        <p>If you have any questions, feel free to reply to this email.</p>
                        <p>Best regards,<br/>{companyName} Team</p>
                        """;
                    break;
            }

            // Add review notes if available
            if (!string.IsNullOrEmpty(updateDto.ReviewNotes))
            {
                notificationMessage += $"\n\nNote from {recruiterName}: {updateDto.ReviewNotes}";
                emailBody = emailBody.Replace("</p>", "<br/><br/>");
                emailBody += $"<p><strong>Note from {recruiterName}:</strong><br/>{updateDto.ReviewNotes}</p>";
            }
            
            // Add a footer to the email
            emailBody += """
                <hr style="margin: 30px 0; border: none; border-top: 1px solid #eaeaea;"/>
                <p style="color: #666; font-size: 12px; line-height: 1.5;">
                    This is an automated message. Please do not reply to this email. If you have any questions, 
                    please contact us through the contact information provided in your application portal.
                </p>
                """;
            
            // Create notification for the applicant
            var notificationDto = new CreateNotificationDTO
            {
                UserID = application.UserID,
                Message = notificationMessage,
                Type = NotificationType.ApplicationUpdate,
                RelatedEntityId = applicationId,
                RelatedEntityType = "Application"
            };
            
            // Send notification and email in parallel
            var notificationTask = _notificationRepository.CreateNotificationAsync(notificationDto);
            
            // Send email to the applicant if email is available
            if (!string.IsNullOrEmpty(applicantEmail))
            {
                try 
                {
                    await _emailService.SendEmailAsync(applicantEmail, emailSubject, emailBody);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send email notification for application status update");
                    // Continue even if email fails - we've already logged the error
                }
            }
            
            // Wait for notification to complete
            await notificationTask;
            
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
