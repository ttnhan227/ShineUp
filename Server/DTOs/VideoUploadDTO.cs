using Microsoft.AspNetCore.Mvc;

namespace Server.DTOs;

public class VideoUploadDTO
{
    [FromForm] public IFormFile VideoFile { get; set; }
    [FromForm] public string Title { get; set; }
    [FromForm] public string Description { get; set; }
    [FromForm] public int CategoryID { get; set; } 
    [FromForm] public int? PrivacyID { get; set; }
}


