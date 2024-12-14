using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using AutismEduConnectSystem.Mapper;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.DTOs;
using AutismEduConnectSystem.DTOs.CreateDTOs;
using AutismEduConnectSystem.DTOs.UpdateDTOs;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace AutismEduConnectSystem.Controllers.v1.Tests
{
    public class AssessmentScoreRangeControllerTests
    {
        private readonly AssessmentScoreRangeController _controller;
        private readonly Mock<IAssessmentScoreRangeRepository> _assessmentScoreRangeRepositoryMock;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<AssessmentScoreRangeController>> _loggerMock;
        private readonly Mock<IResourceService> _resourceServiceMock;

        public AssessmentScoreRangeControllerTests()
        {
            _assessmentScoreRangeRepositoryMock = new Mock<IAssessmentScoreRangeRepository>();
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingConfig());
            });
            _mapper = config.CreateMapper();
            _loggerMock = new Mock<ILogger<AssessmentScoreRangeController>>();
            _resourceServiceMock = new Mock<IResourceService>();

            _controller = new AssessmentScoreRangeController(
                _assessmentScoreRangeRepositoryMock.Object,
                _mapper,
                _loggerMock.Object,
                _resourceServiceMock.Object
            );

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
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

            var requestPayload = new AssessmentScoreRangeCreateDTO
            {
                Description = "Sample Description",
                MinScore = 10,
                MaxScore = 20,
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

            var requestPayload = new AssessmentScoreRangeCreateDTO
            {
                Description = "Sample Description",
                MinScore = 10,
                MaxScore = 20,
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
        public async Task CreateAsync_ValidRequest_ReturnsCreatedResponse()
        {
            // Arrange
            var createDTO = new AssessmentScoreRangeCreateDTO
            {
                MinScore = 10,
                MaxScore = 20,
                Description = "Valid Range",
            };
            var model = new AssessmentScoreRange
            {
                Id = 1,
                MinScore = 10,
                MaxScore = 20,
                Description = "Valid Range",
                CreateBy = "testUserId",
                CreateDate = DateTime.Now,
            };
            _assessmentScoreRangeRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<AssessmentScoreRange>()))
                .ReturnsAsync(model);

            // Act
            var result = await _controller.CreateAsync(createDTO);
            var okResult = result.Result as ObjectResult;
            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            _assessmentScoreRangeRepositoryMock.Verify(
                repo => repo.CreateAsync(It.IsAny<AssessmentScoreRange>()),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateAsync_NullDTO_ReturnsBadRequest()
        {
            // Act
            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ASSESSMENT_SCORE_RANGE))
                .Returns("Khoảng điểm đánh giá không hợp lệ.");
            var result = await _controller.CreateAsync(null);
            var badRequestResult = result.Result as ObjectResult;
            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            apiResponse.ErrorMessages.First().Should().Be("Khoảng điểm đánh giá không hợp lệ.");
        }

        [Fact]
        public async Task CreateAsync_MinScoreGreaterThanMaxScore_ReturnsBadRequest()
        {
            // Arrange
            _resourceServiceMock
                .Setup(r => r.GetString(SD.CANNOT_GREATER, SD.MIN_SCORE, SD.MAX_SCORE))
                .Returns("Khoảng điểm không hợp lệ.");
            var createDTO = new AssessmentScoreRangeCreateDTO
            {
                MinScore = 30,
                MaxScore = 20,
                Description = "Invalid Range",
            };

            // Act
            var result = await _controller.CreateAsync(createDTO);
            var badRequestResult = result.Result as ObjectResult;
            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            apiResponse.ErrorMessages.First().Should().Be("Khoảng điểm không hợp lệ.");
        }

        [Fact]
        public async Task CreateAsync_ExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange

            var createDTO = new AssessmentScoreRangeCreateDTO
            {
                MinScore = 10,
                MaxScore = 20,
                Description = "Valid Range",
            };
            _assessmentScoreRangeRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<AssessmentScoreRange>()))
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
        public async Task GetAllAsync_ReturnsOkResultWithAssessmentScoreRangeDTOList()
        {
            // Arrange
            var scoreRanges = new List<AssessmentScoreRange>
            {
                new AssessmentScoreRange
                {
                    Id = 1,
                    Description = "Range 1",
                    MinScore = 10,
                    MaxScore = 20,
                },
                new AssessmentScoreRange
                {
                    Id = 2,
                    Description = "Range 2",
                    MinScore = 21,
                    MaxScore = 30,
                },
            };
            var scoreRangeDTOs = new List<AssessmentScoreRangeDTO>
            {
                new AssessmentScoreRangeDTO
                {
                    Id = 1,
                    Description = "Range 1",
                    MinScore = 10,
                    MaxScore = 20,
                },
                new AssessmentScoreRangeDTO
                {
                    Id = 2,
                    Description = "Range 2",
                    MinScore = 21,
                    MaxScore = 30,
                },
            };

            var resultMock = (totalCount: scoreRanges.Count, list: scoreRanges);

            _assessmentScoreRangeRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(null, null, null, null, true))
                .ReturnsAsync(resultMock);

            // Act
            var result = await _controller.GetAllAsync();
            var okResult = result.Result as ObjectResult;
            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            // Assert
            _assessmentScoreRangeRepositoryMock.Verify(
                repo => repo.GetAllNotPagingAsync(null, null, null, null, true),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            var exceptionMessage = "Database error";
            _assessmentScoreRangeRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(null, null, null, null, true))
                .ThrowsAsync(new Exception(exceptionMessage));

            _resourceServiceMock
                .Setup(service => service.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Lỗi hệ thống. Vui lòng thử lại sau!");

            // Act

            var result = await _controller.GetAllAsync();
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
            // Assert
            _assessmentScoreRangeRepositoryMock.Verify(
                repo => repo.GetAllNotPagingAsync(null, null, null, null, true),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnOk_WhenUpdateIsSuccessful()
        {
            // Arrange
            var updateDTO = new AssessmentScoreRangeUpdateDTO
            {
                Id = 1,
                MinScore = 10,
                MaxScore = 20,
                Description = "Updated Description",
            };

            var existingModel = new AssessmentScoreRange
            {
                Id = 1,
                MinScore = 5,
                MaxScore = 15,
                Description = "Old Description",
            };

            var updatedModel = new AssessmentScoreRange
            {
                Id = 1,
                MinScore = 10,
                MaxScore = 20,
                Description = "Updated Description",
            };

            var updatedDTO = new AssessmentScoreRangeDTO
            {
                Id = 1,
                MinScore = 10,
                MaxScore = 20,
                Description = "Updated Description",
            };

            _assessmentScoreRangeRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<AssessmentScoreRange, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync(existingModel);

            _assessmentScoreRangeRepositoryMock
                .Setup(repo => repo.UpdateAsync(It.IsAny<AssessmentScoreRange>()))
                .ReturnsAsync(updatedModel);

            // Act
            var result = await _controller.UpdateAsync(updateDTO);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeTrue();
            response
                .Result.Should()
                .BeEquivalentTo(updatedDTO, options => options.Excluding(r => r.CreateDate));

            _assessmentScoreRangeRepositoryMock.Verify(
                repo => repo.UpdateAsync(It.IsAny<AssessmentScoreRange>()),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
        {
            // Arrange
            _controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var updateDTO = new AssessmentScoreRangeUpdateDTO
            {
                Id = 1,
                MinScore = 10,
                MaxScore = 20,
            };

            // Act
            var result = await _controller.UpdateAsync(updateDTO);

            // Assert
            var unauthorizedResult = result.Result as ObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult!.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            var response = unauthorizedResult.Value as APIResponse;
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnBadRequest_WhenUpdateDTOIsNull()
        {
            // Arrange

            // Act
            var result = await _controller.UpdateAsync(null);

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value as APIResponse;
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnBadRequest_WhenAssessmentScoreRangeNotFound()
        {
            // Arrange

            var updateDTO = new AssessmentScoreRangeUpdateDTO
            {
                Id = 1,
                MinScore = 10,
                MaxScore = 20,
            };

            _assessmentScoreRangeRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<AssessmentScoreRange, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync((AssessmentScoreRange)null);

            // Act
            var result = await _controller.UpdateAsync(updateDTO);

            // Assert
            var badRequestResult = result.Result as NotFoundObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

            var response = badRequestResult.Value as APIResponse;
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            _assessmentScoreRangeRepositoryMock.Verify(
                repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<AssessmentScoreRange, bool>>>(),
                        true,
                        null,
                        null
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnForbidden_WhenUserIsNotInRequiredRole()
        {
            // Arrange
            // Simulate a user with a role that is not STAFF_ROLE or MANAGER_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // A role that is neither STAFF_ROLE nor MANAGER_ROLE
                ,
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
            _resourceServiceMock
                .Setup(service => service.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Không có quyền truy cập vào tài nguyên!");
            var updateDTO = new AssessmentScoreRangeUpdateDTO
            {
                Id = 1,
                MinScore = 10,
                MaxScore = 20,
                Description = "Updated Description",
            };

            // Act
            var result = await _controller.UpdateAsync(updateDTO);

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
        public async Task DeleteAsync_ShouldReturnBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
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
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
            _resourceServiceMock
                .Setup(service =>
                    service.GetString(SD.NOT_FOUND_MESSAGE, SD.ASSESSMENT_SCORE_RANGE)
                )
                .Returns("Không tìm thấy Khoảng điểm đánh giá");
            _assessmentScoreRangeRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<AssessmentScoreRange, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync((AssessmentScoreRange)null);

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
            response.ErrorMessages.First().Should().Be("Không tìm thấy Khoảng điểm đánh giá");
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnOk_WhenItemIsDeletedSuccessfully()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            var assessmentScoreRange = new AssessmentScoreRange { Id = 1 };

            _assessmentScoreRangeRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<AssessmentScoreRange, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync(assessmentScoreRange);

            _assessmentScoreRangeRepositoryMock
                .Setup(repo => repo.RemoveAsync(It.IsAny<AssessmentScoreRange>()))
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
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
            _resourceServiceMock
                .Setup(service => service.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Lỗi hệ thống. Vui lòng thử lại sau!");
            _assessmentScoreRangeRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<AssessmentScoreRange, bool>>>(),
                        true,
                        null,
                        null
                    )
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
    }
}
