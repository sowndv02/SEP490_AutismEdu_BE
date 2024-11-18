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
using AutismEduConnectSystem.Models.DTOs.UpdateDTOs;
using static AutismEduConnectSystem.SD;
using System.Linq.Expressions;

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
            var exerciseTypeCreateDTO = new ExerciseTypeCreateDTO { ExerciseTypeName = "New Exercise Type", IsHide = false };
            var exerciseType = new ExerciseType {ExerciseTypeName = "New Exercise Type", SubmitterId = userId, IsHide = false, CreatedDate = DateTime.Now };
            var exerciseTypeReturn = new ExerciseType { Id = 1, ExerciseTypeName = "New Exercise Type", SubmitterId = userId, IsHide = false, CreatedDate = DateTime.Now };
            var exerciseTypeDTO = new ExerciseTypeDTO { Id = 1, ExerciseTypeName = "New Exercise Type", IsHide = false, CreatedDate = DateTime.Now };

            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            _exerciseTypeRepositoryMock.Setup(repo => repo.CreateAsync(exerciseType)).ReturnsAsync(exerciseTypeReturn);

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
            response.Result.Should().NotBeNull();

            _exerciseTypeRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<ExerciseType>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_InternalServerError_ReturnsServerError()
        {
            // Arrange
            var userId = "testUserId";
            var exerciseTypeCreateDTO = new ExerciseTypeCreateDTO { ExerciseTypeName = "Test Exercise" };
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            _resourceServiceMock
                .Setup(x => x.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("An error occurred while processing your request.");

            var createDTO = new ExerciseTypeCreateDTO
            {
                ExerciseTypeName = "New Exercise Type",
                IsHide = true,
            };
            _exerciseTypeRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<ExerciseType>()))
                .ThrowsAsync(new Exception("Lỗi hệ thống. Vui lòng thử lại sau!"));

            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Lỗi hệ thống. Vui lòng thử lại sau!");


            var result = await _controller.CreateAsync(createDTO);
            var internalServerErrorResult = result.Result as ObjectResult;

            // Assert
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = internalServerErrorResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.First().Should().Be("Lỗi hệ thống. Vui lòng thử lại sau!");
        }

        [Fact]
        public async Task CreateAsync_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var exerciseTypeCreateDTO = new ExerciseTypeCreateDTO();
            var userId = "testUserId";
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            _controller.ModelState.AddModelError("Name", "The Name field is required.");

            _resourceServiceMock
                .Setup(x => x.GetString(SD.BAD_REQUEST_MESSAGE, SD.EXERCISE_TYPE))
                .Returns("Invalid model state for ExerciseTypeCreateDTO.");

            // Act
            var result = await _controller.CreateAsync(exerciseTypeCreateDTO);
            var badRequestResult = result.Result as ObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }


        [Fact]
        public async Task UpdateStatusRequest_ReturnsUnauthorized_WhenUserIsUnauthorized()
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


            // Act
            var result = await _controller.UpdateStatusRequest(1);
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
        public async Task UpdateStatusRequest_ReturnsForbiden_WhenUserDoesNotHaveRequiredRole()
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


            // Act
            var result = await _controller.UpdateStatusRequest(1);
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
        public async Task UpdateStatusRequest_ReturnsBadRequest_IfExerciseTypeIsNull()
        {
            // Arrange
            var id = 999999;

            _exerciseTypeRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ExerciseType, bool>>>(), true, "Submitter", null))
                .ReturnsAsync((ExerciseType)null);
            _resourceServiceMock.Setup(r => r.GetString(It.IsAny<string>(), It.IsAny<string>())).Returns("Không timg thấy loại bài tập");

            // Act
            var result = await _controller.UpdateStatusRequest(id);
            var notFoundResult = result.Result as NotFoundObjectResult;

            // Assert
            notFoundResult.Should().NotBeNull();
            var apiResponse = notFoundResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Không timg thấy loại bài tập");
        }


        [Fact]
        public async Task UpdateStatusRequest_ReturnsBadRequest_IfIdIsZero()
        {
            // Arrange
            var id = 0;
            _resourceServiceMock.Setup(r => r.GetString(It.IsAny<string>(), It.IsAny<string>())).Returns("Error message");
            // Act
            var result = await _controller.UpdateStatusRequest(id);
            var badRequestResult = result.Result as BadRequestObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Error message");
        }

        [Fact]
        public async Task UpdateStatusRequest_ReturnsInternalServerError_OnException()
        {
            // Arrange
            var id = 1;
            _resourceServiceMock.Setup(r => r.GetString(INTERNAL_SERVER_ERROR_MESSAGE)).Returns("Internal server error");
            _exerciseTypeRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ExerciseType, bool>>>(), true, "Submitter", null))
                .ThrowsAsync(new Exception("Lỗi hệ thống vui lòng thử lại sau"));

            // Act
            var result = await _controller.UpdateStatusRequest(id);
            var internalServerErrorResult = result.Result as ObjectResult;

            // Assert
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = internalServerErrorResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Lỗi hệ thống vui lòng thử lại sau");
        }

        [Fact]
        public async Task UpdateStatusRequest_ReturnsBadRequest_IfExerciseTypeIsNotHide ()
        {
            // Arrange
            var id = 1;
            var newExerciseType = new ExerciseType { Id = 1, ExerciseTypeName = "Test Exercise", SubmitterId = "user123", IsHide = false };

            _exerciseTypeRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ExerciseType, bool>>>(), true, "Submitter", null))
                .ReturnsAsync(newExerciseType);

            // Act
            var result = await _controller.UpdateStatusRequest(id);
            var badRequestResult = result.Result as ObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateStatusRequest_ReturnsOk_IfExerciseTypeIsHide()
        {
            // Arrange
            var id = 1;
            var existingExerciseType = new ExerciseType { Id = id, ExerciseTypeName = "Test Exercise", SubmitterId = "user123", IsHide = true, Submitter = new ApplicationUser() { Id = "user123" } };
            var updatedExerciseType = new ExerciseType { Id = id, ExerciseTypeName = "Test Exercise", SubmitterId = "user123", Submitter = new ApplicationUser(){ Id = "user123" }, IsHide = false };

            _exerciseTypeRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ExerciseType, bool>>>(), true, "Submitter", null))
                .ReturnsAsync(existingExerciseType);

            _exerciseTypeRepositoryMock.Setup(r => r.UpdateAsync(existingExerciseType))
                .ReturnsAsync(updatedExerciseType);

            // Act
            var result = await _controller.UpdateStatusRequest(id);
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().BeEquivalentTo(new
            {
                Id = id,
                ExerciseTypeName = "Test Exercise",
                IsHide = false
            });
        }


    }
}