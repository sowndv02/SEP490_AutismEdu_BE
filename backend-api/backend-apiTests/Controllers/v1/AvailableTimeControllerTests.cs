using AutoMapper;
using backend_api.Mapper;
using backend_api.Models;
using backend_api.Repository.IRepository;
using backend_api.Services.IServices;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Security.Claims;
using Xunit;

namespace backend_api.Controllers.v1.Tests
{
    public class AvailableTimeControllerTests
    {

        
        private readonly Mock<IAvailableTimeSlotRepository> _availableTimeSlotRepositoryMock;
        private readonly IMapper _mapper;
        private readonly Mock<IResourceService> _resourceServiceMock;
        private readonly Mock<ILogger<AvailableTimeController>> _loggerMock;
        private readonly AvailableTimeController _controller;
        private readonly APIResponse _response;

        public AvailableTimeControllerTests()
        {
            _availableTimeSlotRepositoryMock = new Mock<IAvailableTimeSlotRepository>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingConfig());
            });
            _mapper = config.CreateMapper();
            _resourceServiceMock = new Mock<IResourceService>();
            _loggerMock = new Mock<ILogger<AvailableTimeController>>();
            _controller = new AvailableTimeController(
                _availableTimeSlotRepositoryMock.Object,
                _mapper,
                _resourceServiceMock.Object,
                _loggerMock.Object);
            _response = new APIResponse();


            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        }

        [Fact]
        public async Task CreateAsync_ReturnsBadRequest_WhenDtoIsNull()
        {
            // Arrange
            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.AVAILABLE_TIME))
                .Returns("Invalid request data.");

            // Act
            var result = await _controller.CreateAsync(null);
            var statusCodeResult = result.Result as BadRequestObjectResult;

            // Assert
            statusCodeResult.Should().NotBeNull();
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = statusCodeResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.First().Should().Be("Invalid request data.");
        }



    }
}