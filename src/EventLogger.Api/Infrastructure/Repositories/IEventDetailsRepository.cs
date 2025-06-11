using EventLogger.Api.Domain.Entities;

namespace EventLogger.Api.Infrastructure.Repositories
{
    public interface IEventDetailsRepository
    {
        Task CreateAsync(EventDetails details);
        Task<Dictionary<Guid, EventDetails>> GetByEventIdsAsync(List<Guid> eventIds);
    }
}
