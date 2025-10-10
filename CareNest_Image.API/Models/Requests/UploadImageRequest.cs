using Microsoft.AspNetCore.Http;

namespace CareNest_Image.API.Models.Requests
{
    public class UploadImageRequest
    {
        public IFormFile File { get; set; } = default!;
        public string? Folder { get; set; }
        public string? PublicId { get; set; }
    }
}


