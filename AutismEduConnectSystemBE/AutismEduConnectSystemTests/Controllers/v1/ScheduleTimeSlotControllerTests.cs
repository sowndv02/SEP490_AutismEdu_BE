using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using AutismEduConnectSystem.Controllers.v1;
using AutismEduConnectSystem.Mapper;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Models.DTOs.UpdateDTOs;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutismEduConnectSystem.Controllers.v1.Tests
{
    public class ScheduleTimeSlotControllerTests
    {
        private Mock<IScheduleRepository> _scheduleRepositoryMock;
        private Mock<IScheduleTimeSlotRepository> _scheduleTimeSlotRepositoryMock;
        private Mock<IStudentProfileRepository> _studentProfileRepositoryMock;
        private Mock<IResourceService> _resourceServiceMock;
        private IMapper _mapperMock;
        private Mock<ILogger<ScheduleTimeSlotController>> _loggerMock;
        private ScheduleTimeSlotController _controller;
        private readonly Mock<IConfiguration> _configurationMock;

        public ScheduleTimeSlotControllerTests()
        {
            // Initialize the mocks
            _scheduleRepositoryMock = new Mock<IScheduleRepository>();
            _scheduleTimeSlotRepositoryMock = new Mock<IScheduleTimeSlotRepository>();
            _studentProfileRepositoryMock = new Mock<IStudentProfileRepository>();
            _resourceServiceMock = new Mock<IResourceService>();
            _loggerMock = new Mock<ILogger<ScheduleTimeSlotController>>();
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(c => c["APIConfig:PageSize"]).Returns("10"); // Mock the PageSize value
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingConfig());
            });
            _mapperMock = config.CreateMapper();
            // Instantiate the controller with mocked dependencies
            _controller = new ScheduleTimeSlotController(
                _scheduleRepositoryMock.Object,
                _scheduleTimeSlotRepositoryMock.Object,
                _studentProfileRepositoryMock.Object,
                _resourceServiceMock.Object,
                _mapperMock,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task DeleteAsync_ReturnsUnauthorized_WhenUserIsUnauthorized()
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
            var result = await _controller.DeleteAsync(1);
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
        public async Task DeleteAsync_ReturnsForbiden_WhenUserDoesNotHaveRequiredRole()
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
            var result = await _controller.DeleteAsync(1);

            var forbiddenResult = result.Result as ObjectResult;
            // Assert
            forbiddenResult.Should().NotBeNull();
            forbiddenResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

            var apiResponse = forbiddenResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            apiResponse.ErrorMessages.First().Should().Be("Forbiden access.");
        }

        [Fact]
        public async Task DeleteAsync_InternalServerError_ShouldReturnInternalServerError()
        {
            // Arrange
            var tutorRequestCreateDTO = new TutorRequestCreateDTO
            {
                ChildId = 1,
                TutorId = "tutor-id",
            };

            // Simulate a valid user (Parent role)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");

            // Simulate an exception thrown during the process
            _scheduleTimeSlotRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ScheduleTimeSlot, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeleteAsync(1);

            // Assert
            var internalServerErrorResult = result.Result as ObjectResult;
            internalServerErrorResult
                .StatusCode.Should()
                .Be((int)HttpStatusCode.InternalServerError);

            var response = internalServerErrorResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            response.ErrorMessages.Should().Contain("Internal server error");
        }

        [Fact]
        public async Task DeleteAsync_InvalidTimeSlotId_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidTimeSlotId = 0; // Simulating invalid timeSlotId (<= 0)

            // Simulate a valid user (TUTOR role)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock the resource service for the BadRequest message
            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Invalid ID provided");

            // Act
            var result = await _controller.DeleteAsync(invalidTimeSlotId);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest); // Should return 400 Bad Request

            var response = badRequestResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.ErrorMessages.Should().Contain("Invalid ID provided");
        }

        [Fact]
        public async Task DeleteAsync_TimeSlotNotFound_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistentTimeSlotId = 1; // Simulating a timeSlotId that does not exist in the repository

            // Simulate a valid user (TUTOR role)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock the resource service for the NotFound message
            _resourceServiceMock
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.TIME_SLOT))
                .Returns("Time slot not found");

            // Mock the repository to return null (simulate that time slot is not found)
            _scheduleTimeSlotRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ScheduleTimeSlot, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((ScheduleTimeSlot)null); // Return null, simulating that no time slot is found

            // Act
            var result = await _controller.DeleteAsync(nonExistentTimeSlotId);

            // Assert
            var notFoundResult = result.Result as ObjectResult;
            notFoundResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound); // Should return 404 Not Found

            var response = notFoundResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ErrorMessages.Should().Contain("Time slot not found");
        }

        [Fact]
        public async Task DeleteAsync_ValidTimeSlotId_ShouldReturnOk()
        {
            // Arrange
            var validTimeSlotId = 1; // Simulating a valid timeSlotId
            var studentProfileId = 123; // Simulating a student profile ID
            var model = new ScheduleTimeSlot
            {
                Id = validTimeSlotId,
                StudentProfileId = studentProfileId,
                IsDeleted = false,
                UpdatedDate = DateTime.Now,
            };

            // Simulate a valid user (TUTOR role)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock the resource service for the success message
            _scheduleTimeSlotRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ScheduleTimeSlot, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(model);
            // Mock the repository methods
            _scheduleTimeSlotRepositoryMock
                .Setup(repo => repo.UpdateAsync(It.IsAny<ScheduleTimeSlot>()))
                .ReturnsAsync(model); // Simulate successful update

            var scheduleList = new List<Schedule>
            {
                new Schedule
                {
                    Id = 1,
                    StudentProfileId = studentProfileId,
                    ScheduleTimeSlotId = validTimeSlotId,
                    ScheduleDate = DateTime.Now.AddDays(1),
                },
            };

            _scheduleRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<Schedule, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((scheduleList.Count, scheduleList)); // Simulate valid schedules to remove

            _scheduleRepositoryMock
                .Setup(repo => repo.RemoveAsync(It.IsAny<Schedule>()))
                .Returns(Task.CompletedTask); // Simulate successful removal

            // Act
            var result = await _controller.DeleteAsync(validTimeSlotId);

            // Assert
            var okResult = result.Result as ObjectResult;
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK); // Should return 200 OK

            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.ErrorMessages.Should().BeEmpty(); // No error messages should be present

            // Ensure that the UpdateAsync method was called
            _scheduleTimeSlotRepositoryMock.Verify(
                repo => repo.UpdateAsync(It.IsAny<ScheduleTimeSlot>()),
                Times.Once
            );

            // Ensure that the RemoveAsync method was called for each schedule to remove
            _scheduleRepositoryMock.Verify(
                repo => repo.RemoveAsync(It.IsAny<Schedule>()),
                Times.Once
            );
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

            var createDTOs = new List<ScheduleTimeSlotCreateDTO>
            {
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 1, // Monday
                    From = "09:00", // From time
                    To =
                        "10:00" // To time
                    ,
                },
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 3, // Wednesday
                    From = "11:00", // From time
                    To =
                        "12:00" // To time
                    ,
                },
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 5, // Friday
                    From = "14:00", // From time
                    To =
                        "15:00" // To time
                    ,
                },
            };
            var studentProfileId = 1;

            // Act
            var result = await _controller.CreateAsync(createDTOs, studentProfileId);
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

            var createDTOs = new List<ScheduleTimeSlotCreateDTO>
            {
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 1, // Monday
                    From = "09:00", // From time
                    To =
                        "10:00" // To time
                    ,
                },
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 3, // Wednesday
                    From = "11:00", // From time
                    To =
                        "12:00" // To time
                    ,
                },
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 5, // Friday
                    From = "14:00", // From time
                    To =
                        "15:00" // To time
                    ,
                },
            };
            var studentProfileId = 1;

            // Act
            var result = await _controller.CreateAsync(createDTOs, studentProfileId);

            var forbiddenResult = result.Result as ObjectResult;
            // Assert
            forbiddenResult.Should().NotBeNull();
            forbiddenResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

            var apiResponse = forbiddenResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            apiResponse.ErrorMessages.First().Should().Be("Forbiden access.");
        }

        [Fact]
        public async Task CreateAsync_InternalServerError_ShouldReturnInternalServerError()
        {
            // Arrange
            var createDTOs = new List<ScheduleTimeSlotCreateDTO>
            {
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 1, // Monday
                    From = "09:00", // From time
                    To =
                        "10:00" // To time
                    ,
                },
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 3, // Wednesday
                    From = "11:00", // From time
                    To =
                        "12:00" // To time
                    ,
                },
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 5, // Friday
                    From = "14:00", // From time
                    To =
                        "15:00" // To time
                    ,
                },
            };
            var studentProfileId = 1;

            // Simulate a valid user (Parent role)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");

            // Simulate an exception thrown during the process
            _scheduleTimeSlotRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<ScheduleTimeSlot>()))
                .ThrowsAsync(new Exception("Database error"));
            _studentProfileRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ThrowsAsync(new Exception("Database error"));
            // Act
            var result = await _controller.CreateAsync(createDTOs, studentProfileId);

            // Assert
            var internalServerErrorResult = result.Result as ObjectResult;
            internalServerErrorResult
                .StatusCode.Should()
                .Be((int)HttpStatusCode.InternalServerError);

            var response = internalServerErrorResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            response.ErrorMessages.Should().Contain("Internal server error");
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnBadRequest_WhenStudentProfileIdIsInvalid()
        {
            // Arrange
            var createDTOs = new List<ScheduleTimeSlotCreateDTO>
            {
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 1, // Monday
                    From = "09:00", // From time
                    To =
                        "10:00" // To time
                    ,
                },
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 3, // Wednesday
                    From = "11:00", // From time
                    To =
                        "12:00" // To time
                    ,
                },
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 5, // Friday
                    From = "14:00", // From time
                    To =
                        "15:00" // To time
                    ,
                },
            };

            var invalidStudentProfileId = 0; // Invalid ID (<= 0)

            // Simulate a valid user (Parent role)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Simulate the resource service response for a bad request message
            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Invalid ID provided");

            // Act
            var result = await _controller.CreateAsync(createDTOs, invalidStudentProfileId);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.ErrorMessages.Should().Contain("Invalid ID provided");
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnBadRequest_WhenCreateDTOsIsNullOrEmpty()
        {
            // Arrange
            List<ScheduleTimeSlotCreateDTO> createDTOs = null; // Simulating null or empty DTOs
            var studentProfileId = 1;

            // Simulate a valid user (Parent role)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Simulate the resource service response for a bad request message
            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.TIME_SLOT))
                .Returns("Invalid time slot data");

            // Act
            var result = await _controller.CreateAsync(createDTOs, studentProfileId);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.ErrorMessages.Should().Contain("Invalid time slot data");
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnNotFound_WhenStudentProfileIsNull()
        {
            // Arrange
            var createDTOs = new List<ScheduleTimeSlotCreateDTO>
            {
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 1, // Monday
                    From = "09:00", // From time
                    To =
                        "10:00" // To time
                    ,
                },
            };
            var studentProfileId = 1;
            var tutorId = 1;

            // Simulate a valid user (Parent role)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Simulate the resource service response for a not found message
            _resourceServiceMock
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.STUDENT_PROFILE))
                .Returns("Student profile not found");

            // Simulate a null student profile response
            _studentProfileRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((StudentProfile)null);

            // Act
            var result = await _controller.CreateAsync(createDTOs, studentProfileId);

            // Assert
            var notFoundResult = result.Result as ObjectResult;
            notFoundResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

            var response = notFoundResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ErrorMessages.Should().Contain("Student profile not found");
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnBadRequest_WhenTimeSlotIsInvalid()
        {
            // Arrange
            var createDTOs = new List<ScheduleTimeSlotCreateDTO>
            {
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 1, // Monday
                    From = "10:00", // From time
                    To =
                        "09:00" // Invalid time range (From >= To)
                    ,
                },
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 3, // Wednesday
                    From = "11:00", // From time
                    To =
                        "12:00" // To time
                    ,
                },
            };
            var studentProfileId = 1;

            // Simulate a valid user (Parent role)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            var studentProfile = new StudentProfile() { Id = 1 };
            // Simulate the resource service response for a bad request message
            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.TIME_SLOT))
                .Returns("Invalid time slot data");
            _studentProfileRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(studentProfile);
            // Act
            var result = await _controller.CreateAsync(createDTOs, studentProfileId);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.ErrorMessages.Should().Contain("Invalid time slot data");
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnBadRequest_WhenTimeSlotIsDuplicate()
        {
            // Arrange
            var createDTOs = new List<ScheduleTimeSlotCreateDTO>
            {
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 1, // Monday
                    From = "09:00", // From time
                    To =
                        "10:00" // To time
                    ,
                },
            };
            var studentProfileId = 1;
            var tutorId = "tutorId";

            // Simulate a valid user (Tutor role)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            var studentProfile = new StudentProfile() { Id = 1 };
            _studentProfileRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(studentProfile);
            // Simulate the resource service response for a time slot duplicate message
            _resourceServiceMock
                .Setup(r =>
                    r.GetString(
                        SD.TIMESLOT_DUPLICATED_MESSAGE,
                        SD.TIME_SLOT,
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .Returns("Duplicate time slot found: From: 09:00, To: 10:00");

            // Simulate a duplicate time slot in the repository
            var duplicateSlot = new ScheduleTimeSlot
            {
                Weekday = 1, // Monday
                From = TimeSpan.Parse("09:00"), // From time
                To = TimeSpan.Parse("10:00"), // To time
                StudentProfile = new StudentProfile
                {
                    TutorId = tutorId,
                    Status = SD.StudentProfileStatus.Teaching,
                },
            };

            _scheduleTimeSlotRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ScheduleTimeSlot, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(duplicateSlot);

            // Act
            var result = await _controller.CreateAsync(createDTOs, studentProfileId);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response
                .ErrorMessages.Should()
                .Contain("Duplicate time slot found: From: 09:00, To: 10:00");
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnBadRequest_WhenDuplicateTimeSlotIsFoundInList()
        {
            // Arrange
            var createDTOs = new List<ScheduleTimeSlotCreateDTO>
            {
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 1, // Monday
                    From = "09:00", // From time
                    To =
                        "10:00" // To time
                    ,
                },
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 3, // Wednesday
                    From = "11:00", // From time
                    To =
                        "12:00" // To time
                    ,
                },
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 5, // Friday
                    From = "14:00", // From time
                    To =
                        "15:00" // To time
                    ,
                },
            };

            var tutorId = "tutorId";

            // Simulate a valid user (Tutor role)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            var studentProfile = new StudentProfile() { Id = 1 };
            _studentProfileRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(studentProfile);
            // Simulate the resource service response for a time slot duplicate message
            _resourceServiceMock
                .Setup(r =>
                    r.GetString(
                        SD.TIMESLOT_DUPLICATED_MESSAGE,
                        SD.TIME_SLOT,
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .Returns("Duplicate time slot found: From: 09:00, To: 10:00");

            // Simulate a duplicate time slot found in the list
            var duplicateSlot = new ScheduleTimeSlot
            {
                Weekday = 1, // Monday
                From = TimeSpan.Parse("09:00"), // From time
                To = TimeSpan.Parse("10:00"), // To time
                StudentProfile = new StudentProfile
                {
                    TutorId = tutorId,
                    Status = SD.StudentProfileStatus.Teaching,
                },
            };

            // Simulate that the duplicate slot is found in the list (return the first slot as duplicate)
            _scheduleTimeSlotRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ScheduleTimeSlot, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(duplicateSlot);

            // Act
            var result = await _controller.CreateAsync(createDTOs, 1);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response
                .ErrorMessages.Should()
                .Contain("Duplicate time slot found: From: 09:00, To: 10:00");
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnOk_WhenAllTimeSlotsAreValid()
        {
            // Arrange
            var createDTOs = new List<ScheduleTimeSlotCreateDTO>
            {
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 1,
                    From = "09:00",
                    To = "10:00",
                },
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 3,
                    From = "11:00",
                    To = "12:00",
                },
                new ScheduleTimeSlotCreateDTO
                {
                    Weekday = 5,
                    From = "14:00",
                    To = "15:00",
                },
            };

            var validStudentProfileId = 1; // Valid ID
            var tutorId = "test-tutor-id";

            // Simulate a valid user (Tutor role)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, tutorId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Simulate existing student profile for the tutor
            _studentProfileRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new StudentProfile { Id = validStudentProfileId, TutorId = tutorId });

            // Simulate repository calls for time slots and schedules
            _scheduleTimeSlotRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<ScheduleTimeSlot>()))
                .ReturnsAsync((ScheduleTimeSlot slot) => slot);

            _scheduleRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Schedule>()))
                .ReturnsAsync((Schedule schedule) => schedule);

            // Act
            var result = await _controller.CreateAsync(createDTOs, validStudentProfileId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Result.Should().NotBeNull();
            response.Result.Should().BeOfType<List<ScheduleTimeSlotDTO>>();
        }

        [Fact]
        public async Task UpdateAsync_ReturnsUnauthorized_WhenUserIsNotAuthenticated()
        {
            // Arrange
            _resourceServiceMock
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");

            // Simulate a user with no valid claims (unauthenticated)
            var claimsIdentity = new ClaimsIdentity(); // No claims added
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            var updateDTO = new ScheduleTimeSlotUpdateDTO
            {
                TimeSlotId = 1,
                Weekday = 1,
                From = "08:00",
                To = "10:00",
            };

            // Act
            var result = await _controller.UpdateAsync(updateDTO);
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
        public async Task UpdateAsync_ReturnsForbidden_WhenUserDoesNotHaveTutorRole()
        {
            // Arrange
            _resourceServiceMock
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbidden access.");

            // Simulate a user with valid NameIdentifier claim but missing TUTOR_ROLE
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "test-user-id") };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var updateDTO = new ScheduleTimeSlotUpdateDTO
            {
                TimeSlotId = 1,
                Weekday = 1,
                From = "08:00",
                To = "10:00",
            };

            // Act
            var result = await _controller.UpdateAsync(updateDTO);
            var forbiddenResult = result.Result as ObjectResult;

            // Assert
            forbiddenResult.Should().NotBeNull();
            forbiddenResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

            var apiResponse = forbiddenResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            apiResponse.ErrorMessages.First().Should().Be("Forbidden access.");
        }

        [Fact]
        public async Task UpdateAsync_ReturnsInternalServerError_WhenExceptionOccurs()
        {
            // Arrange
            var updateDTO = new ScheduleTimeSlotUpdateDTO
            {
                TimeSlotId = 1,
                Weekday = 1,
                From = "08:00",
                To = "10:00",
            };

            // Simulate a valid authenticated user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            _scheduleTimeSlotRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ScheduleTimeSlot, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ThrowsAsync(new Exception("Database error"));
            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error.");

            // Simulate an exception thrown during the update process
            _scheduleTimeSlotRepositoryMock
                .Setup(repo => repo.UpdateAsync(It.IsAny<ScheduleTimeSlot>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateAsync(updateDTO);
            var internalServerErrorResult = result.Result as ObjectResult;

            // Assert
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult
                .StatusCode.Should()
                .Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = internalServerErrorResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            apiResponse.ErrorMessages.Should().Contain("Internal server error.");
        }

        [Fact]
        public async Task UpdateAsync_ReturnsBadRequest_WhenTimeSlotIdIsInvalid()
        {
            // Arrange
            var updateDTO = new ScheduleTimeSlotUpdateDTO
            {
                TimeSlotId = 0, // Invalid ID
                Weekday = 1,
                From = "08:00",
                To = "10:00"
            };

            // Simulate a valid authenticated user with the TUTOR_ROLE
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
        new Claim(ClaimTypes.Role, SD.TUTOR_ROLE)
    };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Invalid ID provided.");

            // Act
            var result = await _controller.UpdateAsync(updateDTO);
            var badRequestResult = result.Result as BadRequestObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            apiResponse.ErrorMessages.Should().Contain("Invalid ID provided.");
        }

        [Fact]
        public async Task UpdateAsync_ReturnsNotFound_WhenTimeSlotDoesNotExist()
        {
            // Arrange
            var updateDTO = new ScheduleTimeSlotUpdateDTO
            {
                TimeSlotId = 1, // Non-existent TimeSlotId
                Weekday = 1,
                From = "08:00",
                To = "10:00",
            };

            // Simulate a valid authenticated user with the TUTOR_ROLE
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
        new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
    };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock repository to return null for the requested time slot
            _scheduleTimeSlotRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ScheduleTimeSlot, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((ScheduleTimeSlot)null);

            _resourceServiceMock
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.SCHEDULE))
                .Returns("Schedule time slot not found.");

            // Act
            var result = await _controller.UpdateAsync(updateDTO);
            var notFoundResult = result.Result as NotFoundObjectResult;

            // Assert
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

            var apiResponse = notFoundResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
            apiResponse.ErrorMessages.Should().Contain("Schedule time slot not found.");
        }

        [Fact]
        public async Task UpdateAsync_ReturnsBadRequest_WhenDuplicateTimeSlotDetected()
        {
            // Arrange
            var updateDTO = new ScheduleTimeSlotUpdateDTO
            {
                TimeSlotId = 1,
                Weekday = 1,
                From = "09:00",
                To = "11:00",
            };

            var oldTimeSlot = new ScheduleTimeSlot
            {
                Id = 1,
                Weekday = 1,
                From = TimeSpan.Parse("08:00"),
                To = TimeSpan.Parse("10:00"),
                IsDeleted = false,
            };

            var duplicateTimeSlot = new ScheduleTimeSlot
            {
                Id = 2,
                Weekday = 1,
                From = TimeSpan.Parse("10:00"),
                To = TimeSpan.Parse("12:00"),
                IsDeleted = false,
                StudentProfile = new StudentProfile
                {
                    TutorId = "test-tutor-id",
                    Status = SD.StudentProfileStatus.Teaching
                }
            };

            // Simulate a valid authenticated user with the TUTOR_ROLE
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
        new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
    };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock fetching the old time slot
            _scheduleTimeSlotRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<ScheduleTimeSlot, bool>>>(), true, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(oldTimeSlot);

            // Mock fetching the duplicate time slot
            _scheduleTimeSlotRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<ScheduleTimeSlot, bool>>>(), true, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(duplicateTimeSlot);

            _resourceServiceMock
                .Setup(r => r.GetString(SD.TIMESLOT_DUPLICATED_MESSAGE, SD.TIME_SLOT, "10:00", "12:00"))
                .Returns("Time slot from 10:00 to 12:00 is already in use.");

            // Act
            var result = await _controller.UpdateAsync(updateDTO);
            var badRequestResult = result.Result as BadRequestObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            apiResponse.ErrorMessages.Should().Contain("Time slot from 10:00 to 12:00 is already in use.");
        }

        [Fact]
        public async Task UpdateAsync_ReturnsOkResult_WhenUpdateIsValid()
        {
            // Arrange
            var updateDTO = new ScheduleTimeSlotUpdateDTO
            {
                TimeSlotId = 1,
                Weekday = 3,
                From = "09:00",
                To = "11:00",
            };

            var tutorId = "test-tutor-id";

            // Simulate a valid authenticated user with the TUTOR_ROLE
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, tutorId),
        new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
    };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var oldTimeSlot = new ScheduleTimeSlot
            {
                Id = 1,
                Weekday = 3,
                From = TimeSpan.Parse("08:00"),
                To = TimeSpan.Parse("10:00"),
                StudentProfileId = 1,
                IsDeleted = false
            };

            var newTimeSlot = new ScheduleTimeSlot
            {
                Id = 2,
                Weekday = 3,
                From = TimeSpan.Parse("09:00"),
                To = TimeSpan.Parse("11:00"),
                StudentProfileId = 1,
                IsDeleted = false
            };

            var newSchedule = new Schedule
            {
                Id = 1,
                TutorId = tutorId,
                ScheduleDate = DateTime.Today.AddDays(7),
                Start = TimeSpan.Parse("09:00"),
                End = TimeSpan.Parse("11:00"),
                ScheduleTimeSlotId = newTimeSlot.Id,
                AttendanceStatus = SD.AttendanceStatus.NOT_YET,
                PassingStatus = SD.PassingStatus.NOT_YET,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            _scheduleTimeSlotRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ScheduleTimeSlot, bool>>>(), true, It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync(oldTimeSlot);
            _scheduleTimeSlotRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ScheduleTimeSlot, bool>>>(), false, It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync((ScheduleTimeSlot)null);
            _scheduleTimeSlotRepositoryMock
                .Setup(repo => repo.UpdateAsync(It.IsAny<ScheduleTimeSlot>()))
                .ReturnsAsync(oldTimeSlot);

            _scheduleTimeSlotRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<ScheduleTimeSlot>()))
                .ReturnsAsync(newTimeSlot);

            _scheduleRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Schedule, bool>>>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<Expression<Func<Schedule, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((0, new List<Schedule> { }));

            _scheduleRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Schedule>()))
                .ReturnsAsync(newSchedule);

            // Act
            var result = await _controller.UpdateAsync(updateDTO);
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();

            var scheduleTimeSlotDto = apiResponse.Result as ScheduleTimeSlotDTO;
            scheduleTimeSlotDto.Should().NotBeNull();
            scheduleTimeSlotDto.Id.Should().Be(2);
            scheduleTimeSlotDto.Weekday.Should().Be(3);
            scheduleTimeSlotDto.From.Should().Be(TimeSpan.Parse("09:00"));
            scheduleTimeSlotDto.To.Should().Be(TimeSpan.Parse("11:00"));

            _scheduleTimeSlotRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<ScheduleTimeSlot>()), Times.Once);
            _scheduleTimeSlotRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<ScheduleTimeSlot>()), Times.Once);
            _scheduleRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Schedule>()), Times.Once);
        }

    }
}
