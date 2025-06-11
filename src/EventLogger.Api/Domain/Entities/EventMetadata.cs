namespace EventLogger.Api.Domain.Entities
{
    public class EventMetadata
    {
        public Guid EventId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? Source { get; set; }
    }
}