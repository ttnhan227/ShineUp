using CloudinaryDotNet.Actions;

namespace Server.Interfaces;

public interface ICloudinaryService
{
    Task<ImageUploadResult> UploadImgAsync(IFormFile file);
    Task<VideoUploadResult> UploadVideoAsync(IFormFile file);
    Task<DeletionResult> DeleteMediaAsync(string publicId);
}