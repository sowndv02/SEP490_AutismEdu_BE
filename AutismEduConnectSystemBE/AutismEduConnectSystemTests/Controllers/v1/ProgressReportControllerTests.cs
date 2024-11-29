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
using AutismEduConnectSystem.Models.DTOs.UpdateDTOs;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.SignalR;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutismEduConnectSystem.Controllers.v1.Tests
{
    public class ProgressReportControllerTests
    {
        private readonly IMapper _mockMapper;
        private readonly Mock<IProgressReportRepository> _mockProgressReportRepository;
        private readonly Mock<IAssessmentResultRepository> _mockAssessmentResultRepository;
        private readonly Mock<IResourceService> _mockResourceService;
        private readonly Mock<ILogger<ProgressReportController>> _mockLogger;
        private readonly ProgressReportController _controller;

        public ProgressReportControllerTests()
        {
            _mockProgressReportRepository = new Mock<IProgressReportRepository>();
            _mockAssessmentResultRepository = new Mock<IAssessmentResultRepository>();
            _mockResourceService = new Mock<IResourceService>();
            _mockLogger = new Mock<ILogger<ProgressReportController>>();
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingConfig());
            });
            _mockMapper = config.CreateMapper();
            _controller = new ProgressReportController(
                _mockMapper,
                Mock.Of<IConfiguration>(),
                _mockProgressReportRepository.Object,
                _mockAssessmentResultRepository.Object,
                Mock.Of<IInitialAssessmentResultRepository>(),
                _mockResourceService.Object,
                _mockLogger.Object,
                Mock.Of<INotificationRepository>(),
                Mock.Of<IHubContext<NotificationHub>>(),
                Mock.Of<IStudentProfileRepository>(),
                Mock.Of<IChildInformationRepository>(),
                Mock.Of<IUserRepository>()
            );
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnCreated_WhenValidInput()
        {
            // Arrange
            var tutorId = "testTutorId";
            var progressReportCreateDTO = new ProgressReportCreateDTO { StudentProfileId = 1, From = DateTime.UtcNow.AddDays(-3), To = DateTime.UtcNow };
            var progressReport = new ProgressReport
            {
                TutorId = tutorId,
                StudentProfileId = 1,
                From = DateTime.UtcNow.AddDays(-3),
                To = DateTime.UtcNow,
                CreatedDate = DateTime.Now,
            };
            var progressReportReturn = new ProgressReport
            {
                Id = 1,
                TutorId = tutorId,
                From = DateTime.UtcNow.AddDays(-3),
                To = DateTime.UtcNow,
                StudentProfileId = 1,
                CreatedDate = DateTime.Now,
            };
            var progressReportDTO = new ProgressReportDTO
            {
                Id = 1,
                CreatedDate = progressReportReturn.CreatedDate,
                From = progressReportReturn.From,
                To = progressReportReturn.To,
                AssessmentResults = new List<AssessmentResultDTO>()
            };

            var userClaims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, tutorId),
        new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
    };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            _mockProgressReportRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<ProgressReport>()))
                .ReturnsAsync(progressReportReturn); // Return a valid progress report

            // Act
            var result = await _controller.CreateAsync(progressReportCreateDTO);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<OkObjectResult>();

            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();

            var response = okResult!.Value as APIResponse;
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Result.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(progressReportDTO);

            _mockProgressReportRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<ProgressReport>()),
                Times.Once
            );
        }


        [Fact]
        public async Task CreateAsync_ShouldReturnInternalServerError_WhenExceptionThrown()
        {
            // Arrange
            var tutorId = "testTutorId";
            var progressReportCreateDTO = new ProgressReportCreateDTO { StudentProfileId = 1 };

            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, tutorId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            _mockResourceService
                .Setup(x => x.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("An error occurred while processing your request.");
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };


            // Act
            var result = await _controller.CreateAsync(progressReportCreateDTO);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<ObjectResult>();

            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult!.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            response.ErrorMessages.Should().NotBeNullOrEmpty();
            response
                .ErrorMessages.Should()
                .Contain("An error occurred while processing your request.");
        }

        [Fact]
        public async Task CreateAsync_ReturnsUnauthorized_WhenUserIsUnauthorized()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");

            // Simulate a user with no valid claims (unauthorized)
            var claimsIdentity = new ClaimsIdentity(); // No claims added
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            var requestPayload = new ProgressReportCreateDTO { StudentProfileId = 1 };

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
            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbiden access.");

            // Simulate a user with no valid claims (unauthorized)
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "testUserId") };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            var requestPayload = new ProgressReportCreateDTO { StudentProfileId = 1 };

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
        public async Task CreateAsync_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var requestPayload = new ProgressReportCreateDTO { StudentProfileId = 1 };
            var userId = "testUserId";
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            _controller.ModelState.AddModelError("Name", "The Name field is required.");

            _mockResourceService
                .Setup(x => x.GetString(SD.BAD_REQUEST_MESSAGE, SD.EXERCISE_TYPE))
                .Returns("Invalid model state for ExerciseTypeCreateDTO.");

            // Act
            var result = await _controller.CreateAsync(requestPayload);
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
        public async Task GetByIdAsync_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            var progressReportId = 1;
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() // No user attached to the HttpContext
            };
            _mockResourceService
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");
            // Act
            var result = await _controller.GetByIdÁync(progressReportId);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<ObjectResult>();

            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult!.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            response.ErrorMessages.Should().Contain("Unauthorized access.");

            _mockProgressReportRepository.Verify(
                repo => repo.GetAsync(It.IsAny<Expression<Func<ProgressReport, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never // Repository should not be called
            );
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnForbidden_WhenUserNotAuthorized()
        {
            // Arrange
            var progressReportId = 1;
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, "SomeOtherRole") // User does not have TUTOR_ROLE or PARENT_ROLE
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            _mockResourceService
                 .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                 .Returns("Forbiden access.");
            // Act
            var result = await _controller.GetByIdÁync(progressReportId);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<ObjectResult>();

            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult!.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            response.ErrorMessages.Should().Contain("Forbiden access.");

            _mockProgressReportRepository.Verify(
                repo => repo.GetAsync(It.IsAny<Expression<Func<ProgressReport, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never // Repository should not be called
            );
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnBadRequest_WhenIdIsZero()
        {
            // Arrange
            var invalidId = 0;
            var userId = "testUserId";
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE) // Valid role
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Invalid request data.");
            // Act
            var result = await _controller.GetByIdÁync(invalidId);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();

            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();

            var response = badRequestResult!.Value as APIResponse;
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.ErrorMessages.Should().Contain("Invalid request data.");

            _mockProgressReportRepository.Verify(
                repo => repo.GetAsync(It.IsAny<Expression<Func<ProgressReport, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never // Repository should not be called when Id is invalid
            );
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNotFound_WhenProgressReportDoesNotExist()
        {
            // Arrange
            var validId = 999; // An Id that doesn't exist
            var userId = "testUserId";
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE) // Valid role
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            _mockResourceService
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.PROGRESS_REPORT))
                .Returns("Progress report not found.");
            _mockProgressReportRepository
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ProgressReport, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((ProgressReport)null); // Simulate not found

            // Act
            var result = await _controller.GetByIdÁync(validId);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<NotFoundObjectResult>();

            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();

            var response = notFoundResult!.Value as APIResponse;
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ErrorMessages.Should().Contain("Progress report not found.");

            _mockProgressReportRepository.Verify(
                repo => repo.GetAsync(It.IsAny<Expression<Func<ProgressReport, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Once // Repository is called once to check for existence
            );
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnInternalServerError_WhenExceptionOccurs()
        {
            // Arrange
            var validId = 1; // Any valid Id
            var userId = "testUserId";
            var userClaims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userId),
        new Claim(ClaimTypes.Role, SD.TUTOR_ROLE) // Valid role
    };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            _mockProgressReportRepository
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ProgressReport, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Test exception")); // Simulate exception
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");
            // Act
            var result = await _controller.GetByIdÁync(validId);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<ObjectResult>();

            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult!.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            response.ErrorMessages.Should().Contain("Internal server error");

            _mockProgressReportRepository.Verify(
                repo => repo.GetAsync(It.IsAny<Expression<Func<ProgressReport, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Once // Repository is called once
            );
        }
        [Fact]
        public async Task GetByIdAsync_ReturnsOk_WhenProgressReportExistsAndUserIsTutorRole()
        {
            // Arrange
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[]
                    {
                new Claim(ClaimTypes.NameIdentifier, "12345"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            var progressReport = new ProgressReport
            {
                Id = 1,
                TutorId = "12345",
                CreatedDate = DateTime.Now,
                AssessmentResults = new List<AssessmentResult>
        {
            new AssessmentResult { Id = 101 },
            new AssessmentResult { Id = 102 }
        }
            };

            var progressReportDTO = new ProgressReportDTO
            {
                Id = 1,
                CreatedDate = progressReport.CreatedDate,
                AssessmentResults = new List<AssessmentResultDTO>
        {
            new AssessmentResultDTO { Id = 101 },
            new AssessmentResultDTO { Id = 102 }
        }
            };

            _mockProgressReportRepository
                .Setup(repo => repo.GetAsync(
                    It.IsAny<Expression<Func<ProgressReport, bool>>>(),
                    It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(progressReport);

            // Act
            var result = await _controller.GetByIdÁync(1);
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.Result.Should().BeOfType<ProgressReportDTO>();
            ((ProgressReportDTO)apiResponse.Result).Id.Should().Be(1);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsUnauthorized_WhenUserIsNotAuthenticated()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() // No user assigned
            };

            var updateDTO = new ProgressReportUpdateDTO { Id = 1 };
            _mockResourceService
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");
            // Act
            var result = await _controller.UpdateAsync(updateDTO);
            var unauthorizedResult = result.Result as ObjectResult;

            // Assert
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            var apiResponse = unauthorizedResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Unauthorized access.");
        }

        [Fact]
        public async Task UpdateAsync_ReturnsInternalServerError_WhenExceptionOccurs()
        {
            // Arrange
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[]
                    {
                new Claim(ClaimTypes.NameIdentifier, "12345"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE)
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var updateDTO = new ProgressReportUpdateDTO { Id = 1 };
            _mockResourceService
                .Setup(x => x.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("An error occurred while processing your request.");
            // Simulate exception during repository call
            _mockProgressReportRepository
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ProgressReport, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.UpdateAsync(updateDTO);
            var internalServerErrorResult = result.Result as ObjectResult;

            // Assert
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = internalServerErrorResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("An error occurred while processing your request.");
        }

        [Fact]
        public async Task UpdateAsync_ReturnsForbidden_WhenUserDoesNotHaveTutorRole()
        {
            // Arrange
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[]
                    {
                new Claim(ClaimTypes.NameIdentifier, "12345")
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var progressReport = new ProgressReport
            {
                Id = 1,
                TutorId = "12345",
                CreatedDate = DateTime.Now,
                AssessmentResults = new List<AssessmentResult>
        {
            new AssessmentResult { Id = 101 },
            new AssessmentResult { Id = 102 }
        }
            };

            var progressReportDTO = new ProgressReportDTO
            {
                Id = 1,
                CreatedDate = progressReport.CreatedDate,
                AssessmentResults = new List<AssessmentResultDTO>
        {
            new AssessmentResultDTO { Id = 101 },
            new AssessmentResultDTO { Id = 102 }
        }
            };
            _mockResourceService
               .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
               .Returns("Forbiden access.");
            _mockProgressReportRepository
               .Setup(repo => repo.GetAsync(
                   It.IsAny<Expression<Func<ProgressReport, bool>>>(),
                   It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
               .ReturnsAsync(progressReport);

            var updateDTO = new ProgressReportUpdateDTO { Id = 1 };

            // Act
            var result = await _controller.UpdateAsync(updateDTO);
            var forbiddenResult = result.Result as ObjectResult;

            // Assert
            forbiddenResult.Should().NotBeNull();
            forbiddenResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

            var apiResponse = forbiddenResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Forbiden access.");
        }

        [Fact]
        public async Task UpdateAsync_ReturnsNotFound_WhenProgressReportDoesNotExist()
        {
            // Arrange
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[]
                    {
                new Claim(ClaimTypes.NameIdentifier, "12345"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE)  // Valid role
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var updateDTO = new ProgressReportUpdateDTO { Id = 999 };  // Assuming 999 doesn't exist
            _mockResourceService
               .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.PROGRESS_REPORT))
               .Returns("Progress report not found.");
            _mockProgressReportRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ProgressReport, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((ProgressReport)null);  // Simulate that the report doesn't exist

            // Act
            var result = await _controller.UpdateAsync(updateDTO);
            var notFoundResult = result.Result as ObjectResult;

            // Assert
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

            var apiResponse = notFoundResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Progress report not found.");
        }

        [Fact]
        public async Task UpdateAsync_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[]
                    {
                new Claim(ClaimTypes.NameIdentifier, "12345"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE)  // Valid role
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Simulate an invalid Id (<= 0)
            var updateDTO = new ProgressReportUpdateDTO { Id = 0 };  // Invalid Id

            // Mock the resource service to return the proper error message
            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Invalid ProgressReport Id.");

            // Act
            var result = await _controller.UpdateAsync(updateDTO);
            var badRequestResult = result.Result as ObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Invalid ProgressReport Id.");
        }

        [Fact]
        public async Task UpdateAsync_ReturnsBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[]
                    {
                new Claim(ClaimTypes.NameIdentifier, "12345"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE)  // Valid role
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Simulate an invalid DTO (e.g., required fields missing or wrong format)
            var updateDTO = new ProgressReportUpdateDTO { Id = 1 };  // Valid ID, but ModelState will be invalid

            // Manually invalidate the model state
            _controller.ModelState.AddModelError("Achieved", "The Achieved field is required.");

            // Mock the resource service to return the proper error message
            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.PROGRESS_REPORT))
                .Returns("Invalid ProgressReport data.");

            // Act
            var result = await _controller.UpdateAsync(updateDTO);
            var badRequestResult = result.Result as ObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Invalid ProgressReport data.");
        }

        [Fact]
        public async Task UpdateAsync_ReturnsBadRequest_WhenProgressReportModificationPeriodHasExpired()
        {
            // Arrange
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[]
                    {
                new Claim(ClaimTypes.NameIdentifier, "12345"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE)  // Valid role
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var updateDTO = new ProgressReportUpdateDTO { Id = 1, Achieved = "Achieved", Failed = "Failed" }; // Valid DTO for the update

            // Create a ProgressReport model where the CreatedDate is more than 48 hours ago
            var progressReport = new ProgressReport
            {
                Id = 1,
                TutorId = "12345",
                CreatedDate = DateTime.Now.AddHours(-49), // More than 48 hours ago
                Achieved = "Achieved",
                Failed = "Failed",
                NoteFromTutor = "Note"
            };

            // Mock the repository to return the existing progress report
            _mockProgressReportRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ProgressReport, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(progressReport);

            // Mock the resource service to return the appropriate error message
            _mockResourceService
                .Setup(r => r.GetString(SD.PROGRESS_REPORT_MODIFICATION_EXPIRED))
                .Returns("Progress report modification period has expired.");

            // Act
            var result = await _controller.UpdateAsync(updateDTO);
            var badRequestResult = result.Result as ObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Progress report modification period has expired.");
        }

        [Fact]
        public async Task UpdateAsync_ReturnsOk_WhenProgressReportIsUpdatedSuccessfully()
        {
            // Arrange
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[]
                    {
                new Claim(ClaimTypes.NameIdentifier, "12345"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE)  // Valid role
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var updateDTO = new ProgressReportUpdateDTO
            {
                Id = 1,
                Achieved = "Achieved",
                Failed = "Failed",
                NoteFromTutor = "Updated note"
            }; // Valid DTO for the update

            // Create a ProgressReport model with a valid creation date (within 48 hours)
            var progressReport = new ProgressReport
            {
                Id = 1,
                TutorId = "12345",
                CreatedDate = DateTime.Now.AddHours(-24), // Within 48 hours
                Achieved = "Achieved",
                Failed = "Failed",
                NoteFromTutor = "Original note"
            };

            // Mock the repository to return the existing progress report
            _mockProgressReportRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ProgressReport, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(progressReport);

            // Mock the repository to simulate the successful update
            _mockProgressReportRepository
                .Setup(r => r.UpdateAsync(It.IsAny<ProgressReport>()))
                .ReturnsAsync(progressReport);

            // Act
            var result = await _controller.UpdateAsync(updateDTO);
            var okResult = result.Result as ObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.Result.Should().BeOfType<ProgressReportDTO>();
            var progressReportDTO = (ProgressReportDTO)apiResponse.Result;
            progressReportDTO.Id.Should().Be(1);
            progressReportDTO.Achieved.Should().Be("Achieved");
            progressReportDTO.Failed.Should().Be("Failed");
            progressReportDTO.NoteFromTutor.Should().Be("Updated note");
        }

        [Fact]
        public async Task GetAllAsync_ReturnsUnauthorized_WhenUserIsNotAuthenticated()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } // Simulate unauthenticated user
            };
            var studentProfileId = 1;
            _mockResourceService
                 .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                 .Returns("Unauthorized access.");
            // Act
            var result = await _controller.GetAllAsync(studentProfileId);

            // Assert
            var unauthorizedResult = result.Result as ObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            var apiResponse = unauthorizedResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Unauthorized access.");
        }


        [Fact]
        public async Task GetAllAsync_ReturnsForbidden_WhenUserHasNoRequiredRole()
        {
            // Arrange
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[]
                    {
                new Claim(ClaimTypes.NameIdentifier, "12345"),
                new Claim(ClaimTypes.Role, "OtherRole")  // Invalid role
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
            var studentProfileId = 1;
            _mockResourceService
                 .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                 .Returns("Forbiden access.");
            // Act
            var result = await _controller.GetAllAsync(studentProfileId);

            // Assert
            var forbiddenResult = result.Result as ObjectResult;
            forbiddenResult.Should().NotBeNull();
            forbiddenResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

            var apiResponse = forbiddenResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Forbiden access.");
        }

        [Fact]
        public async Task GetAllAsync_ReturnsInternalServerError_WhenExceptionOccurs()
        {
            // Arrange
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[]
                    {
                new Claim(ClaimTypes.NameIdentifier, "12345"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE)  // Valid role
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
            var studentProfileId = 1;

            // Simulate an exception in the method
            _mockProgressReportRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ProgressReport, bool>>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<ProgressReport, object>>>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Internal server error"));
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");
            // Act
            var result = await _controller.GetAllAsync(studentProfileId);

            // Assert
            var internalServerErrorResult = result.Result as ObjectResult;
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = internalServerErrorResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Internal server error");
        }

        [Fact]
        public async Task GetAllAsync_ReturnsBadRequest_WhenStudentProfileIdIsInvalid()
        {
            // Arrange
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[]
                    {
                new Claim(ClaimTypes.NameIdentifier, "12345"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE)  // Valid role
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Simulate an invalid studentProfileId (<= 0)
            var studentProfileId = 0;

            // Mock the resource service to return the proper error message
            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Invalid Student Profile Id.");

            // Act
            var result = await _controller.GetAllAsync(studentProfileId);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Invalid Student Profile Id.");
        }

        [Fact]
        public async Task GetAllAsync_ReturnsBadRequest_WhenStartDateIsGreaterThanEndDate()
        {
            // Arrange
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[]
                    {
                new Claim(ClaimTypes.NameIdentifier, "12345"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE)  // Valid role
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Simulate an invalid date range where startDate > endDate
            var studentProfileId = 1;
            var startDate = new DateTime(2024, 11, 10);
            var endDate = new DateTime(2024, 11, 9);  // endDate is before startDate

            // Mock the resource service to return the proper error message
            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.SEARCH_DATE))
                .Returns("Invalid date range provided.");

            // Act
            var result = await _controller.GetAllAsync(studentProfileId, startDate: startDate, endDate: endDate);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Invalid date range provided.");
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ProgressReportWithDates_OrderByCreatedDate_Sort_ReturnsOkResponse(string sort)
        {
            // Arrange
            var userId = "tutor-user-id";
            var getInitialResult = true;
            var startDate = new DateTime(2024, 11, 1);
            var endDate = new DateTime(2024, 11, 30);
            var orderBy = SD.CREATED_DATE;
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var progressReports = new List<ProgressReport>
            {
                new ProgressReport { Id = 1, CreatedDate = DateTime.UtcNow.AddDays(-2), From = DateTime.UtcNow.AddDays(-3), To = DateTime.UtcNow.AddDays(-2) },
                new ProgressReport { Id = 2, CreatedDate = DateTime.UtcNow.AddDays(-1), From = DateTime.UtcNow.AddDays(-2), To = DateTime.UtcNow.AddDays(-1) },
                new ProgressReport { Id = 3, CreatedDate = DateTime.UtcNow, From = DateTime.UtcNow.AddDays(-1), To = DateTime.UtcNow },
            };
            if(sort == SD.ORDER_ASC)
            {
                progressReports = progressReports
                .Where(p => p.From >= startDate && p.To <= endDate)
                .OrderBy(p => p.CreatedDate)
                .ToList();
            }
            else
            {
                progressReports = progressReports
                .Where(p => p.From >= startDate && p.To <= endDate)
                .OrderByDescending(p => p.CreatedDate)
                .ToList();
            }
            

            _mockProgressReportRepository
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<ProgressReport, bool>>>(),
                    It.IsAny<string>(),
                    pageSize,
                    pageNumber,
                    It.IsAny<Expression<Func<ProgressReport, object>>>(),
                    It.IsAny<bool>())
                )
                .ReturnsAsync((progressReports.Count, progressReports));

            // Act
            var result = await _controller.GetAllAsync(1, startDate, endDate, orderBy, sort, pageNumber, pageSize, getInitialResult);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<ProgressReportGraphDTO>(); // Update assertion to match expected type

            var graphData = apiResponse.Result as ProgressReportGraphDTO;
            graphData.Should().NotBeNull();
            graphData.ProgressReports.Should().HaveCount(progressReports.Count);

            // Verify sorting
            if (sort == SD.ORDER_ASC)
            {
                graphData.ProgressReports.Should().BeInAscendingOrder(p => p.CreatedDate);
            }
            else
            {
                graphData.ProgressReports.Should().BeInDescendingOrder(p => p.CreatedDate);
            }
        }


        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ProgressReportWithDates_OrderByToDate_Sort_ReturnsOkResponse(string sort)
        {
            // Arrange
            var userId = "tutor-user-id";
            var getInitialResult = true;
            var startDate = new DateTime(2024, 11, 1);
            var endDate = new DateTime(2024, 11, 30);
            var orderBy = SD.DATE_TO;
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userId),
        new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
    };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var progressReports = new List<ProgressReport>
    {
        new ProgressReport { Id = 1, CreatedDate = DateTime.UtcNow.AddDays(-3), From = DateTime.UtcNow.AddDays(-5), To = DateTime.UtcNow.AddDays(-2) },
        new ProgressReport { Id = 2, CreatedDate = DateTime.UtcNow.AddDays(-2), From = DateTime.UtcNow.AddDays(-4), To = DateTime.UtcNow.AddDays(-1) },
        new ProgressReport { Id = 3, CreatedDate = DateTime.UtcNow.AddDays(-1), From = DateTime.UtcNow.AddDays(-3), To = DateTime.UtcNow },
    };

            if (sort == SD.ORDER_ASC)
            {
                progressReports = progressReports
                    .Where(p => p.From >= startDate && p.To <= endDate)
                    .OrderBy(p => p.To)
                    .ToList();
            }
            else
            {
                progressReports = progressReports
                    .Where(p => p.From >= startDate && p.To <= endDate)
                    .OrderByDescending(p => p.To)
                    .ToList();
            }

            _mockProgressReportRepository
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<ProgressReport, bool>>>(),
                    It.IsAny<string>(),
                    pageSize,
                    pageNumber,
                    It.IsAny<Expression<Func<ProgressReport, object>>>(),
                    It.IsAny<bool>())
                )
                .ReturnsAsync((progressReports.Count, progressReports));

            // Act
            var result = await _controller.GetAllAsync(1, startDate, endDate, orderBy, sort, pageNumber, pageSize, getInitialResult);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<ProgressReportGraphDTO>(); // Update assertion to match expected type

            var graphData = apiResponse.Result as ProgressReportGraphDTO;
            graphData.Should().NotBeNull();
            graphData.ProgressReports.Should().HaveCount(progressReports.Count);

            // Verify sorting
            if (sort == SD.ORDER_ASC)
            {
                graphData.ProgressReports.Should().BeInAscendingOrder(p => p.To);
            }
            else
            {
                graphData.ProgressReports.Should().BeInDescendingOrder(p => p.To);
            }
        }


        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ProgressReportWithDates_OrderByFromDate_Sort_ReturnsOkResponse(string sort)
        {
            // Arrange
            var userId = "tutor-user-id";
            var getInitialResult = true;
            var startDate = new DateTime(2024, 11, 1);
            var endDate = new DateTime(2024, 11, 30);
            var orderBy = SD.DATE_FROM;
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userId),
        new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
    };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var progressReports = new List<ProgressReport>
    {
        new ProgressReport { Id = 1, CreatedDate = DateTime.UtcNow.AddDays(-3), From = DateTime.UtcNow.AddDays(-5), To = DateTime.UtcNow.AddDays(-4) },
        new ProgressReport { Id = 2, CreatedDate = DateTime.UtcNow.AddDays(-2), From = DateTime.UtcNow.AddDays(-4), To = DateTime.UtcNow.AddDays(-3) },
        new ProgressReport { Id = 3, CreatedDate = DateTime.UtcNow.AddDays(-1), From = DateTime.UtcNow.AddDays(-3), To = DateTime.UtcNow.AddDays(-2) },
    };

            if (sort == SD.ORDER_ASC)
            {
                progressReports = progressReports
                    .Where(p => p.From >= startDate && p.To <= endDate)
                    .OrderBy(p => p.From)
                    .ToList();
            }
            else
            {
                progressReports = progressReports
                    .Where(p => p.From >= startDate && p.To <= endDate)
                    .OrderByDescending(p => p.From)
                    .ToList();
            }

            _mockProgressReportRepository
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<ProgressReport, bool>>>(),
                    It.IsAny<string>(),
                    pageSize,
                    pageNumber,
                    It.IsAny<Expression<Func<ProgressReport, object>>>(),
                    It.IsAny<bool>())
                )
                .ReturnsAsync((progressReports.Count, progressReports));

            // Act
            var result = await _controller.GetAllAsync(1, startDate, endDate, orderBy, sort, pageNumber, pageSize, getInitialResult);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<ProgressReportGraphDTO>();

            var graphData = apiResponse.Result as ProgressReportGraphDTO;
            graphData.Should().NotBeNull();
            graphData.ProgressReports.Should().HaveCount(progressReports.Count);

            // Verify sorting
            if (sort == SD.ORDER_ASC)
            {
                graphData.ProgressReports.Should().BeInAscendingOrder(p => p.From);
            }
            else
            {
                graphData.ProgressReports.Should().BeInDescendingOrder(p => p.From);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ProgressReportWithDates_OrderByNull_Sort_ReturnsOkResponse(string sort)
        {
            // Arrange
            var userId = "tutor-user-id";
            var getInitialResult = true;
            var startDate = new DateTime(2024, 11, 1);
            var endDate = new DateTime(2024, 11, 30);
            string orderBy = null; // No specific order
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userId),
        new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
    };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var progressReports = new List<ProgressReport>
    {
        new ProgressReport { Id = 3, CreatedDate = DateTime.UtcNow, From = DateTime.UtcNow.AddDays(-3), To = DateTime.UtcNow.AddDays(-2) },
        new ProgressReport { Id = 2, CreatedDate = DateTime.UtcNow.AddDays(-1), From = DateTime.UtcNow.AddDays(-2), To = DateTime.UtcNow.AddDays(-1) },
        new ProgressReport { Id = 1, CreatedDate = DateTime.UtcNow.AddDays(-2), From = DateTime.UtcNow.AddDays(-4), To = DateTime.UtcNow.AddDays(-3) },
    };

            if (sort == SD.ORDER_ASC)
            {
                progressReports = progressReports
                    .Where(p => p.From >= startDate && p.To <= endDate)
                    .OrderBy(p => p.Id) // Default to ID sorting
                    .ToList();
            }
            else
            {
                progressReports = progressReports
                    .Where(p => p.From >= startDate && p.To <= endDate)
                    .OrderByDescending(p => p.Id)
                    .ToList();
            }

            _mockProgressReportRepository
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<ProgressReport, bool>>>(),
                    orderBy,
                    pageSize,
                    pageNumber,
                    It.IsAny<Expression<Func<ProgressReport, object>>>(),
                    It.IsAny<bool>())
                )
                .ReturnsAsync((progressReports.Count, progressReports));

            // Act
            var result = await _controller.GetAllAsync(1, startDate, endDate, orderBy, sort, pageNumber, pageSize, getInitialResult);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<ProgressReportGraphDTO>();

            var graphData = apiResponse.Result as ProgressReportGraphDTO;
            graphData.Should().NotBeNull();
            graphData.ProgressReports.Should().HaveCount(progressReports.Count);

            // Verify sorting
            if (sort == SD.ORDER_ASC)
            {
                graphData.ProgressReports.Should().BeInAscendingOrder(p => p.Id);
            }
            else
            {
                graphData.ProgressReports.Should().BeInDescendingOrder(p => p.Id);
            }
        }

    }
}
