using EventLogger.Api.Domain.Entities;

namespace EventLogger.Api.Infrastructure.Repositories
{
    public interface IEventMetadataRepository
    {
         Task CreateAsync(EventMetadata metadata);
         Task<List<EventMetadata>> GetByUserIdAsync(
            string userId,
            string? eventType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int limit = 50);
    }
}
