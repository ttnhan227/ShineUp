using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;
using System.Security.Claims;
using System.Linq;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<PostsController> _logger;
    private readonly IPostRepository _postRepository;
    
    private PostResponseDto MapToPostResponseDto(Post post)
    {
        if (post == null) return null;
        
        return new PostResponseDto
        {
            PostID = post.PostID,
            Title = post.Title,
            Content = post.Content,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            UserID = post.UserID,
            Username = post.User?.Username,
            FullName = post.User?.FullName,
            CategoryID = post.CategoryID,
            CategoryName = post.Category?.CategoryName,
            CommunityID = post.CommunityID,
            CommunityName = post.Community?.Name,
            PrivacyID = post.PrivacyID,
            PrivacyName = post.Privacy?.Name,
            LikesCount = post.Likes?.Count ?? 0,
            CommentsCount = post.Comments?.Count ?? 0,
            HasLiked = post.Likes?.Any(l => l.UserID == int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value)) ?? false,
            MediaFiles = (post.Images?.Select(i => new MediaFileDTO
            {
                Url = i.ImageURL,
                Type = "image",
                PublicId = i.CloudPublicId
            }) ?? Enumerable.Empty<MediaFileDTO>())
            .Concat(post.Videos?.Select(v => new MediaFileDTO
            {
                Url = v.VideoURL,
                Type = "video",
                PublicId = v.CloudPublicId
            }) ?? Enumerable.Empty<MediaFileDTO>())
            .ToList()
        };
    }

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
                ProfileImageURL = p.User.ProfileImageURL,
                CategoryName = p.Category?.CategoryName,
                CommunityID = p.CommunityID,
                CommunityName = p.Community?.Name,
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
                CommunityID = post.CommunityID,
                CommunityName = post.Community?.Name,
                PrivacyID = post.PrivacyID,
                PrivacyName = post.Privacy?.Name,
                LikesCount = post.Likes?.Count ?? 0,
                CommentsCount = post.Comments?.Count ?? 0,
                HasLiked = currentUserId.HasValue &&
                           await _postRepository.HasUserLikedPostAsync(post.PostID, currentUserId.Value),
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
        IDbContextTransaction transaction = null;
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _postRepository.GetUserByIdAsync(userId);

            if (user == null)
            {
                return Unauthorized();
            }

            // Start a transaction
            transaction = await _postRepository.BeginTransactionAsync();
            if (transaction == null)
            {
                return StatusCode(500, "Unable to start a database transaction.");
            }

            var post = new Post
            {
                Title = string.IsNullOrEmpty(createPostDto.Title)
                    ? createPostDto.Content.Length > 50
                        ? createPostDto.Content.Substring(0, 50) + "..."
                        : createPostDto.Content
                    : createPostDto.Title,
                Content = createPostDto.Content,
                UserID = userId,
                CategoryID = createPostDto.CategoryID,
                PrivacyID = createPostDto.PrivacyID ?? 1,
                CommunityID = createPostDto.CommunityID,
                CreatedAt = DateTime.UtcNow
            };

            await _postRepository.CreatePostAsync(post);

            // Handle media files
            var formFiles = Request.Form.Files;
            var mediaTypes = Request.Form.ContainsKey("mediaTypes") 
                ? Request.Form["mediaTypes"].ToList() 
                : new List<string>();


            _logger.LogInformation("Processing {FileCount} files with {MediaTypeCount} media types", 
                formFiles.Count, mediaTypes.Count);

            for (var i = 0; i < formFiles.Count; i++)
            {
                var file = formFiles[i];
                // Default to "image" if mediaTypes is empty or doesn't have enough items
                var mediaType = i < mediaTypes.Count ? mediaTypes[i] : "image";


                if (file.Length == 0)
                {
                    _logger.LogWarning("Skipping empty file: {FileName}", file.FileName);
                    continue;
                }


                try
                {
                    _logger.LogInformation("Uploading {MediaType}: {FileName} ({Length} bytes)", 
                        mediaType, file.FileName, file.Length);

                    if (mediaType.Equals("video", StringComparison.OrdinalIgnoreCase))
                    {
                        var uploadResult = await _cloudinaryService.UploadVideoAsync(file);
                        if (uploadResult != null)
                        {
                            // Generate a new GUID for the VideoID to avoid conflicts
                            var videoId = Guid.NewGuid().ToString();
                            var video = new Video
                            {
                                PostID = post.PostID,
                                VideoID = videoId,
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
                            _logger.LogInformation("Successfully uploaded video: {PublicId}", uploadResult.PublicId);
                        }
                    }
                    else // Default to image
                    {
                        var uploadResult = await _cloudinaryService.UploadImgAsync(file);
                        if (uploadResult != null)
                        {
                            // Generate a new GUID for the ImageID to avoid conflicts
                            var imageId = Guid.NewGuid().ToString();
                            var image = new Image
                            {
                                PostID = post.PostID,
                                ImageID = imageId,
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
                            _logger.LogInformation("Successfully uploaded image: {PublicId}", uploadResult.PublicId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading {MediaType}: {FileName}", mediaType, file.FileName);
                    // Rollback the transaction if any file upload fails
                    await _postRepository.RollbackTransactionAsync();
                    return StatusCode(500, $"Error uploading {file.FileName}: {ex.Message}");
                }
            }

            // Commit the transaction if everything succeeds
            await _postRepository.CommitTransactionAsync();
            
            // Refresh the post to get all related data
            var createdPost = await _postRepository.GetPostByIdAsync(post.PostID);
            var postDto = MapToPostResponseDto(createdPost);
            return CreatedAtAction(nameof(GetPost), new { id = post.PostID }, postDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post");
            try
            {
                await _postRepository.RollbackTransactionAsync();
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Error rolling back transaction");
            }
            return StatusCode(500, "Internal server error while creating the post: " + ex.Message);
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
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
            {
                post.Title = updatePostDto.Title;
            }

            if (!string.IsNullOrEmpty(updatePostDto.Content))
            {
                post.Content = updatePostDto.Content;
            }

            if (updatePostDto.CategoryID.HasValue)
            {
                post.CategoryID = updatePostDto.CategoryID;
            }

            if (updatePostDto.PrivacyID.HasValue)
            {
                post.PrivacyID = updatePostDto.PrivacyID;
            }

            // Handle media files
            if (updatePostDto.MediaFiles != null && updatePostDto.MediaFiles.Any())
            {
                // Remove existing media files
                await _postRepository.RemoveAllMediaFromPostAsync(post.PostID);

                // Add new media files
                foreach (var mediaFile in updatePostDto.MediaFiles)
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
            foreach (var image in post.Images) await _cloudinaryService.DeleteMediaAsync(image.CloudPublicId);
            foreach (var video in post.Videos) await _cloudinaryService.DeleteMediaAsync(video.CloudPublicId);

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