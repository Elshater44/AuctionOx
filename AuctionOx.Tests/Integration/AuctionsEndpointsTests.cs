using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AuctionOx.DTOs.Auctions;
using AuctionOx.DTOs.Auth;
using AuctionOx.DTOs.Common;
using FluentAssertions;
using Xunit;

namespace AuctionOx.Tests.Integration
{
    public class AuctionsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AuctionsEndpointsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        // =====================================================================
        // HELPER — Creates an HttpClient with a valid Bearer JWT header
        // Usage:
        //   var adminClient = await CreateAuthenticatedClientAsync("admin@auctionox.com", "AdminPassword123!");
        // =====================================================================
        private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
        {
            var client = _factory.CreateClient();
            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
            {
                Email = email,
                Password = password
            });

            var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
            return client;
        }

        // ── Test 1: Public Pagination Query ─────────────────────────────
        // HINT:
        //   1. Call GET "/api/auctions" anonymously using _client.
        //   2. Assert response.StatusCode is 200 OK.
        //   3. Read content using:
        //      var result = await response.Content.ReadFromJsonAsync<PagedResult<AuctionItemDto>>();
        //   4. Assert result is not null and result.PageNumber is 1.
        [Fact]
        public async Task GetAuctions_Returns200Ok_WithPagedResult()
        {
            // Arrange

            // Act

            // Assert

        }

        // ── Test 2: Defensive Pagination Bounds (DoS Guard) ─────────────
        // HINT:
        //   1. Call GET "/api/auctions?pageSize=1000" using _client.
        //   2. Read the PagedResult<AuctionItemDto>.
        //   3. Assert that result.PageSize was clamped to 100 (not 1000!).
        [Fact]
        public async Task GetAuctions_WhenPageSizeExceedsLimit_ShouldClampTo100()
        {
            // Arrange

            // Act

            // Assert

        }

        // ── Test 3: Querying Non-Existent Auction ────────────────────────
        // HINT:
        //   1. Call GET "/api/auctions/99999" using _client.
        //   2. Assert response.StatusCode is HttpStatusCode.NotFound (404).
        [Fact]
        public async Task GetAuction_WhenIdDoesNotExist_Returns404NotFound()
        {
            // Arrange

            // Act

            // Assert

        }

        // ── Test 4: Role-Based Authorization Guard (403 Forbidden) ───────
        // HINT:
        //   1. Register a regular user (e.g., "regular_user@example.com").
        //   2. Call CreateAuthenticatedClientAsync("regular_user@example.com", "StrongPassword123!").
        //   3. Use that client to call DELETE "/api/auctions/admin/1".
        //   4. Assert response.StatusCode is HttpStatusCode.Forbidden (403)!
        [Fact]
        public async Task AdminDeleteAuction_WhenCalledByNonAdmin_Returns403Forbidden()
        {
            // Arrange

            // Act

            // Assert

        }
    }
}
