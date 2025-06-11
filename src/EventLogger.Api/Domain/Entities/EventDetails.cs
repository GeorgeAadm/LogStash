using System.Text.Json;

namespace EventLogger.Api.Domain.Entities
{
    public class EventDetails
    {
        public Guid EventId { get; set; }
        public JsonElement Details { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UserId { get; set; }
        public string? EventType { get; set; }
        public string? Category { get; set; }
    }
}