namespace CareNest_Image.API.Models
{
    public class ImageRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string OwnerId { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
        public string? SecureUrl { get; set; }
        public string? OptimizedUrl { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public long? Bytes { get; set; }
        public string? Format { get; set; }
        public string? Version { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}


