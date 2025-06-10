using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;
using System.Security.Claims;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LikesController : ControllerBase
{
    private readonly ILogger<LikesController> _logger;
    private readonly IPostRepository _postRepository;

    public LikesController(
        IPostRepository postRepository,
        ILogger<LikesController> logger)
    {
        _postRepository = postRepository;
        _logger = logger;
    }

    // GET: api/likes/post/5
    [HttpGet("post/{postId}")]
    [AllowAnonymous] // Allow anonymous access to see likes
    public async Task<ActionResult<IEnumerable<LikeDTO>>> GetLikesForPost(int postId)
    {
        try
        {
            if (!await _postRepository.PostExistsAsync(postId))
            {
                return NotFound("Post not found");
            }

            var likes = await _postRepository.GetLikesForPostAsync(postId);
            var likeDtos = likes.Select(l => new LikeDTO
            {
                LikeID = l.LikeID,
                PostID = l.PostID,
                VideoID = l.VideoID,
                UserID = l.UserID,
                Username = l.User?.Username ?? "Unknown",
                ProfileImageURL = l.User?.ProfileImageURL,
                CreatedAt = l.CreatedAt
            });

            return Ok(likeDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting likes for post {PostId}", postId);
            return StatusCode(500, "Internal server error while retrieving likes");
        }
    }

    // GET: api/likes/post/5/count
    [HttpGet("post/{postId}/count")]
    [AllowAnonymous] // Allow anonymous access to see like counts
    public async Task<ActionResult<int>> GetLikeCountForPost(int postId)
    {
        try
        {
            if (!await _postRepository.PostExistsAsync(postId))
            {
                return NotFound("Post not found");
            }

            var count = await _postRepository.GetPostLikesCountAsync(postId);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting like count for post {PostId}", postId);
            return StatusCode(500, "Internal server error while retrieving like count");
        }
    }

    // GET: api/likes/post/5/status
    [Authorize] // Keep this endpoint authenticated
    [HttpGet("post/{postId}/status")]
    public async Task<ActionResult<bool>> HasUserLikedPost(int postId)
    {
        try
        {
            if (!await _postRepository.PostExistsAsync(postId))
            {
                return NotFound("Post not found");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var hasLiked = await _postRepository.HasUserLikedPostAsync(postId, userId);

            return Ok(hasLiked);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user liked post {PostId}", postId);
            return StatusCode(500, "Internal server error while checking like status");
        }
    }

    // POST: api/likes/toggle
    [Authorize]
    [HttpPost("toggle")]
    public async Task<ActionResult<LikeDTO>> ToggleLike([FromBody] CreateLikeDTO createLikeDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (createLikeDto.PostID == null && createLikeDto.VideoID == null)
            {
                return BadRequest("Either PostID or VideoID must be provided");
            }

            if (createLikeDto.PostID != null && !await _postRepository.PostExistsAsync(createLikeDto.PostID.Value))
            {
                return NotFound("Post not found");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Check if the user already liked the post/video
            bool hasLiked;
            if (createLikeDto.PostID.HasValue)
            {
                hasLiked = await _postRepository.HasUserLikedPostAsync(createLikeDto.PostID.Value, userId);

                if (hasLiked)
                {
                    // Unlike the post
                    var success = await _postRepository.UnlikePostAsync(createLikeDto.PostID.Value, userId);
                    if (!success)
                    {
                        return NotFound("Like not found");
                    }

                    return NoContent();
                }

                // Like the post
                var like = new Like
                {
                    PostID = createLikeDto.PostID,
                    VideoID = createLikeDto.VideoID,
                    UserID = userId,
                    CreatedAt = DateTime.UtcNow
                };

                var createdLike = await _postRepository.LikePostAsync(like);

                // Get the like with user data
                var likes = await _postRepository.GetLikesForPostAsync(createdLike.PostID.Value);
                var createdLikeWithUser = likes.FirstOrDefault(l => l.LikeID == createdLike.LikeID);

                var likeDto = new LikeDTO
                {
                    LikeID = createdLikeWithUser.LikeID,
                    PostID = createdLikeWithUser.PostID,
                    VideoID = createdLikeWithUser.VideoID,
                    UserID = createdLikeWithUser.UserID,
                    Username = createdLikeWithUser.User?.Username ?? "Unknown",
                    ProfileImageURL = createdLikeWithUser.User?.ProfileImageURL,
                    CreatedAt = createdLikeWithUser.CreatedAt
                };

                return Ok(likeDto);
            }

            // Handle video likes (if needed)
            return BadRequest("Video likes are not implemented yet");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling like");
            return StatusCode(500, "Internal server error while toggling like");
        }
    }
}