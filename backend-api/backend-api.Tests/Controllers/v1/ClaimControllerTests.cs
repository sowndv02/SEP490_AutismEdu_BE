using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace backend_api.Controllers.v1.Tests
{
    public class ClaimControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        public ClaimControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }
        [Fact()]
        public async Task GetAllClaimsAsyncTestAsync()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/v1/claim?pageNumber=1");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }
    }
}