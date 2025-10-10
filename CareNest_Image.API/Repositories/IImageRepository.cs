using CareNest_Image.API.Models;

namespace CareNest_Image.API.Repositories
{
    public interface IImageRepository
    {
        Task AddAsync(ImageRecord record, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ImageRecord>> GetByOwnerAsync(string ownerId, CancellationToken cancellationToken = default);
        Task<bool> RemoveByPublicIdAsync(string publicId, CancellationToken cancellationToken = default);
    }
}


