using Microsoft.AspNetCore.Mvc;
using Server.Data;
using Server.DTOs;
using Server.Interfaces;
using Server.Models;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideosController : ControllerBase
    {
        private readonly ICloudinaryService _cloudinary;
        private readonly DatabaseContext _db;

        public VideosController(ICloudinaryService cloudinary, DatabaseContext db)
        {
            _cloudinary = cloudinary;
            _db = db;
        }


        
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadVideo([FromForm] VideoUploadDTO dto)
        {
            if (dto.VideoFile == null || dto.VideoFile.Length == 0)
                return BadRequest("Video file is required.");

            var userId = GetCurrentUserId();

            var uploadResult = await _cloudinary.UploadVidAsync(dto.VideoFile);

            var video = new Video
            {
                VideoID      = uploadResult.PublicId,  // Dùng publicId làm khóa chính
                UserID       = userId,
                CategoryID   = dto.CategoryID,
                Title        = dto.Title,
                Description  = dto.Description,
                VideoURL     = uploadResult.SecureUrl.ToString(),
                CloudPublicId= uploadResult.PublicId,
                PrivacyID    = dto.PrivacyID,
                UploadDate   = DateTime.UtcNow
            };

            _db.Videos.Add(video);
            await _db.SaveChangesAsync();

            var result = new VideoDTO
            {
                VideoID      = video.VideoID,
                UserID       = video.UserID,
                CategoryID   = video.CategoryID,
                Title        = video.Title,
                Description  = video.Description,
                VideoURL     = video.VideoURL,
                PrivacyID    = video.PrivacyID,
                UploadDate   = video.UploadDate
            };

            return Ok(result);
        }


        

        
        [HttpPost("delete"+"/{publicId}")]
        public async Task<IActionResult> Delete(string publicId)
        {
            if (string.IsNullOrEmpty(publicId))
                return BadRequest("Public ID is required");

            var deletionResult = await _cloudinary.DeleteAsync(publicId);
            if (deletionResult.Error != null)
                return BadRequest(deletionResult.Error.Message);

            return Ok(new { Message = "File deleted successfully" });
        }
        
        
        private int GetCurrentUserId()
        {
            return 1; // Tạm thời hardcoded
        }
    }

}
