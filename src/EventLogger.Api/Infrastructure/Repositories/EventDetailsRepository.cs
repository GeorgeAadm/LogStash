using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using EventLogger.Api.Domain.Entities;
using EventLogger.Api.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EventLogger.Api.Infrastructure.Repositories
{
    public class EventDetailsRepository : IEventDetailsRepository
    {
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly DynamoDbConfiguration _configuration;
        private readonly ILogger<EventDetailsRepository> _logger;

        public EventDetailsRepository(
            IAmazonDynamoDB dynamoDb,
            IOptions<DynamoDbConfiguration> configuration,
            ILogger<EventDetailsRepository> logger)
        {
            _dynamoDb = dynamoDb;
            _configuration = configuration.Value;
            _logger = logger;
        }

        public async Task CreateAsync(EventDetails details)
        {
            try
            {
                _logger.LogDebug("Creating event details in DynamoDB for EventId: {EventId}", details.EventId);

                var item = new Dictionary<string, AttributeValue>
                {
                    ["EventId"] = new AttributeValue { S = details.EventId.ToString() },
                    ["CreatedAt"] = new AttributeValue { S = details.CreatedAt.ToString("O") },
                    ["UserId"] = new AttributeValue { S = details.UserId ?? "" },
                    ["EventType"] = new AttributeValue { S = details.EventType ?? "" },
                    ["Category"] = new AttributeValue { S = details.Category ?? "" }
                };

                // Only add Details if it has a value
                if (details.Details.ValueKind != JsonValueKind.Undefined && 
                    details.Details.ValueKind != JsonValueKind.Null)
                {
                    item["Details"] = new AttributeValue { S = details.Details.GetRawText() };
                }

                var request = new PutItemRequest
                {
                    TableName = _configuration.TableName,
                    Item = item
                };

                await _dynamoDb.PutItemAsync(request);
                
                _logger.LogInformation("Successfully created event details for EventId: {EventId}", details.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event details for EventId: {EventId}", details.EventId);
                throw;
            }
        }

        public async Task<Dictionary<Guid, EventDetails>> GetByEventIdsAsync(List<Guid> eventIds)
        {
            try
            {
                _logger.LogDebug("Retrieving event details for {Count} event IDs", eventIds.Count);

                var results = new Dictionary<Guid, EventDetails>();
                
                // DynamoDB BatchGetItem has a limit of 100 items per request
                var batches = eventIds.Chunk(100);

                foreach (var batch in batches)
                {
                    var keys = batch.Select(id => new Dictionary<string, AttributeValue>
                    {
                        ["EventId"] = new AttributeValue { S = id.ToString() }
                    }).ToList();

                    var request = new BatchGetItemRequest
                    {
                        RequestItems = new Dictionary<string, KeysAndAttributes>
                        {
                            [_configuration.TableName] = new KeysAndAttributes { Keys = keys }
                        }
                    };

                    var response = await _dynamoDb.BatchGetItemAsync(request);

                    if (response.Responses.ContainsKey(_configuration.TableName))
                    {
                        foreach (var item in response.Responses[_configuration.TableName])
                        {
                            var eventDetails = MapToEventDetails(item);
                            if (eventDetails != null)
                            {
                                results[eventDetails.EventId] = eventDetails;
                            }
                        }
                    }
                }

                _logger.LogInformation("Retrieved {Count} event details from DynamoDB", results.Count);
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event details");
                throw;
            }
        }

        private EventDetails? MapToEventDetails(Dictionary<string, AttributeValue> item)
        {
            try
            {
                if (!item.ContainsKey("EventId") || string.IsNullOrEmpty(item["EventId"].S))
                    return null;

                var eventDetails = new EventDetails
                {
                    EventId = Guid.Parse(item["EventId"].S),
                    CreatedAt = item.ContainsKey("CreatedAt") && !string.IsNullOrEmpty(item["CreatedAt"].S)
                        ? DateTime.Parse(item["CreatedAt"].S)
                        : DateTime.UtcNow,
                    UserId = item.ContainsKey("UserId") ? item["UserId"].S : null,
                    EventType = item.ContainsKey("EventType") ? item["EventType"].S : null,
                    Category = item.ContainsKey("Category") ? item["Category"].S : null,
                    Details = JsonDocument.Parse("{}").RootElement // Default empty object
                };

                // Parse the Details JSON if present
                if (item.ContainsKey("Details") && !string.IsNullOrEmpty(item["Details"].S))
                {
                    eventDetails.Details = JsonDocument.Parse(item["Details"].S).RootElement;
                }

                return eventDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping DynamoDB item to EventDetails");
                return null;
            }
        }
    }
}