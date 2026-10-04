using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AuctionOx.DTOs.Auth;
using FluentAssertions;
using Xunit;

namespace AuctionOx.Tests.Integration
{
    public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public AuthEndpointsTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetProfile_WithoutToken_Returns401Unauthorized()
        {
            // Act: Call a protected endpoint anonymously
            var response = await _client.GetAsync("/api/auth/profile");

            // Assert: API security middleware should block with 401
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Register_WithInvalidEmail_Returns400BadRequest()
        {
            // Arrange: FluentValidation rule violation
            var request = new RegisterRequest
            {
                Email = "not-an-email",
                Password = "Password123!",
                FirstName = "John",
                LastName = "Doe"
            };

            // Act: Send POST request into the in-memory test pipeline
            var response = await _client.PostAsJsonAsync("/api/auth/register", request);

            // Assert: Should be 400 Bad Request
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_WithValidData_Returns200OkWithToken()
        {
            // Arrange: Valid registration payload
            var request = new RegisterRequest
            {
                Email = "integration_test_user@example.com",
                Password = "StrongPassword123!",
                FirstName = "Alice",
                LastName = "Smith"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            authResponse.Should().NotBeNull();
            authResponse!.Token.Should().NotBeNullOrEmpty();
            authResponse.RefreshToken.Should().NotBeNullOrEmpty();
            authResponse.Expiration.Should().BeAfter(System.DateTime.UtcNow);
        }
    }
}
