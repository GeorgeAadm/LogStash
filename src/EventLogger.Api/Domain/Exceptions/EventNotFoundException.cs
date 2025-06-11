namespace EventLogger.Api.Domain.Exceptions
{
    public class EventNotFoundException : Exception
    {
        public EventNotFoundException() : base()
        {
        }

        public EventNotFoundException(string message) : base(message)
        {
        }

        public EventNotFoundException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        public EventNotFoundException(Guid eventId) 
            : base($"Event with ID {eventId} was not found.")
        {
        }

        public EventNotFoundException(string userId, string eventType) 
            : base($"No events found for user {userId} with type {eventType}.")
        {
        }
    }
}