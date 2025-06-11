using EventLogger.Api.Application.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace EventLogger.Tests.Integration
{
    public class EventsApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public EventsApiIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task HealthCheck_Should_Return_Ok()
        {
            // Act
            var response = await _client.GetAsync("/api/healthcheck");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("healthy");
        }

        [Fact]
        public async Task CreateEvent_Should_Return_Created()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                UserId = "integration@test.com",
                EventType = "LOGIN",
                Source = "web",
                EventDetails = JsonDocument.Parse(@"{""test"": ""integration""}").RootElement
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/events", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            
            var content = await response.Content.ReadFromJsonAsync<CreateEventResponse>();
            content.Should().NotBeNull();
            content.EventId.Should().NotBeEmpty();
            content.UserId.Should().Be(request.UserId);
            content.EventType.Should().Be(request.EventType);
        }

        [Fact]
        public async Task CreateEvent_Should_Return_BadRequest_For_Invalid_Request()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                UserId = "invalid-email",
                EventType = "INVALID_TYPE"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/events", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetUserEvents_Should_Return_NotFound_For_NonExistent_User()
        {
            // Act
            var response = await _client.GetAsync("/api/events/nonexistent@test.com");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetUserEvents_Should_Return_BadRequest_For_Invalid_Email()
        {
            // Act
            var response = await _client.GetAsync("/api/events/not-an-email");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateAndRetrieveEvent_EndToEnd()
        {
            // Arrange
            var userId = $"test_{Guid.NewGuid()}@example.com";
            var createRequest = new CreateEventRequest
            {
                UserId = userId,
                EventType = "PURCHASE",
                Source = "mobile",
                EventDetails = JsonDocument.Parse(@"{""amount"": 99.99, ""currency"": ""USD""}").RootElement
            };

            // Act - Create event
            var createResponse = await _client.PostAsJsonAsync("/api/events", createRequest);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // Small delay to ensure data is persisted
            await Task.Delay(100);

            // Act - Retrieve events
            var getResponse = await _client.GetAsync($"/api/events/{userId}");

            // Assert
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var events = await getResponse.Content.ReadFromJsonAsync<List<EventResponse>>();
            events.Should().NotBeNull();
            events.Should().HaveCount(1);
            events[0].UserId.Should().Be(userId);
            events[0].EventType.Should().Be("PURCHASE");
            events[0].EventDetails.Should().NotBeNull();
        }

        [Theory]
        [InlineData("LOGIN", "web")]
        [InlineData("LOGOUT", "mobile")]
        [InlineData("ERROR", "api")]
        public async Task CreateEvent_Should_Accept_Different_EventTypes_And_Sources(string eventType, string source)
        {
            // Arrange
            var request = new CreateEventRequest
            {
                UserId = "test@example.com",
                EventType = eventType,
                Source = source
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/events", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task GetUserEvents_Should_Support_Filtering()
        {
            // Arrange
            var userId = $"filter_test_{Guid.NewGuid()}@example.com";
            
            // Create multiple events
            var eventTypes = new[] { "LOGIN", "PAGE_VIEW", "LOGIN", "ERROR" };
            foreach (var eventType in eventTypes)
            {
                var request = new CreateEventRequest
                {
                    UserId = userId,
                    EventType = eventType,
                    Source = "web"
                };
                await _client.PostAsJsonAsync("/api/events", request);
            }

            await Task.Delay(100);

            // Act - Filter by event type
            var response = await _client.GetAsync($"/api/events/{userId}?eventType=LOGIN");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var events = await response.Content.ReadFromJsonAsync<List<EventResponse>>();
            events.Should().NotBeNull();
            events.Should().HaveCount(2);
            events.Should().OnlyContain(e => e.EventType == "LOGIN");
        }

        [Fact]
        public async Task GetUserEvents_Should_Respect_Limit_Parameter()
        {
            // Arrange
            var userId = $"limit_test_{Guid.NewGuid()}@example.com";
            
            // Create 5 events
            for (int i = 0; i < 5; i++)
            {
                var request = new CreateEventRequest
                {
                    UserId = userId,
                    EventType = "PAGE_VIEW",
                    Source = "web"
                };
                await _client.PostAsJsonAsync("/api/events", request);
            }

            await Task.Delay(100);

            // Act - Get with limit
            var response = await _client.GetAsync($"/api/events/{userId}?limit=3");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var events = await response.Content.ReadFromJsonAsync<List<EventResponse>>();
            events.Should().NotBeNull();
            events.Should().HaveCount(3);
        }
    }
}