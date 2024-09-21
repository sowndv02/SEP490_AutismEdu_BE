using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;

namespace backend_api.Controllers.Tests
{
    public class RoleControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        public RoleControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetAllRolesAsync_ForValidRequest_Returns200OK()
        {
            // arrange
            var client = _factory.CreateClient();
            // act
            var result = await client.GetAsync("/api/v1/role");
            // assert
            result.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }
    }
}