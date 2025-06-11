using EventLogger.Api.Domain.Entities;
using EventLogger.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventLogger.Api.Infrastructure.Repositories
{
    public class EventMetadataRepository : IEventMetadataRepository
    {
        private readonly EventsDbContext _context;
        private readonly ILogger<EventMetadataRepository> _logger;

        public EventMetadataRepository(EventsDbContext context, ILogger<EventMetadataRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task CreateAsync(EventMetadata metadata)
        {
            try
            {
                _logger.LogDebug("Creating event metadata for EventId: {EventId}", metadata.EventId);
                
                _context.EventMetadata.Add(metadata);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Successfully created event metadata for EventId: {EventId}", metadata.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event metadata for EventId: {EventId}", metadata.EventId);
                throw;
            }
        }

        public async Task<List<EventMetadata>> GetByUserIdAsync(
            string userId,
            string? eventType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int limit = 50)
        {
            try
            {
                _logger.LogDebug("Retrieving events for UserId: {UserId}, EventType: {EventType}, Limit: {Limit}", 
                    userId, eventType, limit);

                var query = _context.EventMetadata
                    .Where(e => e.UserId == userId);

                if (!string.IsNullOrEmpty(eventType))
                {
                    query = query.Where(e => e.EventType == eventType);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(e => e.Timestamp >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(e => e.Timestamp <= toDate.Value);
                }

                var results = await query
                    .OrderByDescending(e => e.Timestamp)
                    .Take(limit)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} events for UserId: {UserId}", results.Count, userId);
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events for UserId: {UserId}", userId);
                throw;
            }
        }
    }
}