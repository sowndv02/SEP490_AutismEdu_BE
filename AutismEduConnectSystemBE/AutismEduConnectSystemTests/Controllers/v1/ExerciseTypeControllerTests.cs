using Xunit;
using AutismEduConnectSystem.Controllers.v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutismEduConnectSystem.Repository.IRepository;
using AutoMapper;
using Moq;
using AutismEduConnectSystem.Services.IServices;
using Microsoft.Extensions.Logging;
using AutismEduConnectSystem.Mapper;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using FluentAssertions;
using AutismEduConnectSystem.Models.DTOs;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace AutismEduConnectSystem.Controllers.v1.Tests
{
    public class ExerciseTypeControllerTests
    {
        private readonly Mock<IExerciseRepository> _exerciseRepositoryMock;
        private readonly Mock<IExerciseTypeRepository> _exerciseTypeRepositoryMock;
        private readonly IMapper _mapperMock;
        private readonly Mock<IResourceService> _resourceServiceMock;
        private readonly Mock<ILogger<ExerciseTypeController>> _loggerMock;
        private readonly ExerciseTypeController _controller;

        public ExerciseTypeControllerTests()
        {
            // Create mocks for dependencies
            _exerciseRepositoryMock = new Mock<IExerciseRepository>();
            _exerciseTypeRepositoryMock = new Mock<IExerciseTypeRepository>();
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingConfig());
            });
            _mapperMock = config.CreateMapper();
            _resourceServiceMock = new Mock<IResourceService>();
            _loggerMock = new Mock<ILogger<ExerciseTypeController>>();

            // Instantiate the controller with the mocks
            _controller = new ExerciseTypeController(
                _exerciseRepositoryMock.Object,
                _exerciseTypeRepositoryMock.Object,
                _mapperMock,
                _resourceServiceMock.Object,
                _loggerMock.Object
            );

            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task CreateAsync_ReturnsUnauthorized_WhenUserIsUnauthorized()
        {
            // Arrange
            _resourceServiceMock
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");

            // Simulate a user with no valid claims (unauthorized)
            var claimsIdentity = new ClaimsIdentity(); // No claims added
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };


            var requestPayload = new ExerciseTypeCreateDTO
            {
                ExerciseTypeName = "Sample Exercise Type"
            };


            // Act
            var result = await _controller.CreateAsync(requestPayload);
            var unauthorizedResult = result.Result as ObjectResult;

            // Assert
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            var apiResponse = unauthorizedResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            apiResponse.ErrorMessages.First().Should().Be("Unauthorized access.");
        }


        [Fact]
        public async Task CreateAsync_ReturnsForbiden_WhenUserDoesNotHaveRequiredRole()
        {
            // Arrange
            _resourceServiceMock
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbiden access.");

            // Simulate a user with no valid claims (unauthorized)
            var claims = new List<Claim>
             {
                 new Claim(ClaimTypes.NameIdentifier, "testUserId")
             };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            var requestPayload = new ExerciseTypeCreateDTO
            {
                ExerciseTypeName = "Sample Exercise Type"
            };


            // Act
            var result = await _controller.CreateAsync(requestPayload);
            var unauthorizedResult = result.Result as ObjectResult;

            // Assert
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

            var apiResponse = unauthorizedResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            apiResponse.ErrorMessages.First().Should().Be("Forbiden access.");
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnCreated_WhenValidInput()
        {
            // Arrange
            var userId = "testUserId";
            var exerciseTypeCreateDTO = new ExerciseTypeCreateDTO { ExerciseTypeName = "New Exercise Type" };
            var exerciseType = new ExerciseType { Id = 1, ExerciseTypeName = "New Exercise Type", SubmitterId = userId };
            var exerciseTypeDTO = new ExerciseTypeDTO { Id = 1, ExerciseTypeName = "New Exercise Type" };

            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            _exerciseTypeRepositoryMock.Setup(repo => repo.CreateAsync(exerciseType)).ReturnsAsync(exerciseType);

            // Act
            var result = await _controller.CreateAsync(exerciseTypeCreateDTO);

            result.Should().NotBeNull();
            result.Result.Should().BeOfType<OkObjectResult>();

            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();

            var response = okResult!.Value as APIResponse;
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Result.Should().BeEquivalentTo(exerciseTypeDTO);

            _exerciseTypeRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<ExerciseType>()), Times.Once);
        }

    }
}