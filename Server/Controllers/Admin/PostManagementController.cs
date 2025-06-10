using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.DTOs.Admin;
using Server.Interfaces.Admin;

namespace Server.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("api/admin/[controller]")]
[ApiController]
public class PostManagementController : ControllerBase
{
    private const int ADMIN_ROLE_ID = 2;
    private readonly ILogger<PostManagementController> _logger;
    private readonly IPostManagementRepository _postRepository;

    public PostManagementController(
        IPostManagementRepository postRepository,
        ILogger<PostManagementController> logger)
    {
        _postRepository = postRepository;
        _logger = logger;
    }

    private bool IsAdmin()
    {
        var roleIdClaim = User.FindFirst("RoleID");
        return roleIdClaim != null && int.TryParse(roleIdClaim.Value, out var roleId) && roleId == ADMIN_ROLE_ID;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminPostDTO>>> GetAllPosts()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        try
        {
            var posts = await _postRepository.GetAllPostsAsync();
            return Ok(posts);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting all posts: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{postId}")]
    public async Task<ActionResult<AdminPostDTO>> GetPost(int postId)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        try
        {
            var post = await _postRepository.GetPostByIdAsync(postId);
            if (post == null)
            {
                return NotFound(new { message = "Post not found" });
            }

            return Ok(post);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting post with ID {postId}: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("status/{postId}")]
    public async Task<IActionResult> UpdatePostStatus(int postId, [FromBody] UpdatePostStatusDTO statusDto)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        if (postId != statusDto.PostID)
        {
            return BadRequest(new { message = "Post ID mismatch" });
        }


        try
        {
            var success = await _postRepository.UpdatePostStatusAsync(postId, statusDto.IsActive);
            if (!success)
            {
                return NotFound(new { message = "Post not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating status for post ID {postId}: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{postId}")]
    public async Task<IActionResult> DeletePost(int postId)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        try
        {
            var success = await _postRepository.DeletePostAsync(postId);
            if (!success)
            {
                return NotFound(new { message = "Post not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting post with ID {postId}: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}