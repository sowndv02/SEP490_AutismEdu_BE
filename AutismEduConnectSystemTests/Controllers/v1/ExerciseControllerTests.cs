using System;
using System.Collections.Generic;
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
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutoMapper;
using Elasticsearch.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutismEduConnectSystem.Controllers.v1.Tests
{
    public class ExerciseControllerTests
    {
        private readonly Mock<IExerciseRepository> _exerciseRepositoryMock;
        private readonly IMapper _mapperMock;
        private readonly Mock<IResourceService> _resourceServiceMock;
        private readonly Mock<ILogger<ExerciseController>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private ExerciseController _controller;

        public ExerciseControllerTests()
        {
            // Mock IExerciseRepository
            _exerciseRepositoryMock = new Mock<IExerciseRepository>();

            // Mock IMapper
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingConfig());
            });
            _mapperMock = config.CreateMapper();

            // Mock IResourceService
            _resourceServiceMock = new Mock<IResourceService>();

            // Mock ILogger
            _loggerMock = new Mock<ILogger<ExerciseController>>();

            // Mock IConfiguration
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(config => config["APIConfig:PageSize"]).Returns("10"); // Simulating pageSize value.
            // Initialize Controller with Mocked Dependencies
            _controller = new ExerciseController(
                _exerciseRepositoryMock.Object,
                _configurationMock.Object,
                _mapperMock,
                _resourceServiceMock.Object,
                _loggerMock.Object
            );
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_ReturnsCreatedResponse_WhenOriginalIdIsZero()
        {
            // Arrange
            var exerciseCreateDTO = new ExerciseCreateDTO
            {
                ExerciseName = "New Exercise",
                Description = "Description of the exercise",
                OriginalId = 0,
            };

            var exercise = new Exercise
            {
                Id = 1,
                ExerciseName = "New Exercise",
                Description = "Description of the exercise",
                OriginalId = null,
                VersionNumber = 1,
                TutorId = "testUserId",
            };

            _exerciseRepositoryMock
                .Setup(repo => repo.GetNextVersionNumberAsync(It.IsAny<int?>()))
                .ReturnsAsync(1);

            _exerciseRepositoryMock
                .Setup(repo => repo.DeactivatePreviousVersionsAsync(It.IsAny<int?>()))
                .Returns(Task.CompletedTask);

            _exerciseRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Exercise>()))
                .ReturnsAsync(exercise);

            // Act
            var result = await _controller.CreateAsync(exerciseCreateDTO);
            var okResult = result.Result as ObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            _exerciseRepositoryMock.Verify(
                repo => repo.CreateAsync(It.IsAny<Exercise>()),
                Times.Once
            );
            _exerciseRepositoryMock.Verify(
                repo => repo.DeactivatePreviousVersionsAsync(It.IsAny<int?>()),
                Times.Once
            );
            _exerciseRepositoryMock.Verify(
                repo => repo.GetNextVersionNumberAsync(It.IsAny<int?>()),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateAsync_ExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange

            var exerciseCreateDTO = new ExerciseCreateDTO
            {
                ExerciseName = "New Exercise",
                Description = "Description of the exercise",
                OriginalId = 0,
            };
            _exerciseRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Exercise>()))
                .ThrowsAsync(new Exception("Lỗi hệ thống. Vui lòng thử lại sau!"));
            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Lỗi hệ thống. Vui lòng thử lại sau!");

            var result = await _controller.CreateAsync(exerciseCreateDTO);
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

            var requestPayload = new ExerciseCreateDTO
            {
                ExerciseName = "New Exercise",
                Description = "Description of the exercise",
                OriginalId = 0,
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

            var requestPayload = new ExerciseCreateDTO
            {
                ExerciseName = "New Exercise",
                Description = "Description of the exercise",
                OriginalId = 0,
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
        public async Task CreateExerciseAsync_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "The Name field is required.");

            // Act
            var result = await _controller.CreateAsync(new ExerciseCreateDTO());
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
        public async Task CreateAsync_ValidRequest_ReturnsCreatedResponse_WhenOriginalIdDifZero()
        {
            // Arrange
            var exerciseCreateDTO = new ExerciseCreateDTO
            {
                ExerciseName = "New Exercise",
                Description = "Description of the exercise",
                OriginalId = 1,
            };

            var exercise = new Exercise
            {
                Id = 1,
                ExerciseName = "New Exercise",
                Description = "Description of the exercise",
                OriginalId = 1,
                VersionNumber = 1,
                TutorId = "testUserId",
            };

            _exerciseRepositoryMock
                .Setup(repo => repo.GetNextVersionNumberAsync(It.IsAny<int?>()))
                .ReturnsAsync(1);

            _exerciseRepositoryMock
                .Setup(repo => repo.DeactivatePreviousVersionsAsync(It.IsAny<int?>()))
                .Returns(Task.CompletedTask);

            _exerciseRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Exercise>()))
                .ReturnsAsync(exercise);

            // Act
            var result = await _controller.CreateAsync(exerciseCreateDTO);
            var okResult = result.Result as ObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            _exerciseRepositoryMock.Verify(
                repo => repo.CreateAsync(It.IsAny<Exercise>()),
                Times.Once
            );
            _exerciseRepositoryMock.Verify(
                repo => repo.DeactivatePreviousVersionsAsync(It.IsAny<int?>()),
                Times.Once
            );
            _exerciseRepositoryMock.Verify(
                repo => repo.GetNextVersionNumberAsync(It.IsAny<int?>()),
                Times.Once
            );
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
            _resourceServiceMock
                .Setup(service => service.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Id không hợp lệ");
            // Act
            var result = await _controller.DeleteAsync(0);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value as APIResponse;
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.ErrorMessages.First().Should().Be("Id không hợp lệ");
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnBadRequest_WhenItemNotFound()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
            _resourceServiceMock
                .Setup(service => service.GetString(SD.NOT_FOUND_MESSAGE, SD.EXERCISE))
                .Returns("Không tìm thấy bài tập");
            _exerciseRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<Exercise, bool>>>(), true, null, null)
                )
                .ReturnsAsync((Exercise)null);

            // Act
            var result = await _controller.DeleteAsync(1);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

            var response = badRequestResult.Value as APIResponse;
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ErrorMessages.First().Should().Be("Không tìm thấy bài tập");
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnOk_WhenItemIsDeletedSuccessfully()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            var exercise = new Exercise { Id = 1 };

            _exerciseRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<Exercise, bool>>>(), true, null, null)
                )
                .ReturnsAsync(exercise);

            _exerciseRepositoryMock
                .Setup(repo => repo.RemoveAsync(It.IsAny<Exercise>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteAsync(1);

            // Assert
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnInternalServerError_WhenExceptionOccurs()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
            _resourceServiceMock
                .Setup(service => service.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Lỗi hệ thống. Vui lòng thử lại sau!");
            _exerciseRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<Exercise, bool>>>(), true, null, null)
                )
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeleteAsync(1);

            // Assert
            var errorResult = result.Result as ObjectResult;
            errorResult.Should().NotBeNull();
            errorResult!.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var response = errorResult.Value as APIResponse;
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            response.ErrorMessages.First().Should().Be("Lỗi hệ thống. Vui lòng thử lại sau!");
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
        {
            // Arrange
            _resourceServiceMock
                .Setup(service => service.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Người dùng cần đăng nhập!");
            _controller.ControllerContext.HttpContext = new DefaultHttpContext();

            // Act
            var result = await _controller.DeleteAsync(1);

            // Assert
            var unauthorizedResult = result.Result as ObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult!.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            var response = unauthorizedResult.Value as APIResponse;
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            response.ErrorMessages.First().Should().Be("Người dùng cần đăng nhập!");
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnForbidden_WhenUserIsNotInRequiredRole()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(
                    ClaimTypes.Role,
                    "OTHER_ROLE"
                ) // Not STAFF_ROLE or MANAGER_ROLE
                ,
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
            _resourceServiceMock
                .Setup(service => service.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Không có quyền truy cập vào tài nguyên!");
            // Act
            var result = await _controller.DeleteAsync(1);

            // Assert
            var forbiddenResult = result.Result as ObjectResult;
            forbiddenResult.Should().NotBeNull();
            forbiddenResult!.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

            var response = forbiddenResult.Value as APIResponse;
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            response.ErrorMessages.First().Should().Be("Không có quyền truy cập vào tài nguyên!");
        }

        [Fact]
        public async Task GetAllAsync_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            // Arrange
            _exerciseRepositoryMock
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Exercise, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<Exercise, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ThrowsAsync(new Exception("Lỗi hệ thống. Vui lòng thử lại sau!"));

            // Mock the resource service to return a specific error message
            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Lỗi hệ thống. Vui lòng thử lại sau!");

            // Act
            var result = await _controller.GetExercisesByTypeAsync(
                3,
                null,
                SD.CREATED_DATE,
                SD.ORDER_DESC
            );
            var statusCodeResult = result.Result as ObjectResult;

            // Assert
            statusCodeResult.Should().NotBeNull();
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = statusCodeResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse
                .ErrorMessages.FirstOrDefault()
                .Should()
                .Be("Lỗi hệ thống. Vui lòng thử lại sau!");
        }

        [Fact]
        public async Task GetAllAsync_ReturnsUnauthorized_WhenUserIsUnauthorized()
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
            var result = await _controller.GetExercisesByTypeAsync(
                3,
                null,
                SD.CREATED_DATE,
                SD.ORDER_DESC
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
        public async Task GetAllAsync_ReturnsForbiden_WhenUserDoesNotHaveRequiredRole()
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
            var result = await _controller.GetExercisesByTypeAsync(
                3,
                null,
                SD.CREATED_DATE,
                SD.ORDER_DESC
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
        public async Task GetAllAsync_ShouldReturnBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
            _resourceServiceMock
                .Setup(service => service.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Id không hợp lệ");
            // Act
            var result = await _controller.GetExercisesByTypeAsync(
                0,
                "Exercise",
                SD.CREATED_DATE,
                SD.ORDER_DESC
            );

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value as APIResponse;
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.ErrorMessages.First().Should().Be("Id không hợp lệ");
        }

        [Fact]
        public async Task GetExercisesByTypeAsync_ShouldReturnExercises_WhenUserIsTutorWithSearchAndOrderByCreatedDateAsc()
        {
            // Arrange
            var userId = "testTutorId";
            var exerciseTypeId = 1;
            var searchTerm = "Math";
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Assign TUTOR_ROLE
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var exercises = new List<Exercise>
            {
                new Exercise
                {
                    Id = 1,
                    ExerciseTypeId = exerciseTypeId,
                    ExerciseName = "Math Basics",
                    TutorId = userId,
                    CreatedDate = DateTime.Now.AddDays(-2),
                    IsActive = true,
                    IsDeleted = false,
                },
                new Exercise
                {
                    Id = 2,
                    ExerciseTypeId = exerciseTypeId,
                    ExerciseName = "Advanced Math",
                    TutorId = userId,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    IsActive = true,
                    IsDeleted = false,
                },
            };

            _exerciseRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Exercise, bool>>>(),
                        null,
                        null,
                        It.Is<Expression<Func<Exercise, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false
                    )
                )
                .ReturnsAsync((exercises.Count, exercises.OrderBy(e => e.CreatedDate).ToList()));

            // Act
            var result = await _controller.GetExercisesByTypeAsync(
                exerciseTypeId,
                searchTerm,
                SD.CREATED_DATE,
                SD.ORDER_ASC
            );

            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.IsSuccess.Should().BeTrue();

            var responseResult = response.Result as List<ExerciseDTO>;
            responseResult.Should().NotBeNull();
            responseResult.Count.Should().Be(2);

            // Verify repository call with expected parameters
            _exerciseRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.Is<Expression<Func<Exercise, bool>>>(filter =>
                            filter
                                .Compile()
                                .Invoke(
                                    new Exercise
                                    {
                                        ExerciseTypeId = exerciseTypeId,
                                        ExerciseName = "Math Basics",
                                        TutorId = userId,
                                        IsActive = true,
                                        IsDeleted = false,
                                    }
                                )
                        ),
                        null,
                        null,
                        It.Is<Expression<Func<Exercise, object>>>(orderBy =>
                            orderBy.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetExercisesByTypeAsync_ShouldReturnExercises_WhenUserIsTutorWithSearchAndOrderByCreatedDateDesc()
        {
            // Arrange
            var userId = "testTutorId";
            var exerciseTypeId = 1;
            var searchTerm = "Math";
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Assign TUTOR_ROLE
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var exercises = new List<Exercise>
            {
                new Exercise
                {
                    Id = 1,
                    ExerciseTypeId = exerciseTypeId,
                    ExerciseName = "Math Basics",
                    TutorId = userId,
                    CreatedDate = DateTime.Now.AddDays(-2),
                    IsActive = true,
                    IsDeleted = false,
                },
                new Exercise
                {
                    Id = 2,
                    ExerciseTypeId = exerciseTypeId,
                    ExerciseName = "Advanced Math",
                    TutorId = userId,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    IsActive = true,
                    IsDeleted = false,
                },
            };

            _exerciseRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Exercise, bool>>>(),
                        null,
                        null,
                        It.Is<Expression<Func<Exercise, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true
                    )
                )
                .ReturnsAsync((exercises.Count, exercises.OrderBy(e => e.CreatedDate).ToList()));

            // Act
            var result = await _controller.GetExercisesByTypeAsync(
                exerciseTypeId,
                searchTerm,
                SD.CREATED_DATE,
                SD.ORDER_DESC
            );

            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.IsSuccess.Should().BeTrue();

            var responseResult = response.Result as List<ExerciseDTO>;
            responseResult.Should().NotBeNull();
            responseResult.Count.Should().Be(2);

            // Verify repository call with expected parameters
            _exerciseRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.Is<Expression<Func<Exercise, bool>>>(filter =>
                            filter
                                .Compile()
                                .Invoke(
                                    new Exercise
                                    {
                                        ExerciseTypeId = exerciseTypeId,
                                        ExerciseName = "Math Basics",
                                        TutorId = userId,
                                        IsActive = true,
                                        IsDeleted = false,
                                    }
                                )
                        ),
                        null,
                        null,
                        It.Is<Expression<Func<Exercise, object>>>(orderBy =>
                            orderBy.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetExercisesByTypeAsync_ShouldReturnExercises_WhenUserIsTutorWithoutSearchAndOrderByCreatedDateAsc()
        {
            // Arrange
            var userId = "testTutorId";
            var exerciseTypeId = 1;
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Assign TUTOR_ROLE
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var exercises = new List<Exercise>
            {
                new Exercise
                {
                    Id = 1,
                    ExerciseTypeId = exerciseTypeId,
                    ExerciseName = "Math Basics",
                    TutorId = userId,
                    CreatedDate = DateTime.Now.AddDays(-2),
                    IsActive = true,
                    IsDeleted = false,
                },
                new Exercise
                {
                    Id = 2,
                    ExerciseTypeId = exerciseTypeId,
                    ExerciseName = "Advanced Math",
                    TutorId = userId,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    IsActive = true,
                    IsDeleted = false,
                },
            };

            _exerciseRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Exercise, bool>>>(),
                        null,
                        null,
                        It.Is<Expression<Func<Exercise, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false
                    )
                )
                .ReturnsAsync((exercises.Count, exercises.OrderBy(e => e.CreatedDate).ToList()));

            // Act
            var result = await _controller.GetExercisesByTypeAsync(
                exerciseTypeId,
                null, // No search term
                SD.CREATED_DATE,
                SD.ORDER_ASC
            );

            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.IsSuccess.Should().BeTrue();

            var responseResult = response.Result as List<ExerciseDTO>;
            responseResult.Should().NotBeNull();
            responseResult.Count.Should().Be(2);

            // Verify repository call with expected parameters
            _exerciseRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.Is<Expression<Func<Exercise, bool>>>(filter =>
                            filter
                                .Compile()
                                .Invoke(
                                    new Exercise
                                    {
                                        ExerciseTypeId = exerciseTypeId,
                                        TutorId = userId,
                                        IsActive = true,
                                        IsDeleted = false,
                                    }
                                )
                        ),
                        null,
                        null,
                        It.Is<Expression<Func<Exercise, object>>>(orderBy =>
                            orderBy.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetExercisesByTypeAsync_ShouldReturnExercises_WhenUserIsTutorWithoutSearchAndOrderByCreatedDateDesc()
        {
            // Arrange
            var userId = "testTutorId";
            var exerciseTypeId = 1;
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Assign TUTOR_ROLE
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var exercises = new List<Exercise>
            {
                new Exercise
                {
                    Id = 1,
                    ExerciseTypeId = exerciseTypeId,
                    ExerciseName = "Math Basics",
                    TutorId = userId,
                    CreatedDate = DateTime.Now.AddDays(-2),
                    IsActive = true,
                    IsDeleted = false,
                },
                new Exercise
                {
                    Id = 2,
                    ExerciseTypeId = exerciseTypeId,
                    ExerciseName = "Advanced Math",
                    TutorId = userId,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    IsActive = true,
                    IsDeleted = false,
                },
            };

            _exerciseRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Exercise, bool>>>(),
                        null,
                        null,
                        It.Is<Expression<Func<Exercise, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true
                    )
                )
                .ReturnsAsync((exercises.Count, exercises.OrderBy(e => e.CreatedDate).ToList()));

            // Act
            var result = await _controller.GetExercisesByTypeAsync(
                exerciseTypeId,
                null, // No search term
                SD.CREATED_DATE,
                SD.ORDER_DESC
            );

            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.IsSuccess.Should().BeTrue();

            var responseResult = response.Result as List<ExerciseDTO>;
            responseResult.Should().NotBeNull();
            responseResult.Count.Should().Be(2);

            // Verify repository call with expected parameters
            _exerciseRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.Is<Expression<Func<Exercise, bool>>>(filter =>
                            filter
                                .Compile()
                                .Invoke(
                                    new Exercise
                                    {
                                        ExerciseTypeId = exerciseTypeId,
                                        TutorId = userId,
                                        IsActive = true,
                                        IsDeleted = false,
                                    }
                                )
                        ),
                        null,
                        null,
                        It.Is<Expression<Func<Exercise, object>>>(orderBy =>
                            orderBy.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Ascending order
                    ),
                Times.Once
            );
        }
    }
}
