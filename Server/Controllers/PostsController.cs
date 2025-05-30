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
            int? userId = null;
            if (User.Identity.IsAuthenticated)
            {
                userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            }

            var posts = await _postRepository.GetVisiblePostsAsync(userId);
            var postDtos = posts.Select(p => new PostListResponseDto
            {
                PostID = p.PostID,
                Title = p.Title,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                UserID = p.UserID,
                UserName = p.User.Username,
                FullName = p.User.FullName,
                CategoryName = p.Category?.CategoryName,
                LikesCount = p.Likes?.Count ?? 0,
                CommentsCount = p.Comments?.Count ?? 0,
                MediaFiles = p.Images.Select(i => new MediaFileDTO
                {
                    Url = i.ImageURL?.Replace("http://", "https://"),
                    Type = "image",
                    PublicId = i.CloudPublicId
                }).Concat(p.Videos.Select(v => new MediaFileDTO
                {
                    Url = v.VideoURL?.Replace("http://", "https://"),
                    Type = "video",
                    PublicId = v.CloudPublicId
                })).ToList()
            });

            return Ok(postDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting posts");
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

            // Check if user is authenticated
            int? currentUserId = null;
            if (User.Identity.IsAuthenticated)
            {
                currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            }

            // Check privacy settings
            if (post.Privacy.Name != "Public" && post.UserID != currentUserId)
            {
                return Forbid();
            }

            var postDto = new PostResponseDto
            {
                PostID = post.PostID,
                Title = post.Title,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                UserID = post.UserID,
                Username = post.User.Username,
                FullName = post.User.FullName,
                CategoryID = post.CategoryID,
                CategoryName = post.Category?.CategoryName,
                PrivacyID = post.PrivacyID,
                PrivacyName = post.Privacy?.Name,
                LikesCount = post.Likes?.Count ?? 0,
                CommentsCount = post.Comments?.Count ?? 0,
                MediaFiles = post.Images.Select(i => new MediaFileDTO
                {
                    Url = i.ImageURL?.Replace("http://", "https://"),
                    Type = "image",
                    PublicId = i.CloudPublicId
                }).Concat(post.Videos.Select(v => new MediaFileDTO
                {
                    Url = v.VideoURL?.Replace("http://", "https://"),
                    Type = "video",
                    PublicId = v.CloudPublicId
                })).ToList()
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
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _postRepository.GetUserByIdAsync(userId);

            if (user == null)
            {
                return Unauthorized();
            }

            var post = new Post
            {
                Title = string.IsNullOrEmpty(createPostDto.Title) ? 
                        (createPostDto.Content.Length > 50 ? createPostDto.Content.Substring(0, 50) + "..." : createPostDto.Content) :
                        createPostDto.Title,
                Content = createPostDto.Content,
                UserID = userId,
                CategoryID = createPostDto.CategoryID,
                PrivacyID = createPostDto.PrivacyID ?? 1,
                CreatedAt = DateTime.UtcNow
            };

            await _postRepository.CreatePostAsync(post);

            // Handle media files
            var formFiles = Request.Form.Files;
            var mediaTypes = Request.Form["MediaTypes"].ToList();

            for (int i = 0; i < formFiles.Count; i++)
            {
                var file = formFiles[i];
                var mediaType = mediaTypes[i];

                if (file.Length > 0)
                {
                    // Upload to Cloudinary
                    if (mediaType == "image")
                    {
                        var uploadResult = await _cloudinaryService.UploadImgAsync(file);
                        if (uploadResult != null)
                        {
                            var image = new Image
                            {
                                PostID = post.PostID,
                                ImageID = uploadResult.PublicId,
                                UserID = userId,
                                ImageURL = uploadResult.Url.ToString(),
                                CloudPublicId = uploadResult.PublicId,
                                UploadDate = DateTime.UtcNow,
                                Title = post.Title,
                                Description = post.Content,
                                CategoryID = post.CategoryID ?? 1,
                                PrivacyID = post.PrivacyID ?? 1
                            };
                            await _postRepository.AddImageAsync(image);
                        }
                    }
                    else if (mediaType == "video")
                    {
                        var uploadResult = await _cloudinaryService.UploadVideoAsync(file);
                        if (uploadResult != null)
                        {
                            var video = new Video
                            {
                                PostID = post.PostID,
                                VideoID = uploadResult.PublicId,
                                UserID = userId,
                                VideoURL = uploadResult.Url.ToString(),
                                CloudPublicId = uploadResult.PublicId,
                                UploadDate = DateTime.UtcNow,
                                Title = post.Title,
                                Description = post.Content,
                                CategoryID = post.CategoryID ?? 1,
                                PrivacyID = post.PrivacyID ?? 1,
                                ViewCount = 0,
                                SkillLevel = "Beginner",
                                Location = "Unknown"
                            };
                            await _postRepository.AddVideoAsync(video);
                        }
                    }
                }
            }

            return CreatedAtAction(nameof(GetPost), new { id = post.PostID }, new PostResponseDto
            {
                PostID = post.PostID,
                Title = post.Title,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                UserID = post.UserID,
                Username = user.Username,
                FullName = user.FullName,
                CategoryID = post.CategoryID,
                CategoryName = post.Category?.CategoryName,
                PrivacyID = post.PrivacyID,
                PrivacyName = post.Privacy?.Name,
                LikesCount = 0,
                CommentsCount = 0,
                MediaFiles = new List<MediaFileDTO>() // We'll populate this in the GetPost action
            });
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

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (post.UserID != userId)
            {
                return Forbid();
            }

            // Update basic post information
            if (!string.IsNullOrEmpty(updatePostDto.Title))
                post.Title = updatePostDto.Title;
            if (!string.IsNullOrEmpty(updatePostDto.Content))
                post.Content = updatePostDto.Content;
            if (updatePostDto.CategoryID.HasValue)
                post.CategoryID = updatePostDto.CategoryID;
            if (updatePostDto.PrivacyID.HasValue)
                post.PrivacyID = updatePostDto.PrivacyID;

            // Handle media files
            if (updatePostDto.MediaFiles != null && updatePostDto.MediaFiles.Any())
            {
                // Remove existing media files
                await _postRepository.RemoveAllMediaFromPostAsync(post.PostID);

                // Add new media files
                foreach (var mediaFile in updatePostDto.MediaFiles)
                {
                    if (mediaFile.Type == "image")
                    {
                        var image = new Image
                        {
                            PostID = post.PostID,
                            ImageID = mediaFile.PublicId,
                            UserID = userId,
                            ImageURL = mediaFile.Url,
                            CloudPublicId = mediaFile.PublicId,
                            UploadDate = DateTime.UtcNow
                        };
                        await _postRepository.AddImageAsync(image);
                    }
                    else if (mediaFile.Type == "video")
                    {
                        var video = new Video
                        {
                            PostID = post.PostID,
                            VideoID = mediaFile.PublicId,
                            UserID = userId,
                            VideoURL = mediaFile.Url,
                            CloudPublicId = mediaFile.PublicId,
                            UploadDate = DateTime.UtcNow
                        };
                        await _postRepository.AddVideoAsync(video);
                    }
                }
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

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (post.UserID != userId)
            {
                return Forbid();
            }

            // Delete media files from Cloudinary
            foreach (var image in post.Images)
            {
                await _cloudinaryService.DeleteMediaAsync(image.CloudPublicId);
            }
            foreach (var video in post.Videos)
            {
                await _cloudinaryService.DeleteMediaAsync(video.CloudPublicId);
            }

            await _postRepository.DeletePostAsync(post);
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
                CreatedAt = p.CreatedAt,
                UserID = p.UserID,
                UserName = p.User.Username,
                FullName = p.User.FullName,
                CategoryName = p.Category?.CategoryName,
                LikesCount = p.Likes?.Count ?? 0,
                CommentsCount = p.Comments?.Count ?? 0,
                MediaFiles = p.Images.Select(i => new MediaFileDTO
                {
                    Url = i.ImageURL?.Replace("http://", "https://"),
                    Type = "image",
                    PublicId = i.CloudPublicId
                }).Concat(p.Videos.Select(v => new MediaFileDTO
                {
                    Url = v.VideoURL?.Replace("http://", "https://"),
                    Type = "video",
                    PublicId = v.CloudPublicId
                })).ToList()
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
                CreatedAt = p.CreatedAt,
                UserID = p.UserID,
                UserName = p.User.Username,
                FullName = p.User.FullName,
                CategoryName = p.Category?.CategoryName,
                LikesCount = p.Likes?.Count ?? 0,
                CommentsCount = p.Comments?.Count ?? 0,
                MediaFiles = p.Images.Select(i => new MediaFileDTO
                {
                    Url = i.ImageURL?.Replace("http://", "https://"),
                    Type = "image",
                    PublicId = i.CloudPublicId
                }).Concat(p.Videos.Select(v => new MediaFileDTO
                {
                    Url = v.VideoURL?.Replace("http://", "https://"),
                    Type = "video",
                    PublicId = v.CloudPublicId
                })).ToList()
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
                CreatedAt = p.CreatedAt,
                UserID = p.UserID,
                UserName = p.User.Username,
                FullName = p.User.FullName,
                CategoryName = p.Category?.CategoryName,
                LikesCount = p.Likes?.Count ?? 0,
                CommentsCount = p.Comments?.Count ?? 0,
                MediaFiles = p.Images.Select(i => new MediaFileDTO
                {
                    Url = i.ImageURL?.Replace("http://", "https://"),
                    Type = "image",
                    PublicId = i.CloudPublicId
                }).Concat(p.Videos.Select(v => new MediaFileDTO
                {
                    Url = v.VideoURL?.Replace("http://", "https://"),
                    Type = "video",
                    PublicId = v.CloudPublicId
                })).ToList()
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