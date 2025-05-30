using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;
using System.Security.Claims;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly IPostRepository _postRepository;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(
        IPostRepository postRepository,
        ILogger<CommentsController> logger)
    {
        _postRepository = postRepository;
        _logger = logger;
    }

    // GET: api/comments/post/5
    [HttpGet("post/{postId}")]
    [AllowAnonymous]  // Allow anonymous access to read comments
    public async Task<ActionResult<IEnumerable<CommentDTO>>> GetCommentsForPost(int postId)
    {
        try
        {
            if (!await _postRepository.PostExistsAsync(postId))
                return NotFound("Post not found");

            var comments = await _postRepository.GetCommentsForPostAsync(postId);
            var commentDtos = comments.Select(c => new CommentDTO
            {
                CommentID = c.CommentID,
                PostID = c.PostID,
                VideoID = c.VideoID,
                UserID = c.UserID,
                Username = c.User?.Username ?? "Unknown",
                ProfileImageURL = c.User?.ProfileImageURL,
                Content = c.Content,
                CreatedAt = c.CreatedAt
            });

            return Ok(commentDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments for post {PostId}", postId);
            return StatusCode(500, "Internal server error while retrieving comments");
        }
    }

    // POST: api/comments
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<CommentDTO>> CreateComment([FromBody] CreateCommentDTO createCommentDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (createCommentDto.PostID == null && createCommentDto.VideoID == null)
                return BadRequest("Either PostID or VideoID must be provided");

            if (createCommentDto.PostID != null && !await _postRepository.PostExistsAsync(createCommentDto.PostID.Value))
                return NotFound("Post not found");

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var comment = new Comment
            {
                PostID = createCommentDto.PostID,
                VideoID = createCommentDto.VideoID,
                UserID = userId,
                Content = createCommentDto.Content,
                CreatedAt = DateTime.UtcNow
            };

            var createdComment = await _postRepository.AddCommentToPostAsync(comment);
            
            // Reload the comment with user data
            var commentWithUser = await _postRepository.GetCommentByIdAsync(createdComment.CommentID);
            
            var commentDto = new CommentDTO
            {
                CommentID = commentWithUser.CommentID,
                PostID = commentWithUser.PostID,
                VideoID = commentWithUser.VideoID,
                UserID = commentWithUser.UserID,
                Username = commentWithUser.User?.Username ?? "Unknown",
                ProfileImageURL = commentWithUser.User?.ProfileImageURL,
                Content = commentWithUser.Content,
                CreatedAt = commentWithUser.CreatedAt
            };

            return CreatedAtAction(
                nameof(GetCommentsForPost),
                new { postId = commentDto.PostID },
                commentDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating comment");
            return StatusCode(500, "Internal server error while creating comment");
        }
    }

    // DELETE: api/comments/5
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteComment(int id)
    {
        try
        {
            var comment = await _postRepository.GetCommentByIdAsync(id);
            if (comment == null)
                return NotFound("Comment not found");

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            // Only the comment owner or post owner can delete the comment
            if (comment.UserID != userId)
            {
                if (comment.PostID.HasValue)
                {
                    var post = await _postRepository.GetPostByIdAsync(comment.PostID.Value);
                    if (post?.UserID != userId)
                        return Forbid();
                }
                else
                {
                    // For video comments, only the comment owner can delete
                    return Forbid();
                }
            }

            var success = await _postRepository.DeleteCommentAsync(id, userId);
            if (!success)
                return NotFound("Comment not found or you don't have permission to delete it");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment {CommentId}", id);
            return StatusCode(500, "Internal server error while deleting comment");
        }
    }
}
