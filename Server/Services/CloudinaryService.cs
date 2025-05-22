using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Server.Interfaces;

namespace Server.Service;

public class CloudinaryService : ICloudinaryService
{
    private readonly IVideoRepository _videoRepository;
    private readonly Cloudinary _cloudinary;
    
    public CloudinaryService(IVideoRepository videoRepository, IConfiguration config)
    {
        _videoRepository = videoRepository;
        var account = new Account(
            config.GetSection("Cloudinary:CloudName").Value,
            config.GetSection("Cloudinary:ApiKey").Value,
            config.GetSection("Cloudinary:ApiSecret").Value
        );

        _cloudinary = new Cloudinary(account);
    }
    
   
    public async Task<ImageUploadResult> UploadImgAsync(IFormFile file)
    {
        var result = new ImageUploadResult();
        if (file.Length > 0)
        {
            using var stream = file.OpenReadStream(); // read file
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                PublicId = "image_" + file.FileName,
                Folder = "demo",
            };
            result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null)
            {
                throw new Exception(result.Error.Message);
            }
        }
        return result;
    }

    public async Task<VideoUploadResult> UploadVidAsync(IFormFile file)
    {
        var result = new VideoUploadResult();
        if (file.Length > 0)
        {
            using var stream = file.OpenReadStream();
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                PublicId = "video_" + file.FileName,
                Folder = "demo",
            };
            result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null)
            {
                throw new Exception(result.Error.Message);
            }
        }
        return result;
    }

    public async Task<DeletionResult> DeleteAsync(string publicId)
    {
        var deletionParams = new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Video
        };
        var result = await _cloudinary.DestroyAsync(deletionParams);
        if (result.Error != null)
        {
            throw new Exception(result.Error.Message);
        }
        return result;
    }
}