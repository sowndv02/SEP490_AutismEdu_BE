using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Printing;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutismEduConnectSystem.Controllers.v1;
using AutismEduConnectSystem.Mapper;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Models.DTOs.UpdateDTOs;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutoMapper;
using Elasticsearch.Net;
using FluentAssertions;
using MailKit.Search;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static AutismEduConnectSystem.SD;
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
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
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
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            var requestPayload = new ExerciseTypeCreateDTO
            {
                ExerciseTypeName = "Sample Exercise Type",
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
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "testUserId") };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            var requestPayload = new ExerciseTypeCreateDTO
            {
                ExerciseTypeName = "Sample Exercise Type",
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
            var exerciseTypeCreateDTO = new ExerciseTypeCreateDTO
            {
                ExerciseTypeName = "New Exercise Type",
                IsHide = false,
            };
            var exerciseType = new ExerciseType
            {
                ExerciseTypeName = "New Exercise Type",
                SubmitterId = userId,
                IsHide = false,
                CreatedDate = DateTime.Now,
            };
            var exerciseTypeReturn = new ExerciseType
            {
                Id = 1,
                ExerciseTypeName = "New Exercise Type",
                SubmitterId = userId,
                IsHide = false,
                CreatedDate = DateTime.Now,
            };
            var exerciseTypeDTO = new ExerciseTypeDTO
            {
                Id = 1,
                ExerciseTypeName = "New Exercise Type",
                IsHide = false,
                CreatedDate = DateTime.Now,
            };

            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            _exerciseTypeRepositoryMock
                .Setup(repo => repo.CreateAsync(exerciseType))
                .ReturnsAsync(exerciseTypeReturn);

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

            _exerciseTypeRepositoryMock.Verify(
                repo => repo.CreateAsync(It.IsAny<ExerciseType>()),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateAsync_InternalServerError_ReturnsServerError()
        {
            // Arrange
            var userId = "testUserId";
            var exerciseTypeCreateDTO = new ExerciseTypeCreateDTO
            {
                ExerciseTypeName = "Test Exercise",
            };
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

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
            internalServerErrorResult
                .StatusCode.Should()
                .Be((int)HttpStatusCode.InternalServerError);

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
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

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
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
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
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "testUserId") };
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

            _exerciseTypeRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        true,
                        "Submitter",
                        null
                    )
                )
                .ReturnsAsync((ExerciseType)null);
            _resourceServiceMock
                .Setup(r => r.GetString(It.IsAny<string>(), It.IsAny<string>()))
                .Returns("Không timg thấy loại bài tập");

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
            _resourceServiceMock
                .Setup(r => r.GetString(It.IsAny<string>(), It.IsAny<string>()))
                .Returns("Error message");
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
            _resourceServiceMock
                .Setup(r => r.GetString(INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Lỗi hệ thống vui lòng thử lại sau");
            _exerciseTypeRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        true,
                        "Submitter",
                        null
                    )
                )
                .ThrowsAsync(new Exception("Lỗi hệ thống vui lòng thử lại sau"));

            // Act
            var result = await _controller.UpdateStatusRequest(id);
            var internalServerErrorResult = result.Result as ObjectResult;

            // Assert
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult
                .StatusCode.Should()
                .Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = internalServerErrorResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Lỗi hệ thống vui lòng thử lại sau");
        }

        [Fact]
        public async Task UpdateStatusRequest_ReturnsBadRequest_IfExerciseTypeIsNotHide()
        {
            // Arrange
            var id = 1;
            var newExerciseType = new ExerciseType
            {
                Id = 1,
                ExerciseTypeName = "Test Exercise",
                SubmitterId = "user123",
                IsHide = false,
            };

            _exerciseTypeRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        true,
                        "Submitter",
                        null
                    )
                )
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
            var existingExerciseType = new ExerciseType
            {
                Id = id,
                ExerciseTypeName = "Test Exercise",
                SubmitterId = "user123",
                IsHide = true,
                Submitter = new ApplicationUser() { Id = "user123" },
            };
            var updatedExerciseType = new ExerciseType
            {
                Id = id,
                ExerciseTypeName = "Test Exercise",
                SubmitterId = "user123",
                Submitter = new ApplicationUser() { Id = "user123" },
                IsHide = false,
            };

            _exerciseTypeRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        true,
                        "Submitter",
                        null
                    )
                )
                .ReturnsAsync(existingExerciseType);

            _exerciseTypeRepositoryMock
                .Setup(r => r.UpdateAsync(existingExerciseType))
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
            apiResponse
                .Result.Should()
                .BeEquivalentTo(
                    new
                    {
                        Id = id,
                        ExerciseTypeName = "Test Exercise",
                        IsHide = false,
                    }
                );
        }

        [Fact]
        public async Task UpdateAsync_ReturnsUnauthorized_WhenUserIsUnauthorized()
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
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            var updateDTO = new ExerciseTypeUpdateDTO()
            {
                Id = 1,
                ExerciseTypeName = "ExerciseType name",
            };
            // Act
            var result = await _controller.UpdateAsync(1, updateDTO);
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
        public async Task UpdateAsync_ReturnsForbiden_WhenUserDoesNotHaveRequiredRole()
        {
            // Arrange
            _resourceServiceMock
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbiden access.");

            // Simulate a user with no valid claims (unauthorized)
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "testUserId") };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            var updateDTO = new ExerciseTypeUpdateDTO()
            {
                Id = 1,
                ExerciseTypeName = "ExerciseType name",
            };
            // Act
            var result = await _controller.UpdateAsync(1, updateDTO);
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
        public async Task UpdateAsync_ReturnsBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            _controller.ModelState.AddModelError("Id", "Invalid");

            // Act
            var result = await _controller.UpdateAsync(1, new ExerciseTypeUpdateDTO());
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
        public async Task UpdateAsync_ReturnsNotFound_WhenExerciseTypeDoesNotExist()
        {
            // Arrange
            var updateDTO = new ExerciseTypeUpdateDTO { Id = 1 };

            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync((ExerciseType)null);

            // Act
            var result = await _controller.UpdateAsync(1, updateDTO);
            var notFoundResult = result.Result as ObjectResult;

            // Assert
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            var apiResponse = notFoundResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsBadRequest_WhenDuplicateNameExists()
        {
            // Arrange
            var updateDTO = new ExerciseTypeUpdateDTO
            {
                Id = 1,
                ExerciseTypeName = "Duplicate Name",
            };
            var existingType = new ExerciseType { Id = 2, ExerciseTypeName = "Duplicate Name" };

            _exerciseTypeRepositoryMock
                .Setup(repo => repo.GetAsync(x => x.Id == updateDTO.Id, true, null, null))
                .ReturnsAsync(new ExerciseType { Id = 1, ExerciseTypeName = "Original Name" });

            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        false,
                        null,
                        null
                    )
                )
                .ReturnsAsync(existingType);

            // Act
            var result = await _controller.UpdateAsync(1, updateDTO);
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
        public async Task UpdateAsync_ReturnsOk_WhenExerciseTypeIsUpdatedSuccessfully()
        {
            // Arrange
            var updateDTO = new ExerciseTypeUpdateDTO { Id = 1, ExerciseTypeName = "Updated Name" };
            var existingType = new ExerciseType { Id = 1, ExerciseTypeName = "Original Name" };

            _exerciseTypeRepositoryMock
                .Setup(repo => repo.GetAsync(x => x.Id == updateDTO.Id, true, null, null))
                .ReturnsAsync(existingType);

            _exerciseTypeRepositoryMock
                .Setup(repo => repo.UpdateAsync(It.IsAny<ExerciseType>()))
                .ReturnsAsync(new ExerciseType { Id = 1, ExerciseTypeName = "Updated Name" });

            // Act
            var result = await _controller.UpdateAsync(1, updateDTO);
            var okResult = result.Result as ObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsInternalServerError_WhenExceptionOccurs()
        {
            // Arrange
            var updateDTO = new ExerciseTypeUpdateDTO { Id = 1, ExerciseTypeName = "Updated Name" };
            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Lỗi hệ thống vui lòng thử lại sau.");
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ThrowsAsync(new Exception("Lỗi hệ thống vui lòng thử lại sau."));

            // Act
            var result = await _controller.UpdateAsync(1, updateDTO);
            var errorResult = result.Result as ObjectResult;

            // Assert
            errorResult.Should().NotBeNull();
            errorResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var response = errorResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            response
                .ErrorMessages.Should()
                .ContainSingle(error =>
                    error == _resourceServiceMock.Object.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE)
                );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_ReturnsInternalServerError_WhenExceptionOccurs()
        {
            var search = "Test";
            var isHide = "true";
            var pageSize = 0;
            var pageNumber = 1;
            var sort = SD.ORDER_DESC;
            var orderBy = SD.CREATED_DATE;
            // Arrange
            var updateDTO = new ExerciseTypeUpdateDTO { Id = 1, ExerciseTypeName = "Updated Name" };
            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Lỗi hệ thống vui lòng thử lại sau.");

            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true
                    )
                )
                .ThrowsAsync(new Exception("Lỗi hệ thống vui lòng thử lại sau."));
            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var errorResult = result.Result as ObjectResult;

            // Assert
            errorResult.Should().NotBeNull();
            errorResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var response = errorResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            response
                .ErrorMessages.Should()
                .ContainSingle(error =>
                    error == _resourceServiceMock.Object.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE)
                );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_SearchAndIsHideTrue_NoPaging_OrderedDesc()
        {
            // Arrange
            var search = "Test";
            var isHide = "true";
            var pageSize = 0;
            var pageNumber = 1;
            var sort = SD.ORDER_DESC;
            var orderBy = SD.CREATED_DATE;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
            var submitterUser = new ApplicationUser { Id = "testTutorId", UserName = "testTutor" };

            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
            };

            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true
                    )
                )
                .ReturnsAsync((exerciseTypes.Count, exerciseTypes));

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2);
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate);
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search) && e.IsHide);

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.Total.Should().Be(exerciseTypes.Count);

            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_ReturnsUnauthorized_WhenUserIsUnauthorized()
        {
            var search = "Test";
            var isHide = "true";
            var pageSize = 0;
            var pageNumber = 1;
            var sort = SD.ORDER_DESC;
            var orderBy = SD.CREATED_DATE;
            // Arrange
            _resourceServiceMock
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");

            // Simulate a user with no valid claims (unauthorized)
            var claimsIdentity = new ClaimsIdentity(); // No claims added
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
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
        public async Task GetAllExerciseTypesAsync_ReturnsForbiden_WhenUserDoesNotHaveRequiredRole()
        {
            var search = "Test";
            var isHide = "true";
            var pageSize = 0;
            var pageNumber = 1;
            var sort = SD.ORDER_DESC;
            var orderBy = SD.CREATED_DATE;
            // Arrange
            _resourceServiceMock
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbiden access.");

            // Simulate a user with no valid claims (unauthorized)
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "testUserId") };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
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
        public async Task GetAllExerciseTypesAsync_TutorRole_SearchAndIsHideTrue_NoPaging_OrderedAsc()
        {
            // Arrange
            var search = "Test";
            var isHide = "true";
            var pageSize = 0;
            var pageNumber = 1;
            var sort = SD.ORDER_ASC;
            var orderBy = SD.CREATED_DATE;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
            var submitterUser = new ApplicationUser { Id = "testTutorId", UserName = "testTutor" };

            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = true,
                },
            };

            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Set to false for ascending order
                    )
                )
                .ReturnsAsync(
                    (exerciseTypes.Count, exerciseTypes.OrderBy(e => e.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2);
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate);
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search) && e.IsHide);

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.Total.Should().Be(exerciseTypes.Count);

            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_SearchAndIsHideTrue_WithPaging_OrderedAsc()
        {
            // Arrange
            var search = "Test";
            var isHide = "true";
            var pageSize = 10;
            var pageNumber = 1;
            var sort = SD.ORDER_ASC;
            var orderBy = SD.CREATED_DATE;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
            var submitterUser = new ApplicationUser { Id = "testTutorId", UserName = "testTutor" };

            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = true,
                },
            };

            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false
                    )
                )
                .ReturnsAsync(
                    (exerciseTypes.Count, exerciseTypes.OrderBy(e => e.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2);
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate);
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search) && e.IsHide);

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.Total.Should().Be(exerciseTypes.Count);

            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_SearchAndIsHideTrue_WithPaging_OrderedDesc()
        {
            // Arrange
            var search = "Test";
            var isHide = "true";
            var pageSize = 10;
            var pageNumber = 1;
            var sort = SD.ORDER_DESC;
            var orderBy = SD.CREATED_DATE;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
            var submitterUser = new ApplicationUser { Id = "testTutorId", UserName = "testTutor" };

            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = true,
                },
            };

            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count,
                        exerciseTypes.OrderByDescending(e => e.CreatedDate).ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2);
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate);
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search) && e.IsHide);

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.Total.Should().Be(exerciseTypes.Count);

            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_SearchAndIsHideAll_WithPaging_OrderedDesc()
        {
            // Arrange
            var search = "Test";
            var isHide = "all";
            var pageSize = 10;
            var pageNumber = 1;
            var sort = SD.ORDER_DESC;
            var orderBy = SD.CREATED_DATE;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
            };

            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count,
                        exerciseTypes.OrderByDescending(e => e.CreatedDate).ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2);
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate);
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search));

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.Total.Should().Be(exerciseTypes.Count);

            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_SearchAndIsHideAll_WithPaging_OrderedAsc()
        {
            // Arrange
            var search = "Test";
            var isHide = "all";
            var pageSize = 10;
            var pageNumber = 1;
            var sort = SD.ORDER_ASC;
            var orderBy = SD.CREATED_DATE;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
            };

            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Ascending order
                    )
                )
                .ReturnsAsync(
                    (exerciseTypes.Count, exerciseTypes.OrderBy(e => e.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2);
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate);
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search));

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.Total.Should().Be(exerciseTypes.Count);

            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_SearchAndIsHideAll_NoPaging_OrderedAsc()
        {
            // Arrange
            var search = "Test";
            var isHide = "all";
            var pageSize = 0;
            var pageNumber = 1;
            var sort = SD.ORDER_ASC;
            var orderBy = SD.CREATED_DATE;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
            };

            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Ascending order
                    )
                )
                .ReturnsAsync(
                    (exerciseTypes.Count, exerciseTypes.OrderBy(e => e.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2);
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate);
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search));

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.Total.Should().Be(exerciseTypes.Count);

            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_SearchAndIsHideAll_NoPaging_OrderedDesc()
        {
            // Arrange
            var search = "Test";
            var isHide = "all"; // All values for IsHide (no filtering)
            var pageSize = 0;
            var pageNumber = 1;
            var sort = SD.ORDER_DESC; // Sorting in descending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Assigning Tutor role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with varied CreatedDate and IsHide properties
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
            };

            // Mock the repository call to return exercise types sorted by CreatedDate in descending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Set to true for descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count,
                        exerciseTypes.OrderByDescending(e => e.CreatedDate).ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2);
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate); // Assert descending order
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search)); // Ensure all records match the search term

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.Total.Should().Be(exerciseTypes.Count); // Ensure total count is correct

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_SearchAndIsHideFalse_NoPaging_OrderedDesc()
        {
            // Arrange
            var search = "Test";
            var isHide = "false"; // Only include non-hidden exercise types
            var pageSize = 0;
            var pageNumber = 1;
            var sort = SD.ORDER_DESC; // Sorting in descending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Assigning Tutor role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with varied CreatedDate and IsHide properties
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
            };

            // Mock the repository call to return exercise types sorted by CreatedDate in descending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Set to true for descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count,
                        exerciseTypes.OrderByDescending(e => e.CreatedDate).ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2);
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate); // Assert descending order
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search) && !e.IsHide); // Ensure all records match the search term and are not hidden

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.Total.Should().Be(exerciseTypes.Count); // Ensure total count is correct

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_SearchAndIsHideFalse_NoPaging_OrderedAsc()
        {
            // Arrange
            var search = "Test";
            var isHide = "false"; // Only include non-hidden exercise types
            var pageSize = 0;
            var pageNumber = 1;
            var sort = SD.ORDER_ASC; // Sorting in ascending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Assigning Tutor role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with varied CreatedDate and IsHide properties
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
            };

            // Mock the repository call to return exercise types sorted by CreatedDate in ascending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Set to false for ascending order
                    )
                )
                .ReturnsAsync(
                    (exerciseTypes.Count, exerciseTypes.OrderBy(e => e.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2);
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate); // Assert ascending order
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search) && !e.IsHide); // Ensure all records match the search term and are not hidden

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.Total.Should().Be(exerciseTypes.Count); // Ensure total count is correct

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_SearchAndIsHideFalse_WithPaging_OrderedDesc()
        {
            // Arrange
            var search = "Test";
            var isHide = "false"; // Only include non-hidden exercise types
            var pageSize = 10; // Paging with a page size of 10
            var pageNumber = 1; // First page
            var sort = SD.ORDER_DESC; // Sorting in descending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Assigning Tutor role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with varied CreatedDate and IsHide properties
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
            };

            // Mock the repository call to return exercise types sorted by CreatedDate in descending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Set to true for descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count,
                        exerciseTypes.OrderByDescending(e => e.CreatedDate).ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2);
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate); // Assert descending order
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search) && !e.IsHide); // Ensure all records match the search term and are not hidden

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.Total.Should().Be(exerciseTypes.Count); // Ensure total count is correct

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_SearchAndIsHideFalse_WithPaging_OrderedAsc()
        {
            // Arrange
            var search = "Test";
            var isHide = "false"; // Only include non-hidden exercise types
            var pageSize = 10; // Paging with a page size of 10
            var pageNumber = 1; // First page
            var sort = SD.ORDER_ASC; // Sorting in ascending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Assigning Tutor role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with varied CreatedDate and IsHide properties
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
            };

            // Mock the repository call to return exercise types sorted by CreatedDate in ascending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Set to false for ascending order
                    )
                )
                .ReturnsAsync(
                    (exerciseTypes.Count, exerciseTypes.OrderBy(e => e.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2);
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate); // Assert ascending order
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search) && !e.IsHide); // Ensure all records match the search term and are not hidden

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.Total.Should().Be(exerciseTypes.Count); // Ensure total count is correct

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_NoSearch_IsHideTrue_NoPaging_OrderedAsc()
        {
            // Arrange
            var search = ""; // No search term
            var isHide = "true"; // Only include hidden exercise types
            var pageSize = 0; // No paging (all items should be returned)
            var pageNumber = 1; // First page
            var sort = SD.ORDER_ASC; // Sorting in ascending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Assigning Tutor role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with IsHide set to true
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = true,
                },
            };

            // Mock the repository call to return exercise types sorted by CreatedDate in ascending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Set to false for ascending order
                    )
                )
                .ReturnsAsync(
                    (exerciseTypes.Count, exerciseTypes.OrderBy(e => e.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2);
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate); // Assert ascending order
            resultList.Should().OnlyContain(e => e.IsHide); // Ensure all records are hidden (IsHide = true)

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 0 (no paging)
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(exerciseTypes.Count); // Ensure total count is correct

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_NoSearch_IsHideTrue_NoPaging_OrderedDesc()
        {
            // Arrange
            var search = ""; // No search term
            var isHide = "true"; // Only include hidden exercise types
            var pageSize = 0; // No paging (all items should be returned)
            var pageNumber = 1; // First page
            var sort = SD.ORDER_DESC; // Sorting in descending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Assigning Tutor role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with IsHide set to true
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = true,
                },
            };

            // Mock the repository call to return exercise types sorted by CreatedDate in descending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Set to true for descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count,
                        exerciseTypes.OrderByDescending(e => e.CreatedDate).ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2);
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate); // Assert descending order
            resultList.Should().OnlyContain(e => e.IsHide); // Ensure all records are hidden (IsHide = true)

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 0 (no paging)
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(exerciseTypes.Count); // Ensure total count is correct

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_NoSearch_IsHideTrue_WithPaging_OrderedDesc()
        {
            // Arrange
            var search = ""; // No search term
            var isHide = "true"; // Only include hidden exercise types
            var pageSize = 5; // Apply paging (not zero)
            var pageNumber = 1; // First page
            var sort = SD.ORDER_DESC; // Sorting in descending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Assigning Tutor role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with mixed IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = true,
                },
            };

            // Mock the repository call to return exercise types, filtered by IsHide = true, sorted by CreatedDate in descending order, with paging
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Set to true for descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count,
                        exerciseTypes
                            .Where(e => e.IsHide)
                            .OrderByDescending(e => e.CreatedDate)
                            .Take(pageSize)
                            .ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2); // Only two items should be returned based on IsHide = true
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate); // Assert descending order
            resultList.Should().OnlyContain(e => e.IsHide); // Ensure all records are hidden (IsHide = true)

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 5
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(3); // Total should be 3 based on filtered results

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_NoSearch_IsHideTrue_WithPaging_OrderedAsc()
        {
            // Arrange
            var search = ""; // No search term
            var isHide = "true"; // Only include hidden exercise types
            var pageSize = 10; // Apply paging (not zero)
            var pageNumber = 1; // First page
            var sort = SD.ORDER_ASC; // Sorting in ascending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Assigning Tutor role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with mixed IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = true,
                },
            };

            // Mock the repository call to return exercise types, filtered by IsHide = true, sorted by CreatedDate in ascending order, with paging
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Set to false for ascending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count,
                        exerciseTypes
                            .Where(e => e.IsHide)
                            .OrderBy(e => e.CreatedDate)
                            .Take(pageSize)
                            .ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2); // Only two items should be returned based on IsHide = true
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate); // Assert ascending order
            resultList.Should().OnlyContain(e => e.IsHide); // Ensure all records are hidden (IsHide = true)

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 10
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(3); // Total should be 2 based on filtered results

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_NoSearch_IsHideAll_WithPaging_OrderedAsc()
        {
            // Arrange
            var search = ""; // No search term
            var isHide = "all"; // Include both hidden and non-hidden exercise types
            var pageSize = 10; // Apply paging (not zero)
            var pageNumber = 1; // First page
            var sort = SD.ORDER_ASC; // Sorting in ascending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Assigning Tutor role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with mixed IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = false,
                },
            };

            // Mock the repository call to return all exercise types (including visible and hidden), sorted by CreatedDate in ascending order, with paging
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Ascending order
                    )
                )
                .ReturnsAsync(
                    (exerciseTypes.Count, exerciseTypes.OrderBy(e => e.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(3); // All three items should be returned as isHide = "all" (including both hidden and non-hidden)
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate); // Assert ascending order

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 10
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(exerciseTypes.Count); // Total should be 3 based on all records being included

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_NoSearch_IsHideAll_WithPaging_OrderedDesc()
        {
            // Arrange
            var search = ""; // No search term
            var isHide = "all"; // Include both hidden and non-hidden exercise types
            var pageSize = 10; // Apply paging (not zero)
            var pageNumber = 1; // First page
            var sort = SD.ORDER_DESC; // Sorting in descending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Assigning Tutor role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with mixed IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = false,
                },
            };

            // Mock the repository call to return all exercise types (including visible and hidden), sorted by CreatedDate in descending order, with paging
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count,
                        exerciseTypes.OrderByDescending(e => e.CreatedDate).ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(3); // All three items should be returned as isHide = "all" (including both hidden and non-hidden)
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate); // Assert descending order

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 10
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(exerciseTypes.Count); // Total should be 3 based on all records being included

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_NoSearch_IsHideAll_NoPaging_OrderedDesc()
        {
            // Arrange
            var search = ""; // No search term
            var isHide = "all"; // Include both hidden and non-hidden exercise types
            var pageSize = 0; // No paging (set to 0 for no paging)
            var pageNumber = 1; // First page (not relevant here, since no paging is applied)
            var sort = SD.ORDER_DESC; // Sorting in descending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Assigning Tutor role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with mixed IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = false,
                },
            };

            // Mock the repository call to return all exercise types (including visible and hidden), sorted by CreatedDate in descending order, with no paging
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count,
                        exerciseTypes.OrderByDescending(e => e.CreatedDate).ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(3); // All three items should be returned as isHide = "all" (including both hidden and non-hidden)
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate); // Assert descending order

            // Since search is empty, it should not filter based on ExerciseTypeName
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search));

            // Pagination should still be present even if pageSize is 0
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 0
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(exerciseTypes.Count); // Total should be 3 based on all records being included

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_NoSearch_IsHideAll_NoPaging_OrderedAsc()
        {
            // Arrange
            var search = ""; // No search term provided
            var isHide = "all"; // Include both hidden and non-hidden exercise types
            var pageSize = 0; // No paging (set to 0 for no paging)
            var pageNumber = 1; // First page (not relevant here, since no paging is applied)
            var sort = SD.ORDER_ASC; // Sorting in ascending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Assigning Tutor role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with mixed IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = false,
                },
            };

            // Mock the repository call to return all exercise types (including visible and hidden), sorted by CreatedDate in ascending order, with no paging
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Ascending order
                    )
                )
                .ReturnsAsync(
                    (exerciseTypes.Count, exerciseTypes.OrderBy(e => e.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(3); // All three items should be returned since isHide = "all" includes both hidden and non-hidden items
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate); // Assert ascending order

            // Since no search is provided, it should not filter based on ExerciseTypeName
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search));

            // Pagination should still be present even if pageSize is 0
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 0
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(exerciseTypes.Count); // Total should be 3 based on all records being included

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_NoSearch_IsHideFalse_NoPaging_OrderedAsc()
        {
            // Arrange
            var search = ""; // No search term provided
            var isHide = "false"; // Only non-hidden exercise types
            var pageSize = 0; // No paging (set to 0 for no paging)
            var pageNumber = 1; // First page (not relevant here, since no paging is applied)
            var sort = SD.ORDER_ASC; // Sorting in ascending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Assigning Tutor role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with mixed IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = false,
                },
            };

            // Mock the repository call to return only non-hidden exercise types, sorted by CreatedDate in ascending order, with no paging
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Ascending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count(e => !e.IsHide),
                        exerciseTypes.Where(e => !e.IsHide).OrderBy(e => e.CreatedDate).ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2); // Only 2 non-hidden exercise types should be returned
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate); // Assert ascending order

            // Since no search is provided, it should not filter based on ExerciseTypeName
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search)); // No filtering on ExerciseTypeName

            // Pagination should still be present even if pageSize is 0
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 0
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(2); // Total should be 2 based on non-hidden records

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_NoSearch_IsHideFalse_NoPaging_OrderedDesc()
        {
            // Arrange
            var search = ""; // No search term provided
            var isHide = "false"; // Only non-hidden exercise types
            var pageSize = 0; // No paging (set to 0 for no paging)
            var pageNumber = 1; // First page (not relevant here, since no paging is applied)
            var sort = SD.ORDER_DESC; // Sorting in descending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Assigning Tutor role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with mixed IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = false,
                },
            };

            // Mock the repository call to return only non-hidden exercise types, sorted by CreatedDate in descending order, with no paging
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count(e => !e.IsHide),
                        exerciseTypes
                            .Where(e => !e.IsHide)
                            .OrderByDescending(e => e.CreatedDate)
                            .ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2); // Only 2 non-hidden exercise types should be returned
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate); // Assert descending order

            // Since no search is provided, it should not filter based on ExerciseTypeName
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search)); // No filtering on ExerciseTypeName

            // Pagination should still be present even if pageSize is 0
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 0
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(2); // Total should be 2 based on non-hidden records

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_NoSearch_IsHideFalse_WithPaging_OrderedAsc()
        {
            // Arrange
            var search = ""; // No search term provided
            var isHide = "false"; // Only non-hidden exercise types
            var pageSize = 10; // Pagination size
            var pageNumber = 1; // First page
            var sort = SD.ORDER_ASC; // Sorting in ascending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Assigning Tutor role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with mixed IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = false,
                },
            };

            // Mock the repository call to return only non-hidden exercise types, sorted by CreatedDate in ascending order, with paging
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Ascending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count(e => !e.IsHide),
                        exerciseTypes
                            .Where(e => !e.IsHide)
                            .OrderBy(e => e.CreatedDate)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2); // Only 2 non-hidden exercise types should be returned
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate); // Assert ascending order

            // Since no search is provided, it should not filter based on ExerciseTypeName
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search)); // No filtering on ExerciseTypeName

            // Pagination should be applied correctly
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 10
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(2); // Total should be 2 based on non-hidden records

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_TutorRole_NoSearch_IsHideFalse_WithPaging_OrderedDesc()
        {
            // Arrange
            var search = ""; // No search term provided
            var isHide = "false"; // Only non-hidden exercise types
            var pageSize = 10; // Pagination size
            var pageNumber = 1; // First page
            var sort = SD.ORDER_DESC; // Sorting in descending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Assigning Tutor role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with mixed IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = false,
                },
            };

            // Mock the repository call to return only non-hidden exercise types, sorted by CreatedDate in descending order, with paging
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count(e => !e.IsHide),
                        exerciseTypes
                            .Where(e => !e.IsHide)
                            .OrderByDescending(e => e.CreatedDate)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2); // Only 2 non-hidden exercise types should be returned
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate); // Assert descending order

            // Since no search is provided, it should not filter based on ExerciseTypeName
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search)); // No filtering on ExerciseTypeName

            // Pagination should be applied correctly
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 10
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(2); // Total should be 2 based on non-hidden records

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_NoSearch_IsHideTrue_NoPaging_OrderedDesc()
        {
            // Arrange
            var search = ""; // No search term provided
            var isHide = "true"; // Only hidden exercise types
            var pageSize = 0; // No paging
            var pageNumber = 1; // First page (should be ignored since no paging)
            var sort = SD.ORDER_DESC; // Sorting in descending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Assigning Staff role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
            var submitterUser = new ApplicationUser { Id = "testStaffId", UserName = "testStaff" };

            // Sample exercise types with mixed IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = false,
                },
            };

            // Mock the repository call to return only hidden exercise types, sorted by CreatedDate in descending order, with no paging
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count(e => e.IsHide),
                        exerciseTypes
                            .Where(e => e.IsHide)
                            .OrderByDescending(e => e.CreatedDate)
                            .ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2); // Only 2 hidden exercise types should be returned
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate); // Assert descending order

            // Since no search is provided, it should not filter based on ExerciseTypeName
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search) && e.IsHide); // Only hidden exercise types

            // Pagination should be applied correctly
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 0 (no paging)
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(2); // Total should be 2 based on hidden records

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_NoSearch_IsHideTrue_NoPaging_OrderedAsc()
        {
            // Arrange
            var search = ""; // No search term provided
            var isHide = "true"; // Only hidden exercise types
            var pageSize = 0; // No paging
            var pageNumber = 1; // First page (but ignored due to no paging)
            var sort = SD.ORDER_ASC; // Sorting in ascending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Assigning Staff role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
            var submitterUser = new ApplicationUser { Id = "testStaffId", UserName = "testStaff" };

            // Sample exercise types with different IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = false,
                },
            };

            // Mock the repository call to return only hidden exercise types, sorted by CreatedDate in ascending order, with no paging
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Ascending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count(e => e.IsHide),
                        exerciseTypes.Where(e => e.IsHide).OrderBy(e => e.CreatedDate).ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2); // Only 2 hidden exercise types should be returned
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate); // Assert ascending order

            // Since no search is provided, it should not filter based on ExerciseTypeName
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search) && e.IsHide); // Only hidden exercise types

            // Pagination should be applied correctly
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 0 (no paging)
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(2); // Total should be 2 based on hidden records

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_NoSearch_IsHideTrue_WithPaging_OrderedAsc()
        {
            // Arrange
            var search = ""; // No search term provided
            var isHide = "true"; // Only hidden exercise types
            var pageSize = 10; // Page size greater than 0
            var pageNumber = 1; // First page
            var sort = SD.ORDER_ASC; // Sorting in ascending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Assigning Staff role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with different IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = true,
                },
            };

            // Mock the repository call to return only hidden exercise types, with paging, sorted by CreatedDate in ascending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Ascending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count(e => e.IsHide),
                        exerciseTypes
                            .Where(e => e.IsHide)
                            .OrderBy(e => e.CreatedDate)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2); // Only 2 hidden exercise types should be returned
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate); // Assert ascending order

            // Since no search is provided, it should not filter based on ExerciseTypeName
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search) && e.IsHide); // Only hidden exercise types

            // Pagination should be applied correctly
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 10
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(2); // Total should be 2 based on hidden records

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_NoSearch_IsHideTrue_WithPaging_OrderedDesc()
        {
            // Arrange
            var search = ""; // No search term provided
            var isHide = "true"; // Only hidden exercise types
            var pageSize = 10; // Page size greater than 0
            var pageNumber = 1; // First page
            var sort = SD.ORDER_DESC; // Sorting in descending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Assigning Staff role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with different IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = true,
                },
            };

            // Mock the repository call to return only hidden exercise types, with paging, sorted by CreatedDate in descending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count(e => e.IsHide),
                        exerciseTypes
                            .Where(e => e.IsHide)
                            .OrderByDescending(e => e.CreatedDate)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2); // Only 2 hidden exercise types should be returned
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate); // Assert descending order

            // Since no search is provided, it should not filter based on ExerciseTypeName
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search) && e.IsHide); // Only hidden exercise types

            // Pagination should be applied correctly
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 10
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(2); // Total should be 2 based on hidden records

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_NoSearch_IsHideAll_WithPaging_OrderedDesc()
        {
            // Arrange
            var search = ""; // No search term provided
            var isHide = "all"; // "all" for both visible and hidden exercise types
            var pageSize = 10; // Page size greater than 0
            var pageNumber = 1; // First page
            var sort = SD.ORDER_DESC; // Sorting in descending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Assigning Staff role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with different IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = true,
                },
            };

            // Mock the repository call to return both hidden and visible exercise types, with paging, sorted by CreatedDate in descending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count,
                        exerciseTypes
                            .OrderByDescending(e => e.CreatedDate)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(3); // All exercise types should be returned (including hidden ones)
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate); // Assert descending order

            // Since no search is provided, it should not filter based on ExerciseTypeName
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search)); // No filtering by search

            // Pagination should be applied correctly
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 10
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(exerciseTypes.Count); // Total should be the count of all exercise types (3)

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_NoSearch_IsHideAll_WithPaging_OrderedAsc()
        {
            // Arrange
            var search = ""; // No search term provided
            var isHide = "all"; // "all" for both visible and hidden exercise types
            var pageSize = 10; // Page size greater than 0
            var pageNumber = 1; // First page
            var sort = SD.ORDER_ASC; // Sorting in ascending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Assigning Staff role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with different IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = true,
                },
            };

            // Mock the repository call to return both hidden and visible exercise types, with paging, sorted by CreatedDate in ascending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Ascending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count,
                        exerciseTypes
                            .OrderBy(e => e.CreatedDate)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(3); // All exercise types should be returned (including hidden ones)
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate); // Assert ascending order

            // Since no search is provided, it should not filter based on ExerciseTypeName
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search)); // No filtering by search

            // Pagination should be applied correctly
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 10
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(exerciseTypes.Count); // Total should be the count of all exercise types (3)

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_NoSearch_IsHideAll_NoPaging_OrderedAsc()
        {
            // Arrange
            var search = ""; // No search term provided
            var isHide = "all"; // "all" means both visible and hidden exercise types should be included
            var pageSize = 0; // No paging (0 indicates no limit on page size)
            var pageNumber = 1; // First page (not relevant as pageSize is 0)
            var sort = SD.ORDER_ASC; // Sorting in ascending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Assigning Staff role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with different IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = true,
                },
            };

            // Mock the repository call to return both hidden and visible exercise types, sorted by CreatedDate in ascending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Ascending order
                    )
                )
                .ReturnsAsync(
                    (exerciseTypes.Count, exerciseTypes.OrderBy(e => e.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(3); // All exercise types should be returned (including hidden ones)
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate); // Assert ascending order

            // Since no search is provided, it should not filter based on ExerciseTypeName
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search)); // No filtering by search

            // Pagination should not be applied since pageSize is 0
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 0
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(exerciseTypes.Count); // Total should be the count of all exercise types (3)

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_NoSearch_IsHideAll_NoPaging_OrderedDesc()
        {
            // Arrange
            var search = ""; // No search term provided
            var isHide = "all"; // "all" means both visible and hidden exercise types should be included
            var pageSize = 0; // No paging (0 indicates no limit on page size)
            var pageNumber = 1; // First page (not relevant as pageSize is 0)
            var sort = SD.ORDER_DESC; // Sorting in descending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Assigning Staff role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with different IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = true,
                },
            };

            // Mock the repository call to return both hidden and visible exercise types, sorted by CreatedDate in descending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count,
                        exerciseTypes.OrderByDescending(e => e.CreatedDate).ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(3); // All exercise types should be returned (including hidden ones)
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate); // Assert descending order

            // Since no search is provided, it should not filter based on ExerciseTypeName
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search)); // No filtering by search

            // Pagination should not be applied since pageSize is 0
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 0
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(exerciseTypes.Count); // Total should be the count of all exercise types (3)

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_NoSearch_IsHideFalse_NoPaging_OrderedDesc()
        {
            // Arrange
            var search = ""; // No search term provided
            var isHide = "false"; // "false" means only visible exercise types should be included
            var pageSize = 0; // No paging (0 indicates no limit on page size)
            var pageNumber = 1; // First page (not relevant as pageSize is 0)
            var sort = SD.ORDER_DESC; // Sorting in descending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Assigning Staff role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with different IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = false,
                },
            };

            // Mock the repository call to return only visible (IsHide = false) exercise types, sorted by CreatedDate in descending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count(e => !e.IsHide),
                        exerciseTypes
                            .Where(e => !e.IsHide)
                            .OrderByDescending(e => e.CreatedDate)
                            .ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2); // Only visible exercise types should be returned (IsHide = false)
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate); // Assert descending order

            // Since no search is provided, it should not filter based on ExerciseTypeName
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search)); // No filtering by search

            // Pagination should not be applied since pageSize is 0
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 0
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(exerciseTypes.Count(e => !e.IsHide)); // Total should be the count of visible exercise types (2)

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_NoSearch_IsHideFalse_NoPaging_OrderedAsc()
        {
            // Arrange
            var search = ""; // No search term provided
            var isHide = "false"; // "false" means only visible exercise types should be included
            var pageSize = 0; // No paging (0 indicates no limit on page size)
            var pageNumber = 1; // First page (not relevant as pageSize is 0)
            var sort = SD.ORDER_ASC; // Sorting in ascending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Assigning Staff role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with different IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = true,
                },
            };

            // Mock the repository call to return only visible (IsHide = false) exercise types, sorted by CreatedDate in ascending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Ascending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count(e => !e.IsHide),
                        exerciseTypes.Where(e => !e.IsHide).OrderBy(e => e.CreatedDate).ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2); // Only visible exercise types should be returned (IsHide = false)
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate); // Assert ascending order

            // Since no search is provided, it should not filter based on ExerciseTypeName
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search)); // No filtering by search

            // Pagination should not be applied since pageSize is 0
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 0
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(exerciseTypes.Count(e => !e.IsHide)); // Total should be the count of visible exercise types (2)

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_NoSearch_IsHideFalse_WithPaging_OrderedAsc()
        {
            // Arrange
            var search = ""; // No search term provided
            var isHide = "false"; // Only visible exercise types (IsHide = false)
            var pageSize = 5; // Paging with a specified page size
            var pageNumber = 1; // First page
            var sort = SD.ORDER_ASC; // Ascending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Assigning Staff role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with different IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 4,
                    ExerciseTypeName = "Test Exercise 4",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-30),
                    IsHide = false,
                },
            };

            // Mock the repository call to return only visible (IsHide = false) exercise types with paging, sorted by CreatedDate in ascending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Ascending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count(e => !e.IsHide),
                        exerciseTypes
                            .Where(e => !e.IsHide)
                            .OrderBy(e => e.CreatedDate)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(3); // Only visible exercise types (IsHide = false) should be returned (3 in this case)
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate); // Assert ascending order

            // Since no search is provided, it should not filter based on ExerciseTypeName
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search)); // No filtering by search

            // Pagination should be applied correctly
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 5
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(exerciseTypes.Count(e => !e.IsHide)); // Total should be the count of visible exercise types (3)

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_NoSearch_IsHideFalse_WithPaging_OrderedDesc()
        {
            // Arrange
            var search = ""; // No search term provided
            var isHide = "false"; // Only visible exercise types (IsHide = false)
            var pageSize = 5; // Paging with a specified page size
            var pageNumber = 1; // First page
            var sort = SD.ORDER_DESC; // Descending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Assigning Staff role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with different IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 4,
                    ExerciseTypeName = "Test Exercise 4",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-30),
                    IsHide = false,
                },
            };

            // Mock the repository call to return only visible (IsHide = false) exercise types with paging, sorted by CreatedDate in descending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count(e => !e.IsHide),
                        exerciseTypes
                            .Where(e => !e.IsHide)
                            .OrderByDescending(e => e.CreatedDate)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(3); // Only visible exercise types (IsHide = false) should be returned (3 in this case)
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate); // Assert descending order

            // Since no search is provided, it should not filter based on ExerciseTypeName
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search)); // No filtering by search

            // Pagination should be applied correctly
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 5
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(exerciseTypes.Count(e => !e.IsHide)); // Total should be the count of visible exercise types (3)

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_WithSearch_IsHideTrue_WithPaging_OrderedDesc()
        {
            // Arrange
            var search = "Test"; // Search term provided
            var isHide = "true"; // Only hidden exercise types (IsHide = true)
            var pageSize = 5; // Paging with a specified page size
            var pageNumber = 1; // First page
            var sort = SD.ORDER_DESC; // Descending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Assigning Staff role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with different IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 4,
                    ExerciseTypeName = "Test Exercise 4",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-30),
                    IsHide = true,
                },
            };

            // Mock the repository call to return only hidden (IsHide = true) exercise types with paging, sorted by CreatedDate in descending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count(e => e.IsHide),
                        exerciseTypes
                            .Where(e => e.IsHide)
                            .OrderByDescending(e => e.CreatedDate)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(3); // Only hidden exercise types (IsHide = true) should be returned (3 in this case)
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate); // Assert descending order

            // Search should be applied to ExerciseTypeName, only returning exercises that contain the search term
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search));

            // Pagination should be applied correctly
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 5
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(exerciseTypes.Count(e => e.IsHide)); // Total should be the count of hidden exercise types (3)

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_WithSearch_IsHideTrue_WithPaging_OrderedAsc()
        {
            // Arrange
            var search = "Test"; // Search term provided
            var isHide = "true"; // Only hidden exercise types (IsHide = true)
            var pageSize = 10; // Paging with a specified page size
            var pageNumber = 1; // First page
            var sort = SD.ORDER_ASC; // Ascending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Assigning Staff role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with different IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 4,
                    ExerciseTypeName = "Test Exercise 4",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-30),
                    IsHide = true,
                },
            };

            // Mock the repository call to return only hidden (IsHide = true) exercise types with paging, sorted by CreatedDate in ascending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Ascending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count(e => e.IsHide),
                        exerciseTypes
                            .Where(e => e.IsHide)
                            .OrderBy(e => e.CreatedDate)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(3); // Only hidden exercise types (IsHide = true) should be returned (3 in this case)
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate); // Assert ascending order

            // Search should be applied to ExerciseTypeName, only returning exercises that contain the search term
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search));

            // Pagination should be applied correctly
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 10
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(exerciseTypes.Count(e => e.IsHide)); // Total should be the count of hidden exercise types (3)

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_WithSearch_IsHideAll_WithPaging_OrderedAsc()
        {
            // Arrange
            var search = "Test"; // Search term provided
            var isHide = "all"; // Considering both hidden and visible exercise types (IsHide = all)
            var pageSize = 10; // Paging with a specified page size
            var pageNumber = 1; // First page
            var sort = SD.ORDER_ASC; // Ascending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Assigning Staff role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with different IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 4,
                    ExerciseTypeName = "Test Exercise 4",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-30),
                    IsHide = false,
                },
            };

            // Mock the repository call to return all exercise types (no filtering by IsHide) with paging, sorted by CreatedDate in ascending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Ascending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count,
                        exerciseTypes
                            .OrderBy(e => e.CreatedDate)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(4); // Since isHide = "all", we expect all exercise types (4 in total)
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate); // Assert ascending order

            // Search should be applied to ExerciseTypeName, only returning exercises that contain the search term
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search));

            // Pagination should be applied correctly
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 10
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(exerciseTypes.Count); // Total should be the count of all exercise types (4)

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_WithSearch_IsHideAll_WithPaging_OrderedDesc()
        {
            // Arrange
            var search = "Test"; // Search term provided
            var isHide = "all"; // Considering both hidden and visible exercise types (IsHide = all)
            var pageSize = 10; // Paging with a specified page size
            var pageNumber = 1; // First page
            var sort = SD.ORDER_DESC; // Descending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Assigning Staff role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with different IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 4,
                    ExerciseTypeName = "Test Exercise 4",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-30),
                    IsHide = false,
                },
            };

            // Mock the repository call to return all exercise types (no filtering by IsHide) with paging, sorted by CreatedDate in descending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count,
                        exerciseTypes
                            .OrderByDescending(e => e.CreatedDate)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(4); // Since isHide = "all", we expect all exercise types (4 in total)
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate); // Assert descending order

            // Search should be applied to ExerciseTypeName, only returning exercises that contain the search term
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search));

            // Pagination should be applied correctly
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 10
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(exerciseTypes.Count); // Total should be the count of all exercise types (4)

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_WithSearch_IsHideFalse_WithPaging_OrderedDesc()
        {
            // Arrange
            var search = "Test"; // Search term provided
            var isHide = "false"; // Only exercise types that are not hidden (IsHide = false)
            var pageSize = 10; // Paging with a specified page size
            var pageNumber = 1; // First page
            var sort = SD.ORDER_DESC; // Descending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Assigning Staff role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with different IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 4,
                    ExerciseTypeName = "Test Exercise 4",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-30),
                    IsHide = false,
                },
            };

            // Mock the repository call to return only non-hidden exercise types with paging, sorted by CreatedDate in descending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Where(e => !e.IsHide).Count(),
                        exerciseTypes
                            .Where(e => !e.IsHide)
                            .OrderByDescending(e => e.CreatedDate)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(3); // Only non-hidden exercise types (IsHide = false)
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate); // Assert descending order

            // Search should be applied to ExerciseTypeName, only returning exercises that contain the search term
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search));

            // Pagination should be applied correctly
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 10
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(exerciseTypes.Count(e => !e.IsHide)); // Total should be the count of non-hidden exercise types (3)

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_WithSearch_IsHideFalse_WithPaging_OrderedAsc()
        {
            // Arrange
            var search = "Test"; // Search term provided
            var isHide = "false"; // Only exercise types that are not hidden (IsHide = false)
            var pageSize = 10; // Paging with a specified page size
            var pageNumber = 1; // First page
            var sort = SD.ORDER_ASC; // Ascending order
            var orderBy = SD.CREATED_DATE; // Ordering by CreatedDate

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Assigning Staff role
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with different IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-20),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 4,
                    ExerciseTypeName = "Test Exercise 4",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-30),
                    IsHide = false,
                },
            };

            // Mock the repository call to return only non-hidden exercise types with paging, sorted by CreatedDate in ascending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify asc order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Where(e => !e.IsHide).Count(),
                        exerciseTypes
                            .Where(e => !e.IsHide)
                            .OrderBy(e => e.CreatedDate)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(3); // Only non-hidden exercise types (IsHide = false)
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate); // Assert ascending order

            // Search should be applied to ExerciseTypeName, only returning exercises that contain the search term
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search));

            // Pagination should be applied correctly
            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize); // PageSize should be 10
            response.Pagination.PageNumber.Should().Be(pageNumber); // PageNumber should be 1
            response.Pagination.Total.Should().Be(exerciseTypes.Count(e => !e.IsHide)); // Total should be the count of non-hidden exercise types (3)

            // Verify that the repository method was called once with the correct parameters
            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify asc order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_SearchAndIsHideTrue_NoPaging_OrderedAsc()
        {
            // Arrange
            var search = "Test";
            var isHide = "true";
            var pageSize = 0;
            var pageNumber = 1;
            var sort = SD.ORDER_ASC;
            var orderBy = SD.CREATED_DATE;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Updated to STAFF_ROLE
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = true,
                },
            };

            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Set to false for ascending order
                    )
                )
                .ReturnsAsync(
                    (exerciseTypes.Count, exerciseTypes.OrderBy(e => e.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2);
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate);
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search) && e.IsHide);

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.Total.Should().Be(exerciseTypes.Count);

            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_SearchAndIsHideTrue_NoPaging_OrderedDesc()
        {
            // Arrange
            var search = "Test";
            var isHide = "true";
            var pageSize = 0;
            var pageNumber = 1;
            var sort = SD.ORDER_DESC; // Set to descending order
            var orderBy = SD.CREATED_DATE;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Role changed to STAFF_ROLE
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = true,
                },
            };

            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Set to true for descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count,
                        exerciseTypes.OrderByDescending(e => e.CreatedDate).ToList()
                    )
                ); // Order by CreatedDate descending

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(2);
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate); // Assert descending order
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search) && e.IsHide); // Assert filtering based on search and IsHide

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.Total.Should().Be(exerciseTypes.Count);

            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_SearchAndIsHideAll_NoPaging_OrderedDesc()
        {
            // Arrange
            var search = "Test";
            var isHide = "All"; // isHide is set to "All"
            var pageSize = 0;
            var pageNumber = 1;
            var sort = SD.ORDER_DESC; // Set to descending order
            var orderBy = SD.CREATED_DATE;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Role is STAFF_ROLE
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-5),
                    IsHide = true,
                },
            };

            // Setup the repository mock to return the exercise types ordered by CreatedDate in descending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Set to true for descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count,
                        exerciseTypes.OrderByDescending(e => e.CreatedDate).ToList()
                    )
                ); // Order by CreatedDate descending

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;
            resultList.Should().HaveCount(3); // Should return all exercises since isHide = "All"
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate); // Assert descending order
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search)); // Assert filtering based on search

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.Total.Should().Be(exerciseTypes.Count);

            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_SearchAndIsHideAll_NoPaging_OrderedAsc()
        {
            // Arrange
            var search = "Test";
            var isHide = "All"; // isHide is set to "All"
            var pageSize = 0;
            var pageNumber = 1;
            var sort = SD.ORDER_ASC; // Set to ascending order
            var orderBy = SD.CREATED_DATE;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Role is STAFF_ROLE
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with different IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-5),
                    IsHide = true,
                },
            };

            // Setup the repository mock to return the exercise types ordered by CreatedDate in ascending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Set to false for ascending order
                    )
                )
                .ReturnsAsync(
                    (exerciseTypes.Count, exerciseTypes.OrderBy(e => e.CreatedDate).ToList())
                ); // Order by CreatedDate ascending

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;

            // Ensure all exercise types are included, since isHide is "All"
            resultList.Should().HaveCount(3);
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate); // Assert ascending order
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search)); // Assert filtering based on search term

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.Total.Should().Be(exerciseTypes.Count);

            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_SearchAndIsHideFalse_NoPaging_OrderedDesc()
        {
            // Arrange
            var search = "Test";
            var isHide = "false"; // isHide is set to "false"
            var pageSize = 0;
            var pageNumber = 1;
            var sort = SD.ORDER_DESC; // Set to descending order
            var orderBy = SD.CREATED_DATE;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Role is STAFF_ROLE
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with different IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = true,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-5),
                    IsHide = false,
                },
            };

            // Setup the repository mock to return the exercise types ordered by CreatedDate in descending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Set to true for descending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count(e => !e.IsHide),
                        exerciseTypes
                            .Where(e => e.IsHide == false)
                            .OrderByDescending(e => e.CreatedDate)
                            .ToList()
                    )
                ); // Order by CreatedDate descending and filter where IsHide is false

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;

            // Ensure only exercise types with IsHide = false are returned
            resultList.Should().HaveCount(2);
            resultList.Should().BeInDescendingOrder(e => e.CreatedDate); // Assert descending order
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search) && !e.IsHide); // Assert filtering based on search term and IsHide = false

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.Total.Should().Be(exerciseTypes.Count(e => !e.IsHide));

            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        true // Verify descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExerciseTypesAsync_StaffRole_SearchAndIsHideFalse_NoPaging_OrderedAsc()
        {
            // Arrange
            var search = "Test";
            var isHide = "false"; // isHide is set to "false"
            var pageSize = 0;
            var pageNumber = 1;
            var sort = SD.ORDER_ASC; // Set to ascending order
            var orderBy = SD.CREATED_DATE;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Role is STAFF_ROLE
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Sample exercise types with different IsHide values
            var exerciseTypes = new List<ExerciseType>
            {
                new ExerciseType
                {
                    Id = 1,
                    ExerciseTypeName = "Test Exercise 2",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 2,
                    ExerciseTypeName = "Test Exercise 1",
                    CreatedDate = DateTime.UtcNow,
                    IsHide = false,
                },
                new ExerciseType
                {
                    Id = 3,
                    ExerciseTypeName = "Test Exercise 3",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-5),
                    IsHide = true,
                },
            };

            // Setup the repository mock to return the exercise types ordered by CreatedDate in ascending order
            _exerciseTypeRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Set to false for ascending order
                    )
                )
                .ReturnsAsync(
                    (
                        exerciseTypes.Count(e => !e.IsHide),
                        exerciseTypes
                            .Where(e => e.IsHide == false)
                            .OrderBy(e => e.CreatedDate)
                            .ToList()
                    )
                ); // Order by CreatedDate ascending and filter where IsHide is false

            // Act
            var result = await _controller.GetAllExerciseTypesAsync(
                search,
                isHide,
                orderBy,
                sort,
                pageSize,
                pageNumber
            );
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseTypeDTO>>();
            var resultList = response.Result as List<ExerciseTypeDTO>;

            // Ensure only exercise types with IsHide = false are returned
            resultList.Should().HaveCount(2);
            resultList.Should().BeInAscendingOrder(e => e.CreatedDate); // Assert ascending order
            resultList.Should().OnlyContain(e => e.ExerciseTypeName.Contains(search) && !e.IsHide); // Assert filtering based on search term and IsHide = false

            response.Pagination.Should().NotBeNull();
            response.Pagination.PageSize.Should().Be(pageSize);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.Total.Should().Be(exerciseTypes.Count(e => !e.IsHide));

            _exerciseTypeRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ExerciseType, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<ExerciseType, object>>>(),
                        false // Verify ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetExercisesByTypeAsync_ReturnsInternalServerError_WhenExceptionOccurs()
        {
            var id = 1;
            var search = "Test Exercise";
            var pageNumber = 1;
            var orderBy = SD.CREATED_DATE;
            var sort = SD.ORDER_DESC;
            // Arrange

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Role is STAFF_ROLE
                ,
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Lỗi hệ thống vui lòng thử lại sau.");

            _exerciseRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Exercise, bool>>>(),
                        null,
                        10,
                        pageNumber,
                        It.IsAny<Expression<Func<Exercise, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ThrowsAsync(new Exception("Lỗi hệ thống vui lòng thử lại sau."));
            // Act
            var result = await _controller.GetExercisesByTypeAsync(
                id,
                search,
                pageNumber,
                orderBy,
                sort
            );

            var errorResult = result.Result as ObjectResult;

            // Assert
            errorResult.Should().NotBeNull();
            errorResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var response = errorResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            response
                .ErrorMessages.Should()
                .ContainSingle(error =>
                    error == _resourceServiceMock.Object.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE)
                );
        }

        [Fact]
        public async Task GetExercisesByTypeAsync_ReturnsUnauthorized_WhenUserIsUnauthorized()
        {
            var id = 1;
            var search = "Test Exercise";
            var pageNumber = 1;
            var orderBy = SD.CREATED_DATE;
            var sort = SD.ORDER_DESC;
            // Arrange
            _resourceServiceMock
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");

            // Simulate a user with no valid claims (unauthorized)
            var claimsIdentity = new ClaimsIdentity(); // No claims added
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Act
            var result = await _controller.GetExercisesByTypeAsync(
                id,
                search,
                pageNumber,
                orderBy,
                sort
            );

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
        public async Task GetExercisesByTypeAsync_ReturnsForbiden_WhenUserDoesNotHaveRequiredRole()
        {
            var id = 1;
            var search = "Test Exercise";
            var pageNumber = 1;
            var orderBy = SD.CREATED_DATE;
            var sort = SD.ORDER_DESC;
            // Arrange
            _resourceServiceMock
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbiden access.");

            // Simulate a user with no valid claims (unauthorized)
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "testUserId") };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Act
            var result = await _controller.GetExercisesByTypeAsync(
                id,
                search,
                pageNumber,
                orderBy,
                sort
            );

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
        public async Task GetExercisesByTypeAsync_InvalidExerciseId_ReturnsBadRequestResult()
        {
            // Arrange
            var id = -1; // Invalid Exercise ID
            var search = "Test Exercise";
            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("ID invalid.");
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Act
            var result = await _controller.GetExercisesByTypeAsync(id, search);
            var badRequestResult = result.Result as ObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            apiResponse.ErrorMessages.First().Should().Be("ID invalid.");
        }

        [Fact]
        public async Task GetExercisesByTypeAsync_TutorRole_WithSearch_OrderByCreatedDate_Asc_ReturnsOk()
        {
            // Arrange
            var userId = "test-tutor-id";
            var exerciseTypeId = 1;
            var search = "test";
            var pageNumber = 1;
            var orderBy = SD.CREATED_DATE;
            var sort = SD.ORDER_ASC; // Ascending order

            // Mock the user claims
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var mockUser = new ClaimsPrincipal(new ClaimsIdentity(userClaims));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = mockUser },
            };

            // Mock response data
            var exercises = new List<Exercise>
            {
                new Exercise
                {
                    Id = 1,
                    ExerciseName = "Test Exercise 1",
                    ExerciseTypeId = exerciseTypeId,
                    TutorId = userId,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Exercise
                {
                    Id = 2,
                    ExerciseName = "Test Exercise 2",
                    ExerciseTypeId = exerciseTypeId,
                    TutorId = userId,
                    CreatedDate = DateTime.Now,
                },
            };

            var count = exercises.Count;
            _exerciseRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Exercise, bool>>>(),
                        null,
                        10,
                        pageNumber,
                        It.IsAny<Expression<Func<Exercise, object>>>(),
                        false
                    )
                )
                .ReturnsAsync((count, exercises));

            // Mock the mapping to ExerciseDTO
            var exerciseDTOs = new List<ExerciseDTO>
            {
                new ExerciseDTO
                {
                    Id = 1,
                    ExerciseName = "Test Exercise 1",
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new ExerciseDTO
                {
                    Id = 2,
                    ExerciseName = "Test Exercise 2",
                    CreatedDate = DateTime.Now,
                },
            };

            // Mock the resource service
            _resourceServiceMock
                .Setup(rs => rs.GetString(It.IsAny<string>()))
                .Returns("Some Resource String");

            // Act
            var result = await _controller.GetExercisesByTypeAsync(
                exerciseTypeId,
                search,
                pageNumber,
                orderBy,
                sort
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseDTO>>();
            var resultList = response.Result as List<ExerciseDTO>;

            // Ensure the result contains the correct number of exercises
            resultList.Should().HaveCount(2);

            // Ensure pagination is correctly set
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(10);
            response.Pagination.Total.Should().Be(count);

            // Check that the filter includes search and correct ordering
            _exerciseRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Exercise, bool>>>(),
                        null,
                        10,
                        pageNumber,
                        It.IsAny<Expression<Func<Exercise, object>>>(),
                        false
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetExercisesByTypeAsync_TutorRole_WithSearch_OrderByCreatedDate_Desc_ReturnsOk()
        {
            // Arrange
            var userId = "test-tutor-id";
            var exerciseTypeId = 1;
            var search = "test";
            var pageNumber = 1;
            var orderBy = SD.CREATED_DATE;
            var sort = SD.ORDER_DESC; // Descending order

            // Mock the user claims
            var userClaims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userId),
        new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
    };
            var mockUser = new ClaimsPrincipal(new ClaimsIdentity(userClaims));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = mockUser },
            };

            // Mock response data
            var exercises = new List<Exercise>
    {
        new Exercise
        {
            Id = 1,
            ExerciseName = "Test Exercise 1",
            ExerciseTypeId = exerciseTypeId,
            TutorId = userId,
            CreatedDate = DateTime.Now.AddDays(-1), // Older date
        },
        new Exercise
        {
            Id = 2,
            ExerciseName = "Test Exercise 2",
            ExerciseTypeId = exerciseTypeId,
            TutorId = userId,
            CreatedDate = DateTime.Now, // Newer date
        },
    };

            var count = exercises.Count;
            _exerciseRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Exercise, bool>>>(),
                        null,
                        10,
                        pageNumber,
                        It.IsAny<Expression<Func<Exercise, object>>>(),
                        true // isDesc is true for descending order
                    )
                )
                .ReturnsAsync((count, exercises));

            // Mock the mapping to ExerciseDTO
            var exerciseDTOs = new List<ExerciseDTO>
    {
        new ExerciseDTO
        {
            Id = 1,
            ExerciseName = "Test Exercise 1",
            CreatedDate = DateTime.Now.AddDays(-1),
        },
        new ExerciseDTO
        {
            Id = 2,
            ExerciseName = "Test Exercise 2",
            CreatedDate = DateTime.Now,
        },
    };

            // Mock the resource service
            _resourceServiceMock
                .Setup(rs => rs.GetString(It.IsAny<string>()))
                .Returns("Some Resource String");

            // Act
            var result = await _controller.GetExercisesByTypeAsync(
                exerciseTypeId,
                search,
                pageNumber,
                orderBy,
                sort
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseDTO>>();
            var resultList = response.Result as List<ExerciseDTO>;

            // Ensure the result contains the correct number of exercises
            resultList.Should().HaveCount(2);

            // Ensure pagination is correctly set
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(10);
            response.Pagination.Total.Should().Be(count);

            // Check that the filter includes search and correct ordering
            _exerciseRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Exercise, bool>>>(),
                        null,
                        10,
                        pageNumber,
                        It.IsAny<Expression<Func<Exercise, object>>>(),
                        true // Verifying isDesc is true for descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetExercisesByTypeAsync_TutorRole_WithNoSearch_OrderByCreatedDate_Desc_ReturnsOk()
        {
            // Arrange
            var userId = "test-tutor-id";
            var exerciseTypeId = 1;
            var search = string.Empty; // No search term
            var pageNumber = 1;
            var orderBy = SD.CREATED_DATE;
            var sort = SD.ORDER_DESC; // Descending order

            // Mock the user claims
            var userClaims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userId),
        new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
    };
            var mockUser = new ClaimsPrincipal(new ClaimsIdentity(userClaims));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = mockUser },
            };

            // Mock response data
            var exercises = new List<Exercise>
    {
        new Exercise
        {
            Id = 1,
            ExerciseName = "Test Exercise 1",
            ExerciseTypeId = exerciseTypeId,
            TutorId = userId,
            CreatedDate = DateTime.Now.AddDays(-1), // Older date
        },
        new Exercise
        {
            Id = 2,
            ExerciseName = "Test Exercise 2",
            ExerciseTypeId = exerciseTypeId,
            TutorId = userId,
            CreatedDate = DateTime.Now, // Newer date
        },
    };

            var count = exercises.Count;
            _exerciseRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Exercise, bool>>>(),
                        null,
                        10,
                        pageNumber,
                        It.IsAny<Expression<Func<Exercise, object>>>(),
                        true // isDesc is true for descending order
                    )
                )
                .ReturnsAsync((count, exercises));

            // Mock the mapping to ExerciseDTO
            var exerciseDTOs = new List<ExerciseDTO>
    {
        new ExerciseDTO
        {
            Id = 1,
            ExerciseName = "Test Exercise 1",
            CreatedDate = DateTime.Now.AddDays(-1),
        },
        new ExerciseDTO
        {
            Id = 2,
            ExerciseName = "Test Exercise 2",
            CreatedDate = DateTime.Now,
        },
    };

            // Mock the resource service
            _resourceServiceMock
                .Setup(rs => rs.GetString(It.IsAny<string>()))
                .Returns("Some Resource String");

            // Act
            var result = await _controller.GetExercisesByTypeAsync(
                exerciseTypeId,
                search,
                pageNumber,
                orderBy,
                sort
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseDTO>>();
            var resultList = response.Result as List<ExerciseDTO>;

            // Ensure the result contains the correct number of exercises
            resultList.Should().HaveCount(2);

            // Ensure pagination is correctly set
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(10);
            response.Pagination.Total.Should().Be(count);

            // Check that the filter includes no search and correct ordering
            _exerciseRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Exercise, bool>>>(),
                        null,
                        10,
                        pageNumber,
                        It.IsAny<Expression<Func<Exercise, object>>>(),
                        true // Verifying isDesc is true for descending order
                    ),
                Times.Once
            );
        }


        [Fact]
        public async Task GetExercisesByTypeAsync_TutorRole_WithNoSearch_OrderByCreatedDate_Asc_ReturnsOk()
        {
            // Arrange
            var userId = "test-tutor-id";
            var exerciseTypeId = 1;
            var search = string.Empty;  // No search
            var pageNumber = 1;
            var orderBy = SD.CREATED_DATE;
            var sort = SD.ORDER_ASC; // Ascending order

            // Mock the user claims
            var userClaims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userId),
        new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
    };
            var mockUser = new ClaimsPrincipal(new ClaimsIdentity(userClaims));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = mockUser },
            };

            // Mock response data
            var exercises = new List<Exercise>
    {
        new Exercise
        {
            Id = 1,
            ExerciseName = "Test Exercise 1",
            ExerciseTypeId = exerciseTypeId,
            TutorId = userId,
            CreatedDate = DateTime.Now.AddDays(-1),
        },
        new Exercise
        {
            Id = 2,
            ExerciseName = "Test Exercise 2",
            ExerciseTypeId = exerciseTypeId,
            TutorId = userId,
            CreatedDate = DateTime.Now,
        },
    };

            var count = exercises.Count;
            _exerciseRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Exercise, bool>>>(),
                        null,
                        10,
                        pageNumber,
                        It.IsAny<Expression<Func<Exercise, object>>>(),
                        false
                    )
                )
                .ReturnsAsync((count, exercises));

            // Mock the mapping to ExerciseDTO
            var exerciseDTOs = new List<ExerciseDTO>
    {
        new ExerciseDTO
        {
            Id = 1,
            ExerciseName = "Test Exercise 1",
            CreatedDate = DateTime.Now.AddDays(-1),
        },
        new ExerciseDTO
        {
            Id = 2,
            ExerciseName = "Test Exercise 2",
            CreatedDate = DateTime.Now,
        },
    };

            // Mock the resource service
            _resourceServiceMock
                .Setup(rs => rs.GetString(It.IsAny<string>()))
                .Returns("Some Resource String");

            // Act
            var result = await _controller.GetExercisesByTypeAsync(
                exerciseTypeId,
                search,
                pageNumber,
                orderBy,
                sort
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Result.Should().BeOfType<List<ExerciseDTO>>();
            var resultList = response.Result as List<ExerciseDTO>;

            // Ensure the result contains the correct number of exercises
            resultList.Should().HaveCount(2);

            // Ensure pagination is correctly set
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(10);
            response.Pagination.Total.Should().Be(count);

            // Check that the filter includes search and correct ordering (ascending order)
            _exerciseRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Exercise, bool>>>(),
                        null,
                        10,
                        pageNumber,
                        It.Is<Expression<Func<Exercise, object>>>(exp => exp.Body.ToString().Contains("CreatedDate")),
                        false
                    ),
                Times.Once
            );
        }



    }
}
