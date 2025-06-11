using EventLogger.Api.Application.DTOs;
using FluentValidation;

namespace EventLogger.Api.Application.Validators
{
    public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
    {
        private readonly string[] _validEventTypes = new[]
        {
            "LOGIN", "LOGOUT", "PURCHASE", "PAGE_VIEW", "ERROR", 
            "API_CALL", "PERFORMANCE", "CRASH", "CLICK", "PAYMENT"
        };

        private readonly string[] _validSources = new[]
        {
            "web", "mobile", "api", "system", "batch"
        };

        public CreateEventRequestValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required")
                .MaximumLength(100).WithMessage("UserId must not exceed 100 characters")
                .Matches(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")
                .WithMessage("UserId must be a valid email address");

            RuleFor(x => x.EventType)
                .NotEmpty().WithMessage("EventType is required")
                .MaximumLength(100).WithMessage("EventType must not exceed 100 characters")
                .Must(BeAValidEventType).WithMessage($"EventType must be one of: {string.Join(", ", _validEventTypes)}");

            RuleFor(x => x.Source)
                .MaximumLength(100).WithMessage("Source must not exceed 100 characters")
                .Must(BeAValidSource).When(x => !string.IsNullOrEmpty(x.Source))
                .WithMessage($"Source must be one of: {string.Join(", ", _validSources)}");

            RuleFor(x => x.EventDetails)
                .Must(BeValidJson).When(x => x.EventDetails.HasValue)
                .WithMessage("EventDetails must be valid JSON");
        }

        private bool BeAValidEventType(string eventType)
        {
            return !string.IsNullOrEmpty(eventType) && 
                   _validEventTypes.Contains(eventType.ToUpper());
        }

        private bool BeAValidSource(string? source)
        {
            return string.IsNullOrEmpty(source) || 
                   _validSources.Contains(source.ToLower());
        }

        private bool BeValidJson(System.Text.Json.JsonElement? json)
        {
            if (!json.HasValue) return true;
            
            try
            {
                // Check if it's a valid JSON object or array
                var valueKind = json.Value.ValueKind;
                return valueKind == System.Text.Json.JsonValueKind.Object || 
                       valueKind == System.Text.Json.JsonValueKind.Array;
            }
            catch
            {
                return false;
            }
        }
    }
}