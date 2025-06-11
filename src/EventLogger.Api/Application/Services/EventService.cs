using EventLogger.Api.Application.DTOs;
using EventLogger.Api.Domain.Entities;
using EventLogger.Api.Infrastructure.Repositories;
using System.Text.Json;

namespace EventLogger.Api.Application.Services
{
    public class EventService : IEventService
    {
        private readonly IEventMetadataRepository _metadataRepository;
        private readonly IEventDetailsRepository _detailsRepository;
        private readonly ILogger<EventService> _logger;

        public EventService(
            IEventMetadataRepository metadataRepository,
            IEventDetailsRepository detailsRepository,
            ILogger<EventService> logger)
        {
            _metadataRepository = metadataRepository;
            _detailsRepository = detailsRepository;
            _logger = logger;
        }

        public async Task<CreateEventResponse> CreateEventAsync(CreateEventRequest request)
        {
            try
            {
                var eventId = Guid.NewGuid();
                var timestamp = DateTime.UtcNow;

                _logger.LogInformation("Creating event {EventId} for user {UserId}", eventId, request.UserId);

                // Create metadata in SQL Server
                var metadata = new EventMetadata
                {
                    EventId = eventId,
                    UserId = request.UserId,
                    EventType = request.EventType,
                    Timestamp = timestamp,
                    Source = request.Source
                };

                await _metadataRepository.CreateAsync(metadata);

                // Create details in DynamoDB if provided
                if (request.EventDetails.HasValue)
                {
                    var details = new EventDetails
                    {
                        EventId = eventId,
                        Details = request.EventDetails.Value,
                        CreatedAt = timestamp,
                        UserId = request.UserId,
                        EventType = request.EventType,
                        Category = DetermineCategory(request.EventType)
                    };

                    await _detailsRepository.CreateAsync(details);
                }

                return new CreateEventResponse
                {
                    EventId = eventId,
                    UserId = request.UserId,
                    EventType = request.EventType,
                    Timestamp = timestamp,
                    Source = request.Source
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event for user {UserId}", request.UserId);
                throw;
            }
        }

        public async Task<List<EventResponse>> GetUserEventsAsync(GetUserEventsQuery query)
        {
            try
            {
                _logger.LogInformation("Retrieving events for user {UserId}", query.UserId);

                // Get metadata from SQL Server
                var metadataList = await _metadataRepository.GetByUserIdAsync(
                    query.UserId,
                    query.EventType,
                    query.FromDate,
                    query.ToDate,
                    query.Limit);

                if (!metadataList.Any())
                {
                    return new List<EventResponse>();
                }

                // Get details from DynamoDB
                var eventIds = metadataList.Select(m => m.EventId).ToList();
                var detailsDict = await _detailsRepository.GetByEventIdsAsync(eventIds);

                // Combine metadata and details
                var responses = metadataList.Select(metadata =>
                {
                    var response = new EventResponse
                    {
                        EventId = metadata.EventId,
                        UserId = metadata.UserId,
                        EventType = metadata.EventType,
                        Timestamp = metadata.Timestamp,
                        Source = metadata.Source
                    };

                    if (detailsDict.TryGetValue(metadata.EventId, out var details))
                    {
                        response.EventDetails = details.Details;
                    }

                    return response;
                }).ToList();

                _logger.LogInformation("Retrieved {Count} events for user {UserId}", responses.Count, query.UserId);
                
                return responses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events for user {UserId}", query.UserId);
                throw;
            }
        }

        private string DetermineCategory(string eventType)
        {
            return eventType.ToUpper() switch
            {
                "LOGIN" or "LOGOUT" => "Authentication",
                "PURCHASE" or "PAYMENT" => "Transaction",
                "ERROR" or "CRASH" => "Error",
                "PAGE_VIEW" or "CLICK" => "Analytics",
                _ => "General"
            };
        }
    }
}