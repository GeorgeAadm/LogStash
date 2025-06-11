using EventLogger.Api.Application.DTOs;
using EventLogger.Api.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using System.Text.Json;
using Xunit;

namespace EventLogger.Tests.Unit.Validators
{
    public class CreateEventRequestValidatorTests
    {
        private readonly CreateEventRequestValidator _validator;

        public CreateEventRequestValidatorTests()
        {
            _validator = new CreateEventRequestValidator();
        }

        [Fact]
        public void Should_Pass_When_Request_Is_Valid()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                UserId = "test@example.com",
                EventType = "LOGIN",
                Source = "web",
                EventDetails = JsonDocument.Parse(@"{""key"": ""value""}").RootElement
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void Should_Fail_When_UserId_Is_Empty(string userId)
        {
            // Arrange
            var request = new CreateEventRequest
            {
                UserId = userId,
                EventType = "LOGIN"
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId)
                .WithErrorMessage("UserId is required");
        }

        [Theory]
        [InlineData("notanemail")]
        [InlineData("@example.com")]
        [InlineData("test@")]
        [InlineData("test.example.com")]
        public void Should_Fail_When_UserId_Is_Not_Valid_Email(string userId)
        {
            // Arrange
            var request = new CreateEventRequest
            {
                UserId = userId,
                EventType = "LOGIN"
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId)
                .WithErrorMessage("UserId must be a valid email address");
        }

        [Fact]
        public void Should_Fail_When_UserId_Exceeds_MaxLength()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                UserId = new string('a', 90) + "@example.com", // More than 100 chars
                EventType = "LOGIN"
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId)
                .WithErrorMessage("UserId must not exceed 100 characters");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Should_Fail_When_EventType_Is_Empty(string eventType)
        {
            // Arrange
            var request = new CreateEventRequest
            {
                UserId = "test@example.com",
                EventType = eventType
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.EventType)
                .WithErrorMessage("EventType is required");
        }

        [Theory]
        [InlineData("INVALID_TYPE")]
        [InlineData("UNKNOWN")]
        [InlineData("123")]
        public void Should_Fail_When_EventType_Is_Invalid(string eventType)
        {
            // Arrange
            var request = new CreateEventRequest
            {
                UserId = "test@example.com",
                EventType = eventType
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.EventType);
        }

        [Theory]
        [InlineData("LOGIN")]
        [InlineData("login")]
        [InlineData("PURCHASE")]
        [InlineData("ERROR")]
        public void Should_Pass_When_EventType_Is_Valid(string eventType)
        {
            // Arrange
            var request = new CreateEventRequest
            {
                UserId = "test@example.com",
                EventType = eventType
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.EventType);
        }

        [Theory]
        [InlineData("web")]
        [InlineData("mobile")]
        [InlineData("api")]
        [InlineData(null)]
        [InlineData("")]
        public void Should_Pass_When_Source_Is_Valid_Or_Empty(string source)
        {
            // Arrange
            var request = new CreateEventRequest
            {
                UserId = "test@example.com",
                EventType = "LOGIN",
                Source = source
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Source);
        }

        [Theory]
        [InlineData("invalid_source")]
        [InlineData("desktop")]
        public void Should_Fail_When_Source_Is_Invalid(string source)
        {
            // Arrange
            var request = new CreateEventRequest
            {
                UserId = "test@example.com",
                EventType = "LOGIN",
                Source = source
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Source);
        }

        [Fact]
        public void Should_Pass_When_EventDetails_Is_Valid_Json_Object()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                UserId = "test@example.com",
                EventType = "LOGIN",
                EventDetails = JsonDocument.Parse(@"{""browser"": ""Chrome"", ""version"": 120}").RootElement
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.EventDetails);
        }

        [Fact]
        public void Should_Pass_When_EventDetails_Is_Valid_Json_Array()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                UserId = "test@example.com",
                EventType = "LOGIN",
                EventDetails = JsonDocument.Parse(@"[1, 2, 3]").RootElement
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.EventDetails);
        }

        [Fact]
        public void Should_Pass_When_EventDetails_Is_Null()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                UserId = "test@example.com",
                EventType = "LOGIN",
                EventDetails = null
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.EventDetails);
        }
    }
}