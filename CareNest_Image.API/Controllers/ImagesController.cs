using CareNest_Image.API.Services;
using CareNest_Image.API.Repositories;
using CareNest_Image.API.Models;
using CareNest_Image.API.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace CareNest_Image.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly IImageService _imageService;
        private readonly IImageRepository _imageRepository;

        public ImagesController(IImageService imageService, IImageRepository imageRepository)
        {
            _imageService = imageService;
            _imageRepository = imageRepository;
        }

        // Upload ảnh kèm ownerId để truy xuất về sau
        [HttpPost("{ownerId}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20_000_000)] // 20 MB
        public async Task<IActionResult> Upload(string ownerId, [FromForm] UploadImageRequest request)
        {
            if (request?.File == null || request.File.Length == 0) return BadRequest("File is required");
            if (string.IsNullOrWhiteSpace(ownerId)) return BadRequest("ownerId is required");

            var result = await _imageService.UploadImageAsync(request.File, request.Folder, request.PublicId);

            var url = _imageService.BuildDeliveryUrl(result.PublicId ?? request.PublicId ?? string.Empty);

            var record = new ImageRecord
            {
                OwnerId = ownerId,
                PublicId = result.PublicId ?? request.PublicId ?? string.Empty,
                SecureUrl = result.SecureUrl?.ToString(),
                OptimizedUrl = url,
                Width = result.Width,
                Height = result.Height,
                Bytes = result.Bytes,
                Format = result.Format,
                Version = result.Version
            };
            await _imageRepository.AddAsync(record);

            return Ok(record);
        }

        // Lấy danh sách ảnh theo ownerId
        [HttpGet("{ownerId}")]
        public async Task<IActionResult> GetByOwner(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return BadRequest("ownerId is required");
            var images = await _imageRepository.GetByOwnerAsync(ownerId);
            return Ok(images);
        }

        [HttpDelete("{publicId}")]
        public async Task<IActionResult> Delete(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId)) return BadRequest("publicId is required");

            var result = await _imageService.DeleteImageAsync(publicId);
            await _imageRepository.RemoveByPublicIdAsync(publicId);
            return Ok(new { result = result.Result });
        }
    }
}


