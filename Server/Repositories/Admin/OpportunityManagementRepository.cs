using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.DTOs.Admin;
using Server.Interfaces;
using Server.Interfaces.Admin;
using Server.Models;

namespace Server.Repositories.Admin;

public class OpportunityManagementRepository : IOpportunityManagementRepository
{
    private readonly DatabaseContext _context;
    private readonly ILogger<OpportunityManagementRepository> _logger;
    private readonly IEmailService _emailService;

    public OpportunityManagementRepository(
        DatabaseContext context, 
        ILogger<OpportunityManagementRepository> logger,
        IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<IEnumerable<AdminOpportunityDTO>> GetAllOpportunitiesAsync()
    {
        try
        {
            return await _context.TalentOpportunities
                .Include(o => o.Category)
                .Include(o => o.PostedByUser)
                .Select(o => new AdminOpportunityDTO
                {
                    Id = o.Id,
                    Title = o.Title,
                    Location = o.Location,
                    IsRemote = o.IsRemote,
                    Type = o.Type.ToString(),
                    Status = o.Status.ToString(),
                    ApplicationDeadline = o.ApplicationDeadline,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt,
                    CategoryName = o.Category != null ? o.Category.CategoryName : null,
                    TalentArea = o.TalentArea,
                    PostedBy = o.PostedByUser.Username,
                    ApplicationCount = o.Applications != null ? o.Applications.Count : 0
                })
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all opportunities");
            throw;
        }
    }

    public async Task<AdminOpportunityDetailDTO> GetOpportunityByIdAsync(int id)
    {
        try
        {
            var opportunity = await _context.TalentOpportunities
                .Include(o => o.Category)
                .Include(o => o.PostedByUser)
                .Where(o => o.Id == id)
                .Select(o => new AdminOpportunityDetailDTO
                {
                    Id = o.Id,
                    Title = o.Title,
                    Description = o.Description,
                    Location = o.Location,
                    IsRemote = o.IsRemote,
                    Type = o.Type.ToString(),
                    Status = o.Status.ToString(),
                    ApplicationDeadline = o.ApplicationDeadline,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt,
                    CategoryName = o.Category != null ? o.Category.CategoryName : null,
                    TalentArea = o.TalentArea,
                    PostedBy = o.PostedByUser.Username,
                    PostedByEmail = o.PostedByUser.Email,
                    PostedByUserId = o.PostedByUserId,
                    ApplicationCount = o.Applications != null ? o.Applications.Count : 0
                })
                .FirstOrDefaultAsync();

            return opportunity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting opportunity with ID {id}");
            throw;
        }
    }

    public async Task<bool> CreateOpportunityAsync(CreateUpdateOpportunityDTO opportunityDto)
    {
        try
        {
            if (!Enum.TryParse<OpportunityType>(opportunityDto.Type, out var type) ||
                !Enum.TryParse<OpportunityStatus>(opportunityDto.Status, out var status))
            {
                return false;
            }

            var opportunity = new TalentOpportunity
            {
                Title = opportunityDto.Title,
                Description = opportunityDto.Description,
                Location = opportunityDto.Location,
                IsRemote = opportunityDto.IsRemote,
                Type = type,
                Status = status,
                ApplicationDeadline = opportunityDto.ApplicationDeadline?.ToUniversalTime(),
                CategoryId = opportunityDto.CategoryId,
                TalentArea = opportunityDto.TalentArea,
                PostedByUserId = 1, // TODO: Get from current user
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.TalentOpportunities.Add(opportunity);
            return await _context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating opportunity");
            return false;
        }
    }

    public async Task<bool> UpdateOpportunityAsync(int id, CreateUpdateOpportunityDTO opportunityDto)
    {
        try
        {
            if (!Enum.TryParse<OpportunityType>(opportunityDto.Type, out var type) ||
                !Enum.TryParse<OpportunityStatus>(opportunityDto.Status, out var status))
            {
                return false;
            }

            var opportunity = await _context.TalentOpportunities.FindAsync(id);
            if (opportunity == null)
            {
                return false;
            }

            opportunity.Title = opportunityDto.Title;
            opportunity.Description = opportunityDto.Description;
            opportunity.Location = opportunityDto.Location;
            opportunity.IsRemote = opportunityDto.IsRemote;
            opportunity.Type = type;
            opportunity.Status = status;
            opportunity.ApplicationDeadline = opportunityDto.ApplicationDeadline?.ToUniversalTime();
            opportunity.CategoryId = opportunityDto.CategoryId;
            opportunity.TalentArea = opportunityDto.TalentArea;
            opportunity.UpdatedAt = DateTime.UtcNow;

            _context.TalentOpportunities.Update(opportunity);
            return await _context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating opportunity with ID {id}");
            return false;
        }
    }

    public async Task<bool> DeleteOpportunityAsync(int id)
    {
        try
        {
            var opportunity = await _context.TalentOpportunities.FindAsync(id);
            if (opportunity == null)
            {
                return false;
            }

            _context.TalentOpportunities.Remove(opportunity);
            return await _context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting opportunity with ID {id}");
            return false;
        }
    }

    public async Task<bool> UpdateOpportunityStatusAsync(int id, string status)
    {
        try
        {
            if (!Enum.TryParse<OpportunityStatus>(status, out var statusEnum))
            {
                return false;
            }

            var opportunity = await _context.TalentOpportunities.FindAsync(id);
            if (opportunity == null)
            {
                return false;
            }

            opportunity.Status = statusEnum;
            opportunity.UpdatedAt = DateTime.UtcNow;

            _context.TalentOpportunities.Update(opportunity);
            return await _context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating status for opportunity with ID {id}");
            return false;
        }
    }

    public async Task<IEnumerable<AdminApplicationDTO>> GetOpportunityApplicationsAsync(int opportunityId)
    {
        try
        {
            return await _context.OpportunityApplications
                .Include(a => a.User)
                .Include(a => a.TalentOpportunity)
                .Where(a => a.TalentOpportunityID == opportunityId)
                .Select(a => new AdminApplicationDTO
                {
                    ApplicationId = a.ApplicationID,
                    OpportunityId = a.TalentOpportunityID,
                    OpportunityTitle = a.TalentOpportunity.Title,
                    UserId = a.UserID,
                    UserName = a.User.Username,
                    UserEmail = a.User.Email,
                    Status = a.Status.ToString(),
                    AppliedAt = a.AppliedAt,
                    ReviewedAt = a.ReviewedAt,
                    CoverLetter = a.CoverLetter,
                    ReviewNotes = a.ReviewNotes
                })
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting applications for opportunity {opportunityId}");
            throw;
        }
    }

    public async Task<AdminApplicationDTO> GetApplicationByIdAsync(int applicationId)
    {
        try
        {
            return await _context.OpportunityApplications
                .Include(a => a.User)
                .Include(a => a.TalentOpportunity)
                .Where(a => a.ApplicationID == applicationId)
                .Select(a => new AdminApplicationDTO
                {
                    ApplicationId = a.ApplicationID,
                    OpportunityId = a.TalentOpportunityID,
                    OpportunityTitle = a.TalentOpportunity.Title,
                    UserId = a.UserID,
                    UserName = a.User.Username,
                    UserEmail = a.User.Email,
                    Status = a.Status.ToString(),
                    AppliedAt = a.AppliedAt,
                    ReviewedAt = a.ReviewedAt,
                    CoverLetter = a.CoverLetter,
                    ReviewNotes = a.ReviewNotes
                })
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting application with ID {applicationId}");
            throw;
        }
    }

    public async Task<bool> UpdateApplicationStatusAsync(int applicationId, UpdateApplicationStatusDTO statusDto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Get the application with related data
            var application = await _context.OpportunityApplications
                .Include(a => a.User)
                .Include(a => a.TalentOpportunity)
                    .ThenInclude(o => o.PostedByUser)
                .FirstOrDefaultAsync(a => a.ApplicationID == applicationId);

            if (application == null)
            {
                _logger.LogWarning($"Application with ID {applicationId} not found");
                return false;
            }

            // Save the old status for comparison
            var oldStatus = application.Status;
            
            // Parse the status string to ApplicationStatus enum
            if (!Enum.TryParse<ApplicationStatus>(statusDto.Status, true, out var newStatus))
            {
                _logger.LogWarning($"Invalid status value: {statusDto.Status}");
                return false;
            }

            // Update the status and review notes
            application.Status = newStatus;
            application.ReviewNotes = statusDto.ReviewNotes;
            
            // If this is a new review, set the reviewed date
            if (oldStatus == ApplicationStatus.Pending && newStatus != ApplicationStatus.Pending)
            {
                application.ReviewedAt = DateTime.UtcNow;
            }

            _context.OpportunityApplications.Update(application);
            var result = await _context.SaveChangesAsync() > 0;

            if (!result)
            {
                _logger.LogWarning($"Failed to update application status for application ID {applicationId}");
                return false;
            }

            // Prepare email notification
            var applicantEmail = application.User?.Email;
            if (!string.IsNullOrEmpty(applicantEmail))
            {
                var applicantName = application.User?.FullName ?? "Applicant";
                var opportunityTitle = application.TalentOpportunity?.Title ?? "the opportunity";
                var companyName = application.TalentOpportunity?.PostedByUser?.FullName ?? "Our company";
                var recruiterName = application.TalentOpportunity?.PostedByUser?.FullName ?? "Our recruitment team";
                var status = application.Status.ToString().ToLower();
                
                string emailSubject;
                string emailBody;

                switch (status)
                {
                    case "pending":
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

                    case "underreview":
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

                    case "accepted":
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
                        emailSubject = $"Update on Your Application for {opportunityTitle}";
                        emailBody = $"""
                                     <h2>Application Status Update</h2>
                                     <p>Dear {applicantName},</p>
                                     <p>This is to inform you that there has been an update to your application for <strong>{opportunityTitle}</strong> at {companyName}.</p>
                                     <p><strong>Status:</strong> {statusDto.Status}</p>
                                     <p>You can check the latest status of your application by logging into your account.</p>
                                     <p>If you have any questions, feel free to reply to this email.</p>
                                     <p>Best regards,<br/>{companyName} Team</p>
                                     """;
                        break;
                }

                // Add review notes if available
                if (!string.IsNullOrEmpty(statusDto.ReviewNotes))
                {
                    emailBody = emailBody.Replace("</p>", "<br/><br/>");
                    emailBody += $"<p><strong>Note from {recruiterName}:</strong><br/>{statusDto.ReviewNotes}</p>";
                }

                // Add a footer to the email
                emailBody += """
                             <hr style="margin: 30px 0; border: none; border-top: 1px solid #eaeaea;"/>
                             <p style="color: #666; font-size: 12px; line-height: 1.5;">
                                 This is an automated message. Please do not reply to this email. If you have any questions, 
                                 please contact us through the contact information provided in your application portal.
                             </p>
                             """;

                // Send email (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendEmailAsync(applicantEmail, emailSubject, emailBody);
                        _logger.LogInformation($"Email notification sent to {applicantEmail} for application ID {applicationId}");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, $"Failed to send email notification to {applicantEmail} for application ID {applicationId}");
                    }
                });
            }

            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, $"Error updating status for application {applicationId}");
            return false;
        }
    }

    public async Task<int> GetOpportunityCountAsync()
    {
        try
        {
            return await _context.TalentOpportunities.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting opportunity count");
            return 0;
        }
    }

    public async Task<int> GetApplicationCountAsync(int opportunityId)
    {
        try
        {
            return await _context.OpportunityApplications
                .CountAsync(a => a.TalentOpportunityID == opportunityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting application count for opportunity {opportunityId}");
            return 0;
        }
    }
}