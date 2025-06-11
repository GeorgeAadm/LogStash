using EventLogger.Api.Application.DTOs;

namespace EventLogger.Api.Application.Services
{
    public interface IEventService
    {
        Task<CreateEventResponse> CreateEventAsync(CreateEventRequest request);
        Task<List<EventResponse>> GetUserEventsAsync(GetUserEventsQuery query);
    }
}