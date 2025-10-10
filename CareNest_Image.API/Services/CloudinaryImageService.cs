using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CareNest_Image.API.Services
{
    public interface IImageService
    {
        Task<ImageUploadResult> UploadImageAsync(IFormFile file, string? folder = null, string? publicId = null, CancellationToken cancellationToken = default);
        Task<DeletionResult> DeleteImageAsync(string publicId, CancellationToken cancellationToken = default);
        string BuildDeliveryUrl(string publicId, int? width = null, int? height = null, string crop = "fill");
    }

    public class CloudinaryImageService : IImageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly Settings.CloudinaryOptions _options;

        public CloudinaryImageService(Cloudinary cloudinary, IOptions<Settings.CloudinaryOptions> options)
        {
            _cloudinary = cloudinary;
            _options = options.Value;
        }

        public async Task<ImageUploadResult> UploadImageAsync(IFormFile file, string? folder = null, string? publicId = null, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty", nameof(file));

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder ?? _options.DefaultFolder,
                PublicId = publicId,
                Overwrite = true,
                UseFilename = string.IsNullOrEmpty(publicId),
                UniqueFilename = string.IsNullOrEmpty(publicId),
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
            return result;
        }

        public async Task<DeletionResult> DeleteImageAsync(string publicId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                throw new ArgumentException("publicId is required", nameof(publicId));

            var delParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image
            };
            var result = await _cloudinary.DestroyAsync(delParams);
            return result;
        }

        public string BuildDeliveryUrl(string publicId, int? width = null, int? height = null, string crop = "fill")
        {
            var transformation = new Transformation().Quality("auto").FetchFormat("auto");
            if (width.HasValue) transformation = transformation.Width(width.Value);
            if (height.HasValue) transformation = transformation.Height(height.Value);
            if (!string.IsNullOrEmpty(crop)) transformation = transformation.Crop(crop);

            return _cloudinary.Api.UrlImgUp.Transform(transformation).BuildUrl(publicId);
        }
    }
}


