using Microsoft.AspNetCore.Mvc;

namespace Server.DTOs;

public class VideoDTO
{
    public string VideoID { get; set; }
    public int UserID { get; set; }
    public int CategoryID { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string VideoURL { get; set; }
    public int? PrivacyID { get; set; }
    public DateTime UploadDate { get; set; }
}

public class VideoUploadDTO
{
    [FromForm] public IFormFile VideoFile { get; set; }
    [FromForm] public string Title { get; set; }
    [FromForm] public string Description { get; set; }
    [FromForm] public int CategoryID { get; set; }
    [FromForm] public int? PrivacyID { get; set; }
}