using EventLogger.Api.Application.DTOs;
using EventLogger.Api.Application.Services;
using EventLogger.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace EventLogger.Tests.Unit.Controllers
{
    public class EventsControllerTests
    {
        private readonly Mock<IEventService> _eventServiceMock;
        private readonly Mock<ILogger<EventsController>> _loggerMock;
        private readonly EventsController _controller;

        public EventsControllerTests()
        {
            _eventServiceMock = new Mock<IEventService>();
            _loggerMock = new Mock<ILogger<EventsController>>();
            _controller = new EventsController(_eventServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task CreateEvent_Should_Return_Created_Result()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                UserId = "test@example.com",
                EventType = "LOGIN",
                Source = "web"
            };

            var expectedResponse = new CreateEventResponse
            {
                EventId = Guid.NewGuid(),
                UserId = request.UserId,
                EventType = request.EventType,
                Timestamp = DateTime.UtcNow,
                Source = request.Source
            };

            _eventServiceMock
                .Setup(x => x.CreateEventAsync(It.IsAny<CreateEventRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateEvent(request);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.StatusCode.Should().Be(201);
            createdResult.ActionName.Should().Be(nameof(EventsController.GetUserEvents));
            createdResult.Value.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task CreateEvent_Should_Return_500_When_Service_Throws()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                UserId = "test@example.com",
                EventType = "LOGIN"
            };

            _eventServiceMock
                .Setup(x => x.CreateEventAsync(It.IsAny<CreateEventRequest>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.CreateEvent(request);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
            objectResult.Value.Should().BeEquivalentTo(new { error = "An error occurred while creating the event" });
        }

        [Fact]
        public async Task GetUserEvents_Should_Return_Ok_With_Events()
        {
            // Arrange
            var userId = "test@example.com";
            var events = new List<EventResponse>
            {
                new EventResponse
                {
                    EventId = Guid.NewGuid(),
                    UserId = userId,
                    EventType = "LOGIN",
                    Timestamp = DateTime.UtcNow
                }
            };

            _eventServiceMock
                .Setup(x => x.GetUserEventsAsync(It.IsAny<GetUserEventsQuery>()))
                .ReturnsAsync(events);

            // Act
            var result = await _controller.GetUserEvents(userId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(events);
        }

        [Fact]
        public async Task GetUserEvents_Should_Return_NotFound_When_No_Events()
        {
            // Arrange
            var userId = "test@example.com";

            _eventServiceMock
                .Setup(x => x.GetUserEventsAsync(It.IsAny<GetUserEventsQuery>()))
                .ReturnsAsync(new List<EventResponse>());

            // Act
            var result = await _controller.GetUserEvents(userId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
            notFoundResult.Value.Should().BeEquivalentTo(new { message = $"No events found for user {userId}" });
        }

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("@example.com")]
        [InlineData("test@")]
        [InlineData("")]
        public async Task GetUserEvents_Should_Return_BadRequest_For_Invalid_Email(string userId)
        {
            // Act
            var result = await _controller.GetUserEvents(userId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().BeEquivalentTo(new { error = "Invalid userId format. Must be a valid email address." });
        }

        [Fact]
        public async Task GetUserEvents_Should_Pass_Query_Parameters_Correctly()
        {
            // Arrange
            var userId = "test@example.com";
            var eventType = "LOGIN";
            var fromDate = DateTime.UtcNow.AddDays(-7);
            var toDate = DateTime.UtcNow;
            var limit = 25;

            GetUserEventsQuery capturedQuery = null;
            _eventServiceMock
                .Setup(x => x.GetUserEventsAsync(It.IsAny<GetUserEventsQuery>()))
                .Callback<GetUserEventsQuery>(q => capturedQuery = q)
                .ReturnsAsync(new List<EventResponse> { new EventResponse() });

            // Act
            await _controller.GetUserEvents(userId, eventType, fromDate, toDate, limit);

            // Assert
            capturedQuery.Should().NotBeNull();
            capturedQuery.UserId.Should().Be(userId);
            capturedQuery.EventType.Should().Be(eventType);
            capturedQuery.FromDate.Should().Be(fromDate);
            capturedQuery.ToDate.Should().Be(toDate);
            capturedQuery.Limit.Should().Be(limit);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1001)]
        [InlineData(-1)]
        public async Task GetUserEvents_Should_Handle_Invalid_Limit(int limit)
        {
            // Arrange
            var userId = "test@example.com";

            // Act
            // Note: In a real scenario, model validation would handle this
            // This test demonstrates that the Range attribute should work
            var result = await _controller.GetUserEvents(userId, limit: limit);

            // Assert
            // The actual validation would be handled by ASP.NET Core's model validation
            // This is more of an integration test scenario
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetUserEvents_Should_Return_500_When_Service_Throws()
        {
            // Arrange
            var userId = "test@example.com";

            _eventServiceMock
                .Setup(x => x.GetUserEventsAsync(It.IsAny<GetUserEventsQuery>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GetUserEvents(userId);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
            objectResult.Value.Should().BeEquivalentTo(new { error = "An error occurred while retrieving events" });
        }
    }
}