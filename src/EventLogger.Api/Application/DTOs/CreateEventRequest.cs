using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace EventLogger.Api.Application.DTOs
{
    public class CreateEventRequest
    {
        [Required]
        [StringLength(100)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string EventType { get; set; } = string.Empty;

        public string? Source { get; set; }

        public JsonElement? EventDetails { get; set; }
    }

    public class CreateEventResponse
    {
        public Guid EventId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? Source { get; set; }
    }

    public class EventResponse
    {
        public Guid EventId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? Source { get; set; }
        public JsonElement? EventDetails { get; set; }
    }

    public class GetUserEventsQuery
    {
        public string UserId { get; set; } = string.Empty;
        public string? EventType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Limit { get; set; } = 50;
    }
}