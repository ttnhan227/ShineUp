using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.DTOs;
using Server.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Security.Claims;

namespace Server.Controllers;

[Authorize]
[ApiController]
[Route("api/social")]
[Produces(MediaTypeNames.Application.Json)]
[Consumes(MediaTypeNames.Application.Json)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class SocialController : ControllerBase
{
    private readonly ILogger<SocialController> _logger;
    private readonly ISocialService _socialService;

    public SocialController(ISocialService socialService, ILogger<SocialController> logger)
    {
        _socialService = socialService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var id))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }

        return id;
    }

    #region Comments

    [HttpGet("posts/{postId}/comments")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetPostComments(
        [FromRoute] [Required] int postId)
    {
        _logger.LogInformation("Getting comments for post {PostId}", postId);

        try
        {
            var comments = await _socialService.GetCommentsForPostAsync(postId);
            if (comments == null || !comments.Any())
            {
                _logger.LogInformation("No comments found for post {PostId}", postId);
                return Ok(Enumerable.Empty<CommentDto>());
            }

            return Ok(comments);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Post {PostId} not found", postId);
            return NotFound(new { message = $"Post with ID {postId} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments for post {PostId}", postId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving comments" });
        }
    }


    [HttpPost("comments")]
    public async Task<ActionResult<CommentDto>> AddComment([FromBody] [Required] CreateCommentDto commentDto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid comment data: {Errors}", ModelState.Values);
            return BadRequest(new { message = "Invalid comment data", errors = ModelState.Values });
        }

        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} is adding a comment", userId);

            var comment = new CommentDto
            {
                PostID = commentDto.PostID,
                VideoID = commentDto.VideoID,
                UserID = userId,
                Content = commentDto.Content?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            var createdComment = await _socialService.AddCommentAsync(comment);
            _logger.LogInformation("Comment {CommentId} added successfully", createdComment?.CommentID);

            return CreatedAtAction(
                nameof(GetPostComments),
                new { postId = createdComment?.PostID },
                createdComment);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { message = "Authentication required" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Post/Video not found");
            return NotFound(new { message = "Post/Video not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while adding the comment" });
        }
    }


    [HttpDelete("comments/{commentId}")]
    public async Task<IActionResult> DeleteComment([FromRoute] [Required] int commentId)
    {
        _logger.LogInformation("Deleting comment {CommentId}", commentId);

        try
        {
            var success = await _socialService.DeleteCommentAsync(commentId);
            if (!success)
            {
                _logger.LogWarning("Comment {CommentId} not found", commentId);
                return NotFound(new { message = "Comment not found" });
            }

            _logger.LogInformation("Comment {CommentId} deleted successfully", commentId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized attempt to delete comment {CommentId}", commentId);
            return Unauthorized(new { message = "You are not authorized to delete this comment" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the comment" });
        }
    }

    #endregion

    #region Likes

    /// <summary>
    ///     Gets all likes for a specific post
    /// </summary>
    /// <param name="postId">The ID of the post</param>
    /// <returns>List of likes for the post</returns>
    /// <response code="200">Returns the list of likes</response>
    /// <response code="404">If the post is not found</response>
    /// <response code="500">If there was an error retrieving the likes</response>
    [HttpGet("posts/{postId}/likes")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<LikeDto>>> GetPostLikes(
        [FromRoute] [Required] int postId)
    {
        _logger.LogInformation("Getting likes for post {PostId}", postId);

        try
        {
            var likes = await _socialService.GetLikesForPostAsync(postId);
            if (likes == null || !likes.Any())
            {
                _logger.LogInformation("No likes found for post {PostId}", postId);
                return Ok(Enumerable.Empty<LikeDto>());
            }

            return Ok(likes);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Post {PostId} not found", postId);
            return NotFound(new { message = $"Post with ID {postId} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting likes for post {PostId}", postId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving likes" });
        }
    }

    /// <summary>
    ///     Toggles a like on a post or video
    /// </summary>
    /// <param name="likeDto">The like data</param>
    /// <returns>The like if added, or NoContent if removed</returns>
    /// <response code="200">If the like was added</response>
    /// <response code="204">If the like was removed</response>
    /// <response code="400">If the like data is invalid</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="404">If the post/video is not found</response>
    /// <response code="500">If there was an error toggling the like</response>
    [HttpPost("likes/toggle")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LikeDto>> ToggleLike([FromBody] [Required] CreateLikeDto likeDto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid like data: {Errors}", ModelState.Values);
            return BadRequest(new { message = "Invalid like data", errors = ModelState.Values });
        }

        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} is toggling like for Post: {PostId}, Video: {VideoId}",
                userId, likeDto.PostID, likeDto.VideoID);

            var like = new LikeDto
            {
                PostID = likeDto.PostID,
                VideoID = likeDto.VideoID,
                UserID = userId
            };

            var result = await _socialService.ToggleLikeAsync(like);

            if (result == null)
            {
                _logger.LogInformation("Like removed by user {UserId}", userId);
                return NoContent();
            }

            _logger.LogInformation("Like added by user {UserId}", userId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { message = "Authentication required" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Post/Video not found");
            return NotFound(new { message = "Post/Video not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling like");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while toggling the like" });
        }
    }

    /// <summary>
    ///     Checks if the current user has liked a post
    /// </summary>
    /// <param name="postId">The ID of the post</param>
    /// <returns>True if the user has liked the post, false otherwise</returns>
    /// <response code="200">Returns the like status</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="404">If the post is not found</response>
    /// <response code="500">If there was an error checking the like status</response>
    [HttpGet("posts/{postId}/has-liked")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<bool>> HasLikedPost(
        [FromRoute] [Required] int postId)
    {
        _logger.LogInformation("Checking if user has liked post {PostId}", postId);

        try
        {
            var userId = GetCurrentUserId();
            var hasLiked = await _socialService.HasLikedPostAsync(postId, userId);
            return Ok(hasLiked);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { message = "Authentication required" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Post {PostId} not found", postId);
            return NotFound(new { message = $"Post with ID {postId} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user liked post {PostId}", postId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while checking like status" });
        }
    }

    /// <summary>
    ///     Gets the like count for a post
    /// </summary>
    /// <param name="postId">The ID of the post</param>
    /// <returns>The number of likes for the post</returns>
    /// <response code="200">Returns the like count</response>
    /// <response code="404">If the post is not found</response>
    /// <response code="500">If there was an error getting the like count</response>
    [HttpGet("posts/{postId}/like-count")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<int>> GetPostLikeCount(
        [FromRoute] [Required] int postId)
    {
        _logger.LogInformation("Getting like count for post {PostId}", postId);

        try
        {
            var count = await _socialService.GetLikeCountAsync(postId);
            return Ok(count);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Post {PostId} not found", postId);
            return NotFound(new { message = $"Post with ID {postId} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting like count for post {PostId}", postId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while getting like count" });
        }
    }

    #endregion
}