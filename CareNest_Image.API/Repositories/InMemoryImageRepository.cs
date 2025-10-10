using CareNest_Image.API.Models;
using System.Collections.Concurrent;

namespace CareNest_Image.API.Repositories
{
    public class InMemoryImageRepository : IImageRepository
    {
        private readonly ConcurrentDictionary<string, List<ImageRecord>> _ownerIdToImages = new();

        public Task AddAsync(ImageRecord record, CancellationToken cancellationToken = default)
        {
            var list = _ownerIdToImages.GetOrAdd(record.OwnerId, _ => new List<ImageRecord>());
            lock (list)
            {
                list.Add(record);
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ImageRecord>> GetByOwnerAsync(string ownerId, CancellationToken cancellationToken = default)
        {
            if (_ownerIdToImages.TryGetValue(ownerId, out var list))
            {
                lock (list)
                {
                    return Task.FromResult((IReadOnlyList<ImageRecord>)list.OrderByDescending(i => i.CreatedAtUtc).ToList());
                }
            }
            return Task.FromResult((IReadOnlyList<ImageRecord>)Array.Empty<ImageRecord>());
        }

        public Task<bool> RemoveByPublicIdAsync(string publicId, CancellationToken cancellationToken = default)
        {
            bool removed = false;
            foreach (var kv in _ownerIdToImages)
            {
                var list = kv.Value;
                lock (list)
                {
                    var countBefore = list.Count;
                    list.RemoveAll(i => i.PublicId == publicId);
                    if (list.Count != countBefore)
                    {
                        removed = true;
                    }
                }
            }
            return Task.FromResult(removed);
        }
    }
}


