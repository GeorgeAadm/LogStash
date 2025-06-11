using EventLogger.Api.Application.DTOs;
using EventLogger.Api.Application.Services;
using EventLogger.Api.Domain.Entities;
using EventLogger.Api.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace EventLogger.Tests.Unit.Services
{
    public class EventServiceTests
    {
        private readonly Mock<IEventMetadataRepository> _metadataRepositoryMock;
        private readonly Mock<IEventDetailsRepository> _detailsRepositoryMock;
        private readonly Mock<ILogger<EventService>> _loggerMock;
        private readonly EventService _eventService;

        public EventServiceTests()
        {
            _metadataRepositoryMock = new Mock<IEventMetadataRepository>();
            _detailsRepositoryMock = new Mock<IEventDetailsRepository>();
            _loggerMock = new Mock<ILogger<EventService>>();
            
            _eventService = new EventService(
                _metadataRepositoryMock.Object,
                _detailsRepositoryMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task CreateEventAsync_Should_Create_Event_Successfully()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                UserId = "test@example.com",
                EventType = "LOGIN",
                Source = "web",
                EventDetails = JsonDocument.Parse(@"{""browser"": ""Chrome""}").RootElement
            };

            // Act
            var result = await _eventService.CreateEventAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.EventId.Should().NotBeEmpty();
            result.UserId.Should().Be(request.UserId);
            result.EventType.Should().Be(request.EventType);
            result.Source.Should().Be(request.Source);
            result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            // Verify repository calls
            _metadataRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<EventMetadata>()), Times.Once);
            _detailsRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<EventDetails>()), Times.Once);
        }

        [Fact]
        public async Task CreateEventAsync_Should_Create_Event_Without_Details()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                UserId = "test@example.com",
                EventType = "LOGIN",
                Source = "web",
                EventDetails = null
            };

            // Act
            var result = await _eventService.CreateEventAsync(request);

            // Assert
            result.Should().NotBeNull();
            
            // Verify only metadata repository was called
            _metadataRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<EventMetadata>()), Times.Once);
            _detailsRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<EventDetails>()), Times.Never);
        }

        [Fact]
        public async Task CreateEventAsync_Should_Set_Correct_Category()
        {
            // Arrange
            var testCases = new[]
            {
                ("LOGIN", "Authentication"),
                ("LOGOUT", "Authentication"),
                ("PURCHASE", "Transaction"),
                ("PAYMENT", "Transaction"),
                ("ERROR", "Error"),
                ("CRASH", "Error"),
                ("PAGE_VIEW", "Analytics"),
                ("CLICK", "Analytics"),
                ("UNKNOWN_TYPE", "General")
            };

            foreach (var (eventType, expectedCategory) in testCases)
            {
                var request = new CreateEventRequest
                {
                    UserId = "test@example.com",
                    EventType = eventType,
                    EventDetails = JsonDocument.Parse("{}").RootElement
                };

                EventDetails capturedDetails = null;
                _detailsRepositoryMock
                    .Setup(x => x.CreateAsync(It.IsAny<EventDetails>()))
                    .Callback<EventDetails>(details => capturedDetails = details)
                    .Returns(Task.CompletedTask);

                // Act
                await _eventService.CreateEventAsync(request);

                // Assert
                capturedDetails.Should().NotBeNull();
                capturedDetails.Category.Should().Be(expectedCategory);
            }
        }

        [Fact]
        public async Task CreateEventAsync_Should_Throw_When_Repository_Fails()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                UserId = "test@example.com",
                EventType = "LOGIN"
            };

            _metadataRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<EventMetadata>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _eventService.CreateEventAsync(request));
        }

        [Fact]
        public async Task GetUserEventsAsync_Should_Return_Events_With_Details()
        {
            // Arrange
            var userId = "test@example.com";
            var eventId = Guid.NewGuid();
            
            var metadata = new List<EventMetadata>
            {
                new EventMetadata
                {
                    EventId = eventId,
                    UserId = userId,
                    EventType = "LOGIN",
                    Timestamp = DateTime.UtcNow,
                    Source = "web"
                }
            };

            var details = new Dictionary<Guid, EventDetails>
            {
                [eventId] = new EventDetails
                {
                    EventId = eventId,
                    Details = JsonDocument.Parse(@"{""browser"": ""Chrome""}").RootElement,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _metadataRepositoryMock
                .Setup(x => x.GetByUserIdAsync(userId, null, null, null, 50))
                .ReturnsAsync(metadata);

            _detailsRepositoryMock
                .Setup(x => x.GetByEventIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(details);

            var query = new GetUserEventsQuery { UserId = userId };

            // Act
            var result = await _eventService.GetUserEventsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].EventId.Should().Be(eventId);
            result[0].EventDetails.Should().NotBeNull();
            result[0].EventDetails.Value.GetProperty("browser").GetString().Should().Be("Chrome");
        }

        [Fact]
        public async Task GetUserEventsAsync_Should_Return_Events_Without_Details_When_Not_Found()
        {
            // Arrange
            var userId = "test@example.com";
            var eventId = Guid.NewGuid();
            
            var metadata = new List<EventMetadata>
            {
                new EventMetadata
                {
                    EventId = eventId,
                    UserId = userId,
                    EventType = "LOGIN",
                    Timestamp = DateTime.UtcNow
                }
            };

            _metadataRepositoryMock
                .Setup(x => x.GetByUserIdAsync(userId, null, null, null, 50))
                .ReturnsAsync(metadata);

            _detailsRepositoryMock
                .Setup(x => x.GetByEventIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new Dictionary<Guid, EventDetails>());

            var query = new GetUserEventsQuery { UserId = userId };

            // Act
            var result = await _eventService.GetUserEventsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].EventDetails.Should().BeNull();
        }

        [Fact]
        public async Task GetUserEventsAsync_Should_Return_Empty_List_When_No_Events()
        {
            // Arrange
            var userId = "test@example.com";
            
            _metadataRepositoryMock
                .Setup(x => x.GetByUserIdAsync(userId, null, null, null, 50))
                .ReturnsAsync(new List<EventMetadata>());

            var query = new GetUserEventsQuery { UserId = userId };

            // Act
            var result = await _eventService.GetUserEventsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
            
            // Verify details repository was not called
            _detailsRepositoryMock.Verify(x => x.GetByEventIdsAsync(It.IsAny<List<Guid>>()), Times.Never);
        }

        [Fact]
        public async Task GetUserEventsAsync_Should_Pass_Query_Parameters_Correctly()
        {
            // Arrange
            var query = new GetUserEventsQuery
            {
                UserId = "test@example.com",
                EventType = "LOGIN",
                FromDate = DateTime.UtcNow.AddDays(-7),
                ToDate = DateTime.UtcNow,
                Limit = 25
            };

            _metadataRepositoryMock
                .Setup(x => x.GetByUserIdAsync(
                    query.UserId,
                    query.EventType,
                    query.FromDate,
                    query.ToDate,
                    query.Limit))
                .ReturnsAsync(new List<EventMetadata>());

            // Act
            await _eventService.GetUserEventsAsync(query);

            // Assert
            _metadataRepositoryMock.Verify(x => x.GetByUserIdAsync(
                query.UserId,
                query.EventType,
                query.FromDate,
                query.ToDate,
                query.Limit), Times.Once);
        }
    }
}