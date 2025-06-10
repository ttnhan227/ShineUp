using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;
using System.Security.Claims;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContestEntriesController : ControllerBase
{
    private readonly ICloudinaryService _cloudinaryService;
    private readonly DatabaseContext _context;
    private readonly ILogger<ContestEntriesController> _logger;
    private readonly IContestEntryRepositories _repository;

    public ContestEntriesController(
        IContestEntryRepositories repository,
        DatabaseContext context,
        ICloudinaryService cloudinaryService,
        ILogger<ContestEntriesController> logger)
    {
        _repository = repository;
        _context = context;
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    [Authorize]
    [HttpPost("upload")]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100MB limit
    public async Task<IActionResult> UploadEntry()
    {
        try
        {
            if (!Request.HasFormContentType)
            {
                return BadRequest("Request content type is not multipart/form-data");
            }

            var form = await Request.ReadFormAsync();
            var file = form.Files.GetFile("file");

            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            // Parse form data
            if (!int.TryParse(form["ContestID"], out var contestId) ||
                !int.TryParse(form["UserID"], out var userId) ||
                string.IsNullOrEmpty(form["MediaType"]))
            {
                return BadRequest("Invalid form data");
            }

            var mediaType = form["MediaType"].ToString().ToLower();
            var title = form["Title"].ToString();
            var description = form["Description"].ToString();

            // Validate media type
            if (mediaType != "image" && mediaType != "video")
            {
                return BadRequest("Invalid media type. Must be 'image' or 'video'");
            }

            // Check contest exists and is active
            var contest = await _context.Contests.FindAsync(contestId);
            if (contest == null || DateTime.UtcNow < contest.StartDate || DateTime.UtcNow > contest.EndDate)
            {
                return BadRequest("This contest is not active.");
            }

            // Check if user has already submitted to this contest
            if (await _repository.HasSubmittedAsync(contestId, userId))
            {
                return BadRequest("You have already submitted to this contest.");
            }

            // Upload file to Cloudinary
            dynamic uploadResult;
            string publicId;
            string mediaUrl;

            if (mediaType == "image")
            {
                var imageResult = await _cloudinaryService.UploadImgAsync(file);
                uploadResult = imageResult;
                publicId = imageResult.PublicId;
                mediaUrl = imageResult.SecureUrl.ToString();
            }
            else // video
            {
                var videoResult = await _cloudinaryService.UploadVideoAsync(file);
                uploadResult = videoResult;
                publicId = videoResult.PublicId;
                mediaUrl = videoResult.SecureUrl.ToString();
            }

            // Create Video or Image record first
            string? videoId = null;
            string? imageId = null;

            if (mediaType == "image")
            {
                var image = new Image
                {
                    ImageID = publicId,
                    CloudPublicId = publicId,
                    Title = title,
                    Description = description,
                    ImageURL = mediaUrl,
                    UserID = userId,
                    UploadDate = DateTime.UtcNow,
                    // Set default privacy or get from request if available
                    PrivacyID = 1 // Default privacy ID, adjust as needed
                };

                await _context.Images.AddAsync(image);
                await _context.SaveChangesAsync();
                imageId = image.ImageID;
            }
            else
            {
                var video = new Video
                {
                    VideoID = publicId,
                    CloudPublicId = publicId,
                    Title = title,
                    Description = description,
                    VideoURL = mediaUrl,
                    UserID = userId,
                    UploadDate = DateTime.UtcNow,
                    // Set default values
                    ViewCount = 0,
                    SkillLevel = "Beginner",
                    PrivacyID = 1, // Default privacy ID, adjust as needed
                    CategoryID = 1 // Default category ID, adjust as needed or make it required in the request
                };

                await _context.Videos.AddAsync(video);
                await _context.SaveChangesAsync();
                videoId = video.VideoID;
            }

            // Create contest entry
            var entry = new ContestEntry
            {
                ContestID = contestId,
                UserID = userId,
                SubmissionDate = DateTime.UtcNow,
                Title = title,
                Description = description,
                ImageID = imageId,
                VideoID = videoId,
                MediaType = mediaType
            };

            await _repository.AddAsync(entry);

            // Return the created entry
            var dto = new ContestEntryDTO
            {
                EntryID = entry.EntryID,
                ContestID = entry.ContestID,
                UserID = entry.UserID,
                ImageID = entry.ImageID,
                VideoID = entry.VideoID,
                MediaType = entry.MediaType,
                MediaUrl = mediaUrl,
                Title = entry.Title,
                Description = entry.Description,
                SubmissionDate = entry.SubmissionDate
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading contest entry");
            return StatusCode(500, "An error occurred while processing your request");
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] ContestEntryDTO dto)
    {
        // Validate that either VideoID or ImageID is provided, but not both
        if (string.IsNullOrEmpty(dto.VideoID) && string.IsNullOrEmpty(dto.ImageID))
        {
            return BadRequest("Either VideoID or ImageID must be provided.");
        }

        if (!string.IsNullOrEmpty(dto.VideoID) && !string.IsNullOrEmpty(dto.ImageID))
        {
            return BadRequest("Cannot provide both VideoID and ImageID. Choose one.");
        }

        var contest = await _context.Contests.FindAsync(dto.ContestID);
        if (contest == null || DateTime.UtcNow < contest.StartDate || DateTime.UtcNow > contest.EndDate)
        {
            return BadRequest("This contest is not active.");
        }

        if (await _repository.HasSubmittedAsync(dto.ContestID, dto.UserID))
        {
            return BadRequest("You have already submitted to this contest.");
        }

        // Verify the media exists and belongs to the user
        if (!string.IsNullOrEmpty(dto.VideoID))
        {
            var video = await _context.Videos.FirstOrDefaultAsync(v =>
                v.VideoID == dto.VideoID && v.UserID == dto.UserID);
            if (video == null)
            {
                return BadRequest("Video not found or you don't have permission to use it.");
            }
        }
        else if (!string.IsNullOrEmpty(dto.ImageID))
        {
            var image = await _context.Images.FirstOrDefaultAsync(i =>
                i.ImageID == dto.ImageID && i.UserID == dto.UserID);
            if (image == null)
            {
                return BadRequest("Image not found or you don't have permission to use it.");
            }
        }

        var entity = new ContestEntry
        {
            ContestID = dto.ContestID,
            UserID = dto.UserID,
            VideoID = dto.VideoID,
            ImageID = dto.ImageID,
            SubmissionDate = DateTime.UtcNow
        };

        await _repository.AddAsync(entity);

        // Map back to DTO with additional info
        dto.EntryID = entity.EntryID;
        dto.SubmissionDate = entity.SubmissionDate;
        dto.MediaUrl = entity.MediaUrl;
        dto.MediaType = entity.MediaType;

        return Ok(dto);
    }

    [HttpGet("contest/{contestId}")]
    public async Task<IActionResult> GetByContest(int contestId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var entries = await _repository.GetEntriesByContestAsync(contestId);

        // Get all entry IDs for batch voting check
        var entryIds = entries.Select(e => e.EntryID).ToList();
        var userVotes = await _context.Votes
            .Where(v => v.UserID == userId && entryIds.Contains(v.EntryID))
            .Select(v => v.EntryID)
            .ToListAsync();

        // Get vote counts for all entries
        var voteCounts = await _context.Votes
            .Where(v => entryIds.Contains(v.EntryID))
            .GroupBy(v => v.EntryID)
            .Select(g => new { EntryID = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EntryID, x => x.Count);

        // Map to DTOs with media info and voting data
        var dtos = entries.Select(e =>
        {
            var dto = new ContestEntryDTO
            {
                EntryID = e.EntryID,
                ContestID = e.ContestID,
                UserID = e.UserID,
                UserName = e.User?.Username ?? "Unknown User",
                UserAvatar = e.User?.ProfileImageURL,
                VideoID = e.VideoID,
                ImageID = e.ImageID,
                SubmissionDate = e.SubmissionDate,
                MediaUrl = e.MediaUrl ?? (e.Video != null ? e.Video.VideoURL : e.Image?.ImageURL),
                MediaType = e.MediaType ?? (e.VideoID != null ? "video" : "image"),
                Title = !string.IsNullOrEmpty(e.Title)
                    ? e.Title
                    : (e.Video != null ? e.Video.Title : e.Image?.Title) ?? "Untitled",
                Description = !string.IsNullOrEmpty(e.Description) ? e.Description :
                    e.Video != null ? e.Video.Description : e.Image?.Description,
                VoteCount = voteCounts.GetValueOrDefault(e.EntryID, 0),
                HasVoted = userVotes.Contains(e.EntryID)
            };
            return dto;
        });

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEntry(int id)
    {
        var entry = await _repository.GetByIdAsync(id);
        if (entry == null)
        {
            return NotFound();
        }

        var dto = new ContestEntryDTO
        {
            EntryID = entry.EntryID,
            ContestID = entry.ContestID,
            UserID = entry.UserID,
            UserName = entry.User.Username,
            UserAvatar = entry.User.ProfileImageURL,
            VideoID = entry.VideoID,
            ImageID = entry.ImageID,
            SubmissionDate = entry.SubmissionDate,
            MediaUrl = entry.MediaUrl,
            MediaType = entry.MediaType,
            Title = entry.Video != null ? entry.Video.Title : entry.Image?.Title,
            Description = entry.Video != null ? entry.Video.Description : entry.Image?.Description
        };

        return Ok(dto);
    }

    [HttpGet("user/{userId}/contest/{contestId}")]
    public async Task<IActionResult> CheckUserSubmission(int userId, int contestId)
    {
        var exists = await _repository.HasSubmittedAsync(contestId, userId);
        return Ok(new { hasSubmitted = exists });
    }
}