using CloudinaryDotNet.Actions;

namespace Server.Interfaces;

public interface ICloudinaryService
{
    Task<ImageUploadResult> UploadImgAsync(IFormFile file);
    Task<VideoUploadResult> UploadVidAsync(IFormFile file);
    Task<DeletionResult> DeleteAsync(string publicId);
}