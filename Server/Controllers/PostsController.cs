using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;
using System.Security.Claims;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly IPostRepository _postRepository;
    private readonly ILogger<PostsController> _logger;
    private readonly ICloudinaryService _cloudinaryService;

    public PostsController(
        IPostRepository postRepository, 
        ILogger<PostsController> logger,
        ICloudinaryService cloudinaryService)
    {
        _postRepository = postRepository;
        _logger = logger;
        _cloudinaryService = cloudinaryService;
    }

    // GET: api/posts
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PostListResponseDto>>> GetPosts()
    {
        try
        {
            var posts = await _postRepository.GetAllPostsAsync();
            var postDtos = posts.Select(p => new PostListResponseDto
            {
                PostID = p.PostID,
                Title = p.Title,
                Content = p.Content,
                ImageURL = p.ImageURL,
                CreatedAt = p.CreatedAt,
                UserID = p.UserID,
                UserName = p.User.Username,
                CategoryName = p.Category?.CategoryName,
                LikesCount = p.Likes?.Count ?? 0,
                CommentsCount = p.Comments?.Count ?? 0
            });

            return Ok(postDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all posts");
            return StatusCode(500, "Internal server error");
        }
    }

    // GET: api/posts/5
    [HttpGet("{id}")]
    public async Task<ActionResult<PostResponseDto>> GetPost(int id)
    {
        try
        {
            var post = await _postRepository.GetPostByIdAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            var postDto = new PostResponseDto
            {
                PostID = post.PostID,
                Title = post.Title,
                Content = post.Content,
                ImageURL = post.ImageURL,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                UserID = post.UserID,
                Username = post.User.Username,
                CategoryID = post.CategoryID,
                CategoryName = post.Category?.CategoryName,
                PrivacyID = post.PrivacyID,
                PrivacyName = post.Privacy?.Name,
                LikesCount = post.Likes?.Count ?? 0,
                CommentsCount = post.Comments?.Count ?? 0
            };

            return Ok(postDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting post {PostId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    // POST: api/posts
    [Authorize]
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PostResponseDto>> CreatePost([FromForm] CreatePostDto createPostDto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("Invalid user ID");
            }

            string? imageUrl = null;
            if (createPostDto.Image != null)
            {
                var uploadResult = await _cloudinaryService.UploadImgAsync(createPostDto.Image);
                if (uploadResult.Error != null)
                {
                    _logger.LogError($"Cloudinary image upload error: {uploadResult.Error.Message}");
                    return BadRequest(new { message = "Image upload failed: " + uploadResult.Error.Message });
                }
                imageUrl = uploadResult.SecureUrl.ToString();
            }
            
            var post = new Post
            {
                Title = createPostDto.Title,
                Content = createPostDto.Content,
                ImageURL = imageUrl,
                CategoryID = createPostDto.CategoryID,
                PrivacyID = createPostDto.PrivacyID,
                UserID = userId,
                CreatedAt = DateTime.UtcNow
            };

            var createdPost = await _postRepository.CreatePostAsync(post);
            
            var postDto = new PostResponseDto
            {
                PostID = createdPost.PostID,
                Title = createdPost.Title,
                Content = createdPost.Content,
                ImageURL = createdPost.ImageURL,
                CreatedAt = createdPost.CreatedAt,
                UserID = createdPost.UserID,
                Username = createdPost.User.Username,
                CategoryID = createdPost.CategoryID,
                CategoryName = createdPost.Category?.CategoryName,
                PrivacyID = createdPost.PrivacyID,
                PrivacyName = createdPost.Privacy?.Name,
                LikesCount = 0,
                CommentsCount = 0
            };

            return CreatedAtAction(nameof(GetPost), new { id = postDto.PostID }, postDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post");
            return StatusCode(500, "Internal server error");
        }
    }

    // PUT: api/posts/5
    [Authorize]
    [HttpPut("{id}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdatePost(int id, [FromForm] UpdatePostDto updatePostDto)
    {
        try
        {
            var post = await _postRepository.GetPostByIdAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("Invalid user ID");
            }

            if (post.UserID != userId)
            {
                return Forbid();
            }

            // Update only the fields that are provided
            if (updatePostDto.Title != null)
                post.Title = updatePostDto.Title;
            if (updatePostDto.Content != null)
                post.Content = updatePostDto.Content;
            if (updatePostDto.CategoryID.HasValue)
                post.CategoryID = updatePostDto.CategoryID;
            if (updatePostDto.PrivacyID.HasValue)
                post.PrivacyID = updatePostDto.PrivacyID;

            // Handle image upload if present
            if (updatePostDto.Image != null)
            {
                var uploadResult = await _cloudinaryService.UploadImgAsync(updatePostDto.Image);
                if (uploadResult.Error != null)
                {
                    _logger.LogError($"Cloudinary image upload error: {uploadResult.Error.Message}");
                    return BadRequest(new { message = "Image upload failed: " + uploadResult.Error.Message });
                }
                post.ImageURL = uploadResult.SecureUrl.ToString();
            }
            else if (updatePostDto.ImageURL != null)
            {
                post.ImageURL = updatePostDto.ImageURL;
            }

            await _postRepository.UpdatePostAsync(post);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating post {PostId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    // DELETE: api/posts/5
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePost(int id)
    {
        try
        {
            var post = await _postRepository.GetPostByIdAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("Invalid user ID");
            }

            if (post.UserID != userId)
            {
                return Forbid();
            }

            await _postRepository.DeletePostAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post {PostId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    // GET: api/posts/user/5
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<PostListResponseDto>>> GetUserPosts(int userId)
    {
        try
        {
            var posts = await _postRepository.GetPostsByUserIdAsync(userId);
            var postDtos = posts.Select(p => new PostListResponseDto
            {
                PostID = p.PostID,
                Title = p.Title,
                Content = p.Content,
                ImageURL = p.ImageURL,
                CreatedAt = p.CreatedAt,
                UserID = p.UserID,
                UserName = p.User.Username,
                CategoryName = p.Category?.CategoryName,
                LikesCount = p.Likes?.Count ?? 0,
                CommentsCount = p.Comments?.Count ?? 0
            });

            return Ok(postDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting posts for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    // GET: api/posts/category/5
    [HttpGet("category/{categoryId}")]
    public async Task<ActionResult<IEnumerable<PostListResponseDto>>> GetPostsByCategory(int categoryId)
    {
        try
        {
            var posts = await _postRepository.GetPostsByCategoryAsync(categoryId);
            var postDtos = posts.Select(p => new PostListResponseDto
            {
                PostID = p.PostID,
                Title = p.Title,
                Content = p.Content,
                ImageURL = p.ImageURL,
                CreatedAt = p.CreatedAt,
                UserID = p.UserID,
                UserName = p.User.Username,
                CategoryName = p.Category?.CategoryName,
                LikesCount = p.Likes?.Count ?? 0,
                CommentsCount = p.Comments?.Count ?? 0
            });

            return Ok(postDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting posts for category {CategoryId}", categoryId);
            return StatusCode(500, "Internal server error");
        }
    }

    // GET: api/posts/recent/5
    [HttpGet("recent/{count}")]
    public async Task<ActionResult<IEnumerable<PostListResponseDto>>> GetRecentPosts(int count)
    {
        try
        {
            var posts = await _postRepository.GetRecentPostsAsync(count);
            var postDtos = posts.Select(p => new PostListResponseDto
            {
                PostID = p.PostID,
                Title = p.Title,
                Content = p.Content,
                ImageURL = p.ImageURL,
                CreatedAt = p.CreatedAt,
                UserID = p.UserID,
                UserName = p.User.Username,
                CategoryName = p.Category?.CategoryName,
                LikesCount = p.Likes?.Count ?? 0,
                CommentsCount = p.Comments?.Count ?? 0
            });

            return Ok(postDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent posts");
            return StatusCode(500, "Internal server error");
        }
    }
} 