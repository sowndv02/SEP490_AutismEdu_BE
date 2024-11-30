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
using static AutismEduConnectSystem.SD;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace AutismEduConnectSystem.Controllers.v1.Tests
{
    public class ScheduleControllerTests
    {
        private readonly Mock<IScheduleRepository> _scheduleRepositoryMock;
        private readonly Mock<IStudentProfileRepository> _studentProfileRepositoryMock;
        private readonly Mock<IResourceService> _resourceServiceMock;
        private readonly IMapper _mapperMock;
        private readonly Mock<IChildInformationRepository> _childInfoRepositoryMock;
        private readonly Mock<ISyllabusRepository> _syllabusRepositoryMock;
        private readonly Mock<ILogger<ScheduleController>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly ScheduleController _controller;

        public ScheduleControllerTests()
        {
            // Initialize mock objects
            _scheduleRepositoryMock = new Mock<IScheduleRepository>();
            _studentProfileRepositoryMock = new Mock<IStudentProfileRepository>();
            _resourceServiceMock = new Mock<IResourceService>();
            _childInfoRepositoryMock = new Mock<IChildInformationRepository>();
            _syllabusRepositoryMock = new Mock<ISyllabusRepository>();
            _loggerMock = new Mock<ILogger<ScheduleController>>();
            _configurationMock = new Mock<IConfiguration>();

            // Setup for configuration
            _configurationMock.Setup(c => c["APIConfig:PageSize"]).Returns("10"); // Mock the PageSize value
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingConfig());
            });
            _mapperMock = config.CreateMapper();
            // Create the controller instance with mock dependencies
            _controller = new ScheduleController(
                _scheduleRepositoryMock.Object,
                _mapperMock,
                _studentProfileRepositoryMock.Object,
                _resourceServiceMock.Object,
                _childInfoRepositoryMock.Object,
                _syllabusRepositoryMock.Object,
                _loggerMock.Object,
                _configurationMock.Object
            );

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
            var submitterUser = new ApplicationUser { Id = "testTutorId", UserName = "testTutor" };
        }

        [Fact]
        public async Task GetByIdAsync_UserUnauthorized_ReturnsUnauthorized()
        {
            // Arrange
            var claimsIdentity = new ClaimsIdentity(); // No claims added
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            _resourceServiceMock
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized");

            // Act
            var result = await _controller.GetByIdAsync(1);

            // Assert
            var unauthorizedResult = result.Result as ObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            var response = unauthorizedResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("Unauthorized");
        }

        [Fact]
        public async Task GetByIdAsync_InvalidId_ReturnsBadRequest()
        {
            // Arrange
            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Invalid ID");

            // Act
            var result = await _controller.GetByIdAsync(0);

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("Invalid ID");
        }

        [Fact]
        public async Task GetByIdAsync_ScheduleNotFound_ReturnsNotFound()
        {
            // Arrange
            _scheduleRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((Schedule)null);

            _resourceServiceMock
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.SCHEDULE))
                .Returns("Schedule not found");

            // Act
            var result = await _controller.GetByIdAsync(1);

            // Assert
            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

            var response = notFoundResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("Schedule not found");
        }

        [Fact]
        public async Task GetByIdAsync_InternalServerError_ReturnsInternalServerError()
        {
            // Arrange

            _scheduleRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ThrowsAsync(new Exception("Database error"));

            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");

            // Act
            var result = await _controller.GetByIdAsync(1);

            // Assert
            var internalServerErrorResult = result.Result as ObjectResult;
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult
                .StatusCode.Should()
                .Be((int)HttpStatusCode.InternalServerError);

            var response = internalServerErrorResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("Internal server error");
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsOk_WhenScheduleExistsAndUserIsTutorRole()
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

            // Create mock Schedule data
            var schedule = new Schedule
            {
                Id = 1,
                ScheduleDate = DateTime.Now,
                ScheduleTimeSlotId = 2,
                Start = new TimeSpan(9, 0, 0),
                End = new TimeSpan(10, 0, 0),
                AttendanceStatus = AttendanceStatus.ATTENDED,
                PassingStatus = PassingStatus.PASSED,
                SyllabusId = 1,
                Exercise = new Exercise { Id = 1, ExerciseName = "Math Exercise" },
                ExerciseType = new ExerciseType { Id = 1, ExerciseTypeName = "Math" },
                Note = "Test Schedule",
                IsUpdatedSchedule = false,
                CreatedDate = DateTime.Now,
                UpdatedDate = null,
                StudentProfile = new StudentProfile() { Id = 1 },
            };

            // Set up mock repository
            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        false,
                        It.IsAny<string>(),
                        null
                    )
                )
                .ReturnsAsync(schedule);

            // Act
            var result = await _controller.GetByIdAsync(1);
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.Result.Should().BeOfType<ScheduleDTO>();

            var scheduleDto = (ScheduleDTO)apiResponse.Result;
            scheduleDto.Id.Should().Be(1);
            scheduleDto.ScheduleDate.Should().Be(schedule.ScheduleDate);
            scheduleDto.ScheduleTimeSlotId.Should().Be(schedule.ScheduleTimeSlotId);
            scheduleDto.Start.Should().Be(schedule.Start);
            scheduleDto.End.Should().Be(schedule.End);
            scheduleDto.AttendanceStatus.Should().Be(schedule.AttendanceStatus);
            scheduleDto.PassingStatus.Should().Be(schedule.PassingStatus);
            scheduleDto.SyllabusId.Should().Be(schedule.SyllabusId);
            scheduleDto.Note.Should().Be(schedule.Note);
            scheduleDto.IsUpdatedSchedule.Should().Be(schedule.IsUpdatedSchedule);
            scheduleDto.CreatedDate.Should().Be(schedule.CreatedDate);
        }

        [Fact]
        public async Task AssignExercises_InternalServerError_ReturnsInternalServerError()
        {
            // Arrange
            var updateDTO = new AssignExerciseScheduleDTO
            {
                Id = 1,
                ExerciseId = 1,
                ExerciseTypeId = 1,
                SyllabusId = 1,
            };

            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");
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

            // Simulate an exception during execution of the method
            _scheduleRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<System.Linq.Expressions.Expression<System.Func<Schedule, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ThrowsAsync(new System.Exception("Something went wrong"));

            // Act
            var result = await _controller.AssignExercises(1, updateDTO);

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
        public async Task AssignExercises_Forbidden_ReturnsForbidden()
        {
            // Arrange
            var updateDTO = new AssignExerciseScheduleDTO
            {
                Id = 1,
                ExerciseId = 1,
                ExerciseTypeId = 1,
                SyllabusId = 1,
            };

            _resourceServiceMock
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbidden access");

            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[] { new Claim(ClaimTypes.NameIdentifier, "12345") } // Missing TUTOR_ROLE
                )
            );

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Act
            var result = await _controller.AssignExercises(1, updateDTO);

            // Assert
            var forbiddenResult = result.Result as ObjectResult;
            forbiddenResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

            var response = forbiddenResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            response.ErrorMessages.Should().Contain("Forbidden access");
        }

        [Fact]
        public async Task AssignExercises_Unauthorized_ReturnsUnauthorized()
        {
            // Arrange
            var updateDTO = new AssignExerciseScheduleDTO
            {
                Id = 1,
                ExerciseId = 1,
                ExerciseTypeId = 1,
                SyllabusId = 1,
            };

            _resourceServiceMock
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access");

            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity() // No NameIdentifier claim
            );

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Act
            var result = await _controller.AssignExercises(1, updateDTO);

            // Assert
            var unauthorizedResult = result.Result as ObjectResult;
            unauthorizedResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            var response = unauthorizedResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            response.ErrorMessages.Should().Contain("Unauthorized access");
        }

        [Fact]
        public async Task AssignExercises_InvalidId_ReturnsBadRequest()
        {
            // Arrange
            var invalidId = 0; // Invalid ID (<= 0)
            var updateDTO = new AssignExerciseScheduleDTO
            {
                Id = invalidId,
                ExerciseId = 1,
                ExerciseTypeId = 1,
                SyllabusId = 1,
            };

            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Invalid ID provided");

            // Simulating a valid claims principal (authenticated user)
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

            // Act
            var result = await _controller.AssignExercises(invalidId, updateDTO);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.ErrorMessages.Should().Contain("Invalid ID provided");
        }

        [Fact]
        public async Task AssignExercises_InvalidUpdateData_ReturnsBadRequest()
        {
            // Arrange
            var invalidId = 1; // Valid ID for the test
            var invalidUpdateDTO = new AssignExerciseScheduleDTO
            {
                Id = 2, // ID mismatch, should be 1 to match
                ExerciseId = 1,
                ExerciseTypeId = 1,
                SyllabusId = 1,
            };

            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.SCHEDULE))
                .Returns("Invalid schedule update data");

            // Simulating a valid claims principal (authenticated user)
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

            // Act
            var result = await _controller.AssignExercises(invalidId, invalidUpdateDTO);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.ErrorMessages.Should().Contain("Invalid schedule update data");
        }

        [Fact]
        public async Task AssignExercises_ScheduleNotFound_ReturnsNotFound()
        {
            // Arrange
            var updateDTO = new AssignExerciseScheduleDTO
            {
                Id = 1,
                ExerciseId = 1,
                ExerciseTypeId = 1,
                SyllabusId = 1,
            };

            _resourceServiceMock
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.SCHEDULE))
                .Returns("Schedule not found");

            // Simulate a claims principal (authenticated user)
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

            // Simulate the scenario where the schedule is not found (model is null)
            _scheduleRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<System.Linq.Expressions.Expression<System.Func<Schedule, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((Schedule)null); // Simulate not finding the schedule

            // Act
            var result = await _controller.AssignExercises(1, updateDTO);

            // Assert
            var notFoundResult = result.Result as ObjectResult;
            notFoundResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

            var response = notFoundResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ErrorMessages.Should().Contain("Schedule not found");
        }

        [Fact]
        public async Task AssignExercises_ValidRequest_ReturnsNoContent()
        {
            // Arrange
            var updateDTO = new AssignExerciseScheduleDTO
            {
                Id = 1,
                ExerciseId = 1,
                ExerciseTypeId = 1,
                SyllabusId = 1,
            };

            // Mock the resource service to return appropriate messages
            _resourceServiceMock
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.SCHEDULE))
                .Returns("Schedule not found");
            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Invalid ID");
            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.SCHEDULE))
                .Returns("Invalid schedule data");
            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("An internal server error occurred.");

            // Simulate a claims principal (authenticated user with TUTOR_ROLE)
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

            // Simulate that the schedule with the given ID exists
            var existingSchedule = new Schedule
            {
                Id = 1,
                ExerciseId = 1,
                ExerciseTypeId = 1,
                SyllabusId = 1,
                UpdatedDate = DateTime.Now,
            };

            _scheduleRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<System.Linq.Expressions.Expression<System.Func<Schedule, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(existingSchedule);

            // Mock UpdateAsync to simulate successful update
            _scheduleRepositoryMock
                .Setup(repo => repo.UpdateAsync(It.IsAny<Schedule>()))
                .ReturnsAsync(existingSchedule);

            // Act
            var result = await _controller.AssignExercises(1, updateDTO);

            // Assert
            var okResult = result.Result as ObjectResult;
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            response.ErrorMessages.Should().BeEmpty();
            response.Result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAllNotPassedExerciseAsync_Unauthorized_ReturnsUnauthorized()
        {
            // Arrange
            var studentProfileId = 1; // Example student profile ID

            // Mock the resource service to return the unauthorized message
            _resourceServiceMock
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access");

            // Create a claims principal with no "NameIdentifier" claim to simulate an unauthorized user
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Act
            var result = await _controller.GetAllNotPassedExerciseAsync(studentProfileId);

            // Assert
            var unauthorizedResult = result.Result as ObjectResult;
            unauthorizedResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            var response = unauthorizedResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            response.ErrorMessages.Should().Contain("Unauthorized access");
        }

        [Fact]
        public async Task GetAllNotPassedExerciseAsync_InvalidStudentProfileId_ReturnsBadRequest()
        {
            // Arrange
            var studentProfileId = 0; // Invalid studentProfileId (<= 0)

            // Mock the resource service to return the bad request message
            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Invalid ID provided");

            // Create a valid claims principal (since authentication is not the focus here)
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

            // Act
            var result = await _controller.GetAllNotPassedExerciseAsync(studentProfileId);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.ErrorMessages.Should().Contain("Invalid ID provided");
        }

        [Fact]
        public async Task GetAllNotPassedExerciseAsync_ValidStudentProfileId_ReturnsOk()
        {
            // Arrange
            var studentProfileId = 1; // Valid studentProfileId

            // Mock the resource service to return appropriate messages
            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Invalid ID provided");

            // Create a valid claims principal (since authentication is valid)
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "12345"), // UserId
                        new Claim(ClaimTypes.Role, SD.TUTOR_ROLE), // Role
                    }
                )
            );

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Mock the repository to return some data
            // First, return failed exercises with ExerciseId 1, 2, and 3
            var failedSchedules = new List<Schedule>
            {
                new Schedule { ExerciseId = 1 },
                new Schedule { ExerciseId = 2 },
                new Schedule { ExerciseId = 3 },
            };

            // Second, return passed exercises with ExerciseId 1
            var passedSchedules = new List<Schedule> { new Schedule { ExerciseId = 1 } };

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
                .ReturnsAsync((failedSchedules.Count, failedSchedules)); // Return the failed exercises
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
                .ReturnsAsync((passedSchedules.Count, passedSchedules)); // Return the passed exercises

            // Act
            var result = await _controller.GetAllNotPassedExerciseAsync(studentProfileId);

            // Assert
            var okResult = result.Result as ObjectResult;
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task UpdateAsync_InvalidRoleUser_ReturnsForbidden()
        {
            // Arrange
            var studentProfileId = 1;
            var updateDTO = new ScheduleUpdateDTO
            {
                Id = studentProfileId,
                AttendanceStatus = AttendanceStatus.ABSENT,
                PassingStatus = PassingStatus.NOT_YET,
                Note = "Test Note",
            };
            _resourceServiceMock
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbiden access.");
            // Mock the claims principal to simulate a user with an invalid role (not a Tutor)
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "12345"), // UserId
                        new Claim(ClaimTypes.Role, "Student"), // Invalid Role
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Act
            var result = await _controller.UpdateAsync(studentProfileId, updateDTO);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
            var response = objectResult.Value as APIResponse;
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("Forbiden access.");
        }

        [Fact]
        public async Task UpdateAsync_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            var studentProfileId = 1;
            var updateDTO = new ScheduleUpdateDTO
            {
                Id = studentProfileId,
                AttendanceStatus = AttendanceStatus.ABSENT,
                PassingStatus = PassingStatus.NOT_YET,
                Note = "Test Note",
            };
            _resourceServiceMock
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");
            // Mock the claims principal to simulate an unauthenticated user (No NameIdentifier)
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Act
            var result = await _controller.UpdateAsync(studentProfileId, updateDTO);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
            var response = objectResult.Value as APIResponse;
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("Unauthorized access.");
        }

        [Fact]
        public async Task UpdateAsync_InternalServerError_ReturnsInternalServerError()
        {
            // Arrange
            var studentProfileId = 1;
            var updateDTO = new ScheduleUpdateDTO
            {
                Id = studentProfileId,
                AttendanceStatus = AttendanceStatus.ABSENT,
                PassingStatus = PassingStatus.NOT_YET,
                Note = "Test Note",
            };
            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("An internal server error occurred.");

            // Mock the claims principal to simulate a valid user (Authenticated)
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "12345"), // UserId
                        new Claim(
                            ClaimTypes.Role,
                            SD.TUTOR_ROLE
                        ) // Tutor Role
                        ,
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Mock the repository to throw an exception when updating the schedule
            _scheduleRepositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<Schedule>()))
                .ThrowsAsync(new Exception("Some internal error"));
            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ThrowsAsync(new Exception("Some internal error"));
            // Act
            var result = await _controller.UpdateAsync(studentProfileId, updateDTO);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
            var response = objectResult.Value as APIResponse;
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("An internal server error occurred.");
        }

        [Fact]
        public async Task UpdateAsync_InvalidId_ReturnsBadRequest()
        {
            // Arrange
            var invalidId = 0; // Invalid ID, should trigger BadRequest
            var updateDTO = new ScheduleUpdateDTO
            {
                Id = invalidId,
                AttendanceStatus = AttendanceStatus.ABSENT,
                PassingStatus = PassingStatus.NOT_YET,
                Note = "Test Note",
            };

            // Mock the error message for the BadRequest case
            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Invalid ID provided.");

            // Mock the claims principal to simulate a valid user (Authenticated)
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "12345"), // UserId
                        new Claim(
                            ClaimTypes.Role,
                            SD.TUTOR_ROLE
                        ) // Tutor Role
                        ,
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Act
            var result = await _controller.UpdateAsync(invalidId, updateDTO);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            var response = objectResult.Value as APIResponse;
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("Invalid ID provided.");
        }

        [Fact]
        public async Task UpdateAsync_ScheduleIdMismatchOrNullRequest_ReturnsBadRequest()
        {
            // Arrange
            var validId = 1; // Valid ID for the schedule
            ScheduleUpdateDTO updateDTO = null; // Simulate null updateDTO, which should trigger BadRequest

            // Mock the error message for the BadRequest case
            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.SCHEDULE))
                .Returns("Invalid update request for schedule.");

            // Mock the claims principal to simulate a valid user (Authenticated)
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "12345"), // UserId
                        new Claim(
                            ClaimTypes.Role,
                            SD.TUTOR_ROLE
                        ) // Tutor Role
                        ,
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Act
            var result = await _controller.UpdateAsync(validId, updateDTO);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            var response = objectResult.Value as APIResponse;
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("Invalid update request for schedule.");
        }

        [Fact]
        public async Task UpdateAsync_ScheduleNotFound_ReturnsNotFound()
        {
            // Arrange
            var validId = 1; // Valid ID for the schedule
            var userId = "12345"; // Simulate a valid user ID
            var updateDTO = new ScheduleUpdateDTO
            {
                Id = validId,
                AttendanceStatus = AttendanceStatus.ABSENT,
                PassingStatus = PassingStatus.NOT_YET,
                Note = "Test Note",
            };

            // Mock the error message for the NotFound case
            _resourceServiceMock
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.SCHEDULE))
                .Returns("Schedule not found.");

            // Mock the repository to return null, simulating a schedule not found
            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((Schedule)null);

            // Mock the claims principal to simulate a valid user (Authenticated)
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId), // UserId
                        new Claim(
                            ClaimTypes.Role,
                            SD.TUTOR_ROLE
                        ) // Tutor Role
                        ,
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Act
            var result = await _controller.UpdateAsync(validId, updateDTO);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            var response = objectResult.Value as APIResponse;
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("Schedule not found.");
        }

        [Fact]
        public async Task UpdateAsync_ValidUpdate_NoNextSchedule_ReturnsOk()
        {
            // Arrange
            var validId = 1; // Valid ID for the schedule
            var userId = "12345"; // Simulate a valid user ID
            var updateDTO = new ScheduleUpdateDTO
            {
                Id = validId,
                AttendanceStatus = AttendanceStatus.ATTENDED,
                PassingStatus = PassingStatus.PASSED,
                Note = "Updated Note",
            };

            // Mock the valid schedule model
            var schedule = new Schedule
            {
                Id = validId,
                AttendanceStatus = AttendanceStatus.NOT_YET,
                PassingStatus = PassingStatus.NOT_YET,
                Note = "Initial Note",
                ScheduleDate = DateTime.Now.AddDays(-1), // Example date
                StudentProfileId = 1,
                ExerciseId = 1,
                ExerciseTypeId = 1,
                TutorId = userId,
            };

            // Mock the repository to return the schedule when fetching it by ID
            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(schedule);

            // Mock the repository to return the updated schedule after the update
            _scheduleRepositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<Schedule>()))
                .ReturnsAsync(schedule);

            // Mock the next schedule to return null (no next schedule found)
            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAsync(It.IsAny<Expression<Func<Schedule, bool>>>(), false, null, null)
                )
                .ReturnsAsync((Schedule)null);

            // Mock the claims principal to simulate a valid user (Authenticated)
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId), // UserId
                        new Claim(
                            ClaimTypes.Role,
                            SD.TUTOR_ROLE
                        ) // Tutor Role
                        ,
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Act
            var result = await _controller.UpdateAsync(validId, updateDTO);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
            var response = objectResult.Value as APIResponse;
            response.IsSuccess.Should().BeTrue();
            response.Result.Should().NotBeNull();
            var scheduleDTO = response.Result as ScheduleDTO;
            scheduleDTO.Id.Should().Be(validId);
            scheduleDTO.AttendanceStatus.Should().Be(updateDTO.AttendanceStatus);
            scheduleDTO.PassingStatus.Should().Be(updateDTO.PassingStatus);
            scheduleDTO.Note.Should().Be(updateDTO.Note);

            // Ensure no error messages are added for the next schedule
            response.ErrorMessages.FirstOrDefault().Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_ValidUpdate_NextScheduleFound_ReturnsOk()
        {
            // Arrange
            var validId = 1; // Valid ID for the schedule
            var userId = "12345"; // Simulate a valid user ID
            var updateDTO = new ScheduleUpdateDTO
            {
                Id = validId,
                AttendanceStatus = AttendanceStatus.ATTENDED,
                PassingStatus = PassingStatus.PASSED,
                Note = "Updated Note",
            };

            // Mock the valid schedule model
            var schedule = new Schedule
            {
                Id = validId,
                AttendanceStatus = AttendanceStatus.NOT_YET,
                PassingStatus = PassingStatus.NOT_YET,
                Note = "Initial Note",
                ScheduleDate = DateTime.Now.AddDays(-1), // Example date
                StudentProfileId = 1,
                ExerciseId = 1,
                ExerciseTypeId = 1,
                TutorId = userId,
            };

            // Mock the repository to return the schedule when fetching it by ID
            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(schedule);

            // Mock the repository to return the updated schedule after the update
            _scheduleRepositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<Schedule>()))
                .ReturnsAsync(schedule);

            // Mock the next schedule to return a schedule (next schedule found)
            var nextSchedule = new Schedule
            {
                Id = 2,
                AttendanceStatus = AttendanceStatus.NOT_YET,
                PassingStatus = PassingStatus.NOT_YET,
                ScheduleDate = DateTime.Now.AddDays(1), // Example date
                ExerciseId = schedule.ExerciseId,
                ExerciseTypeId = schedule.ExerciseTypeId,
            };
            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAsync(It.IsAny<Expression<Func<Schedule, bool>>>(), false, null, null)
                )
                .ReturnsAsync(nextSchedule);
            _resourceServiceMock
                .Setup(r => r.GetString(SD.DUPPLICATED_ASSIGN_EXERCISE))
                .Returns("Duplicated exercise.");
            // Mock the claims principal to simulate a valid user (Authenticated)
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId), // UserId
                        new Claim(
                            ClaimTypes.Role,
                            SD.TUTOR_ROLE
                        ) // Tutor Role
                        ,
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Act
            var result = await _controller.UpdateAsync(validId, updateDTO);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
            var response = objectResult.Value as APIResponse;
            response.IsSuccess.Should().BeTrue();
            response.Result.Should().NotBeNull();
            var scheduleDTO = response.Result as ScheduleDTO;
            scheduleDTO.Id.Should().Be(validId);
            scheduleDTO.AttendanceStatus.Should().Be(updateDTO.AttendanceStatus);
            scheduleDTO.PassingStatus.Should().Be(updateDTO.PassingStatus);
            scheduleDTO.Note.Should().Be(updateDTO.Note);

            // Ensure the error message indicates a duplicated exercise assignment
            response.ErrorMessages.Should().Contain("Duplicated exercise.");
        }

        [Fact]
        public async Task GetAllAsync_InternalServerError_ReturnsInternalServerError()
        {
            // Arrange
            var studentProfileId = 1;
            var startDate = DateTime.Now.AddDays(-7);
            var endDate = DateTime.Now;
            var tutorId = "12345";
            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");
            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, tutorId) })
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Mock the repository to throw an exception
            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<Schedule, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAllAsync(studentProfileId, startDate, endDate);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("Internal server error");
        }

        [Fact]
        public async Task GetAllAsync_AuthenticationInvalid_ReturnsUnauthorized()
        {
            // Arrange
            var studentProfileId = 1;
            var startDate = DateTime.Now.AddDays(-7);
            var endDate = DateTime.Now;

            // Mock claims principal to simulate no user (unauthenticated)
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            };
            _resourceServiceMock
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");
            // Act
            var result = await _controller.GetAllAsync(studentProfileId, startDate, endDate);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("Unauthorized access.");
        }

        [Fact]
        public async Task GetAllAsync_ValidRequestWithStudentProfileIdAndGetAll_ReturnsSchedules()
        {
            // Arrange
            var studentProfileId = 1;
            var startDate = DateTime.Now.AddDays(-7);
            DateTime? endDate = null;
            bool getAll = true;

            var tutorId = "12345";

            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, tutorId) })
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Mock repository to return schedules for a specific StudentProfileId
            var mockSchedules = new List<Schedule>
            {
                new Schedule
                {
                    Id = 1,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now,
                    Start = TimeSpan.Parse("10:00"), // Use TimeSpan.Parse
                    End = TimeSpan.Parse("11:00"), // Use TimeSpan.Parse
                    IsHidden = false,
                },
                new Schedule
                {
                    Id = 2,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now.AddDays(1),
                    Start = TimeSpan.Parse("14:00"), // Convert "2:00 PM" to 24-hour format
                    End = TimeSpan.Parse("15:00"), // Convert "3:00 PM" to 24-hour format
                    IsHidden = false,
                },
            };
            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<string>(),
                        null,
                        null,
                        true
                    )
                )
                .ReturnsAsync((mockSchedules.Count, mockSchedules));

            // Mock dependent repositories
            _studentProfileRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(
                    new StudentProfile
                    {
                        Id = studentProfileId,
                        Status = SD.StudentProfileStatus.Teaching,
                    }
                );

            _childInfoRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new ChildInformation { Id = 1 });

            _syllabusRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new Syllabus { Id = 1 });

            // Act
            var result = await _controller.GetAllAsync(
                studentProfileId,
                startDate,
                endDate,
                getAll
            );

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Result.Should().NotBeNull();

            var model = response.Result as ListScheduleDTO;
            model.Should().NotBeNull();
            model.Schedules.Should().HaveCount(mockSchedules.Count);
            model.MaxDate.Should().Be(mockSchedules.Max(x => x.ScheduleDate.Date));
        }

        [Fact]
        public async Task GetAllAsync_ValidRequestWithStartDateEndDateGetAllAndStudentProfileId_ReturnsFilteredSchedules()
        {
            // Arrange
            var studentProfileId = 1;
            var startDate = DateTime.Now.AddDays(-7);
            var endDate = DateTime.Now.AddDays(7);
            bool getAll = true;

            var tutorId = "12345";

            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, tutorId) })
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Mock repository to return schedules filtered by StudentProfileId and date range
            var mockSchedules = new List<Schedule>
            {
                new Schedule
                {
                    Id = 1,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now,
                    Start = TimeSpan.Parse("10:00"), // Use TimeSpan.Parse
                    End = TimeSpan.Parse("11:00"), // Use TimeSpan.Parse
                    IsHidden = false,
                },
                new Schedule
                {
                    Id = 2,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now.AddDays(1),
                    Start = TimeSpan.Parse("14:00"), // Convert "2:00 PM" to 24-hour format
                    End = TimeSpan.Parse("15:00"), // Convert "3:00 PM" to 24-hour format
                    IsHidden = false,
                },
            };

            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<string>(),
                        null,
                        null,
                        true
                    )
                )
                .ReturnsAsync((mockSchedules.Count, mockSchedules));

            // Mock dependent repositories
            _studentProfileRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(
                    new StudentProfile
                    {
                        Id = studentProfileId,
                        Status = SD.StudentProfileStatus.Teaching,
                    }
                );

            _childInfoRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new ChildInformation { Id = 1 });

            _syllabusRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new Syllabus { Id = 1 });

            // Act
            var result = await _controller.GetAllAsync(
                studentProfileId,
                startDate,
                endDate,
                getAll
            );

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Result.Should().NotBeNull();

            var model = response.Result as ListScheduleDTO;
            model.Should().NotBeNull();
            model.Schedules.Should().HaveCount(mockSchedules.Count);

            // Ensure schedules are within the date range
            model
                .Schedules.All(s => s.ScheduleDate >= startDate && s.ScheduleDate <= endDate)
                .Should()
                .BeTrue();

            model.MaxDate.Should().Be(mockSchedules.Max(x => x.ScheduleDate.Date));
        }

        [Fact]
        public async Task GetAllAsync_ValidRequestWithEndDateAndGetAll_ReturnsFilteredSchedules()
        {
            // Arrange
            var studentProfileId = 1;
            DateTime? startDate = null;
            var endDate = DateTime.Now.AddDays(7);
            bool getAll = true;

            var tutorId = "12345";

            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, tutorId) })
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Mock repository to return schedules filtered by StudentProfileId and EndDate
            var mockSchedules = new List<Schedule>
            {
                new Schedule
                {
                    Id = 1,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now,
                    Start = TimeSpan.Parse("10:00"),
                    End = TimeSpan.Parse("11:00"),
                    IsHidden = false,
                },
                new Schedule
                {
                    Id = 2,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now.AddDays(1),
                    Start = TimeSpan.Parse("14:00"),
                    End = TimeSpan.Parse("15:00"),
                    IsHidden = false,
                },
            };

            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<string>(),
                        null,
                        null,
                        true
                    )
                )
                .ReturnsAsync((mockSchedules.Count, mockSchedules));

            // Mock dependent repositories
            _studentProfileRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(
                    new StudentProfile
                    {
                        Id = studentProfileId,
                        Status = SD.StudentProfileStatus.Teaching,
                    }
                );

            _childInfoRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new ChildInformation { Id = 1 });

            _syllabusRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new Syllabus { Id = 1 });

            // Act
            var result = await _controller.GetAllAsync(
                studentProfileId,
                startDate,
                endDate,
                getAll
            );

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Result.Should().NotBeNull();

            var model = response.Result as ListScheduleDTO;
            model.Should().NotBeNull();
            model.Schedules.Should().HaveCount(mockSchedules.Count);

            // Ensure schedules are within the date range defined by EndDate
            model.Schedules.All(s => s.ScheduleDate <= endDate.Date).Should().BeTrue();

            model.MaxDate.Should().Be(mockSchedules.Max(x => x.ScheduleDate.Date));
        }

        [Fact]
        public async Task GetAllAsync_ValidRequestWithNoDateRangeAndGetAll_ReturnsAllSchedules()
        {
            // Arrange
            var studentProfileId = 1;
            DateTime? startDate = null;
            DateTime? endDate = null;
            bool getAll = true;

            var tutorId = "12345";

            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, tutorId) })
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Mock repository to return all schedules for a specific StudentProfileId
            var mockSchedules = new List<Schedule>
            {
                new Schedule
                {
                    Id = 1,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now,
                    Start = TimeSpan.Parse("10:00"),
                    End = TimeSpan.Parse("11:00"),
                    IsHidden = false,
                },
                new Schedule
                {
                    Id = 2,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now.AddDays(1),
                    Start = TimeSpan.Parse("14:00"),
                    End = TimeSpan.Parse("15:00"),
                    IsHidden = false,
                },
                new Schedule
                {
                    Id = 3,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now.AddDays(2),
                    Start = TimeSpan.Parse("16:00"),
                    End = TimeSpan.Parse("17:00"),
                    IsHidden = false,
                },
            };

            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<string>(),
                        null,
                        null,
                        true
                    )
                )
                .ReturnsAsync((mockSchedules.Count, mockSchedules));

            // Mock dependent repositories
            _studentProfileRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(
                    new StudentProfile
                    {
                        Id = studentProfileId,
                        Status = SD.StudentProfileStatus.Teaching,
                    }
                );

            _childInfoRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new ChildInformation { Id = 1 });

            _syllabusRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new Syllabus { Id = 1 });

            // Act
            var result = await _controller.GetAllAsync(
                studentProfileId,
                startDate,
                endDate,
                getAll
            );

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Result.Should().NotBeNull();

            var model = response.Result as ListScheduleDTO;
            model.Should().NotBeNull();
            model.Schedules.Should().HaveCount(mockSchedules.Count);

            // Verify that all schedules for the given StudentProfileId are included
            model.Schedules.All(s => s.StudentProfile.Id == studentProfileId).Should().BeTrue();

            model.MaxDate.Should().Be(mockSchedules.Max(x => x.ScheduleDate.Date));
        }

        [Fact]
        public async Task GetAllAsync_ValidRequestWithStudentProfileIdAndNoDateRange_ReturnsFilteredSchedules()
        {
            // Arrange
            var studentProfileId = 1;
            DateTime? startDate = null;
            DateTime? endDate = null;
            bool getAll = false;

            var tutorId = "12345";

            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, tutorId) })
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Mock repository to return a filtered set of schedules
            var mockSchedules = new List<Schedule>
            {
                new Schedule
                {
                    Id = 1,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now,
                    Start = TimeSpan.Parse("10:00"),
                    End = TimeSpan.Parse("11:00"),
                    IsHidden = false,
                },
                new Schedule
                {
                    Id = 2,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now.AddDays(-3),
                    Start = TimeSpan.Parse("14:00"),
                    End = TimeSpan.Parse("15:00"),
                    IsHidden = false,
                },
                new Schedule
                {
                    Id = 3,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now.AddDays(-10), // This schedule should not be included
                    Start = TimeSpan.Parse("16:00"),
                    End = TimeSpan.Parse("17:00"),
                    IsHidden = false,
                },
            };

            var filteredSchedules = mockSchedules
                .Where(s =>
                    s.ScheduleDate >= DateTime.Now.AddDays(-7) && s.ScheduleDate <= DateTime.Now
                )
                .ToList();

            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<string>(),
                        null,
                        null,
                        true
                    )
                )
                .ReturnsAsync((filteredSchedules.Count, filteredSchedules));

            // Mock dependent repositories
            _studentProfileRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(
                    new StudentProfile
                    {
                        Id = studentProfileId,
                        Status = SD.StudentProfileStatus.Teaching,
                    }
                );

            _childInfoRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new ChildInformation { Id = 1 });

            _syllabusRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new Syllabus { Id = 1 });

            // Act
            var result = await _controller.GetAllAsync(
                studentProfileId,
                startDate,
                endDate,
                getAll
            );

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Result.Should().NotBeNull();

            var model = response.Result as ListScheduleDTO;
            model.Should().NotBeNull();
            model.Schedules.Should().HaveCount(filteredSchedules.Count);

            // Verify that schedules are correctly filtered by date range
            model
                .Schedules.All(s =>
                    s.ScheduleDate >= DateTime.Now.AddDays(-7) && s.ScheduleDate <= DateTime.Now
                )
                .Should()
                .BeTrue();

            model.MaxDate.Should().Be(filteredSchedules.Max(x => x.ScheduleDate.Date));
        }

        [Fact]
        public async Task GetAllAsync_ValidRequestWithStudentProfileIdAndEndDate_ReturnsSchedules()
        {
            // Arrange
            var studentProfileId = 1;
            DateTime? startDate = null;
            DateTime? endDate = DateTime.Now.AddDays(7); // EndDate is not null
            bool getAll = false;

            var tutorId = "12345";

            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, tutorId) })
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Mock repository to return schedules for a specific StudentProfileId
            var mockSchedules = new List<Schedule>
            {
                new Schedule
                {
                    Id = 1,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now.AddDays(3),
                    Start = TimeSpan.Parse("10:00"), // Use TimeSpan.Parse
                    End = TimeSpan.Parse("11:00"), // Use TimeSpan.Parse
                    IsHidden = false,
                },
                new Schedule
                {
                    Id = 2,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now.AddDays(5),
                    Start = TimeSpan.Parse("14:00"), // Use TimeSpan.Parse
                    End = TimeSpan.Parse("15:00"), // Use TimeSpan.Parse
                    IsHidden = false,
                },
            };
            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(), // Filter by startDate
                        It.IsAny<Expression<Func<Schedule, object>>>(),
                        true // Reflecting the getAll = false
                    )
                )
                .ReturnsAsync((mockSchedules.Count, mockSchedules));

            // Mock dependent repositories
            _studentProfileRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(
                    new StudentProfile
                    {
                        Id = studentProfileId,
                        Status = SD.StudentProfileStatus.Teaching,
                    }
                );

            _childInfoRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new ChildInformation { Id = 1 });

            _syllabusRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new Syllabus { Id = 1 });

            // Act
            var result = await _controller.GetAllAsync(
                studentProfileId,
                startDate,
                endDate,
                getAll
            );

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Result.Should().NotBeNull();

            var model = response.Result as ListScheduleDTO;
            model.Should().NotBeNull();
            model.Schedules.Should().HaveCount(mockSchedules.Count);
            model.Schedules.All(x => x.ScheduleDate <= endDate).Should().BeTrue();
            model.MaxDate.Should().Be(mockSchedules.Max(x => x.ScheduleDate.Date));
        }

        [Fact]
        public async Task GetAllAsync_ValidRequestWithStartDateEndDateAndGetAllFalse_ReturnsFilteredSchedules()
        {
            // Arrange
            var studentProfileId = 1;
            var startDate = DateTime.Now.AddDays(-7); // StartDate is not null
            var endDate = DateTime.Now.AddDays(7); // EndDate is not null
            bool getAll = false;

            var tutorId = "12345";

            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, tutorId) })
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Mock repository to return schedules filtered by the date range and StudentProfileId
            var mockSchedules = new List<Schedule>
            {
                new Schedule
                {
                    Id = 1,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now.AddDays(-3),
                    Start = TimeSpan.Parse("09:00"),
                    End = TimeSpan.Parse("10:00"),
                    IsHidden = false,
                },
                new Schedule
                {
                    Id = 2,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now.AddDays(3),
                    Start = TimeSpan.Parse("14:00"),
                    End = TimeSpan.Parse("15:00"),
                    IsHidden = false,
                },
            };

            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<string>(),
                        null,
                        null,
                        true // Reflecting getAll = false
                    )
                )
                .ReturnsAsync((mockSchedules.Count, mockSchedules));

            // Mock dependent repositories
            _studentProfileRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(
                    new StudentProfile
                    {
                        Id = studentProfileId,
                        Status = SD.StudentProfileStatus.Teaching,
                    }
                );

            _childInfoRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new ChildInformation { Id = 1 });

            _syllabusRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new Syllabus { Id = 1 });

            // Act
            var result = await _controller.GetAllAsync(
                studentProfileId,
                startDate,
                endDate,
                getAll
            );

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Result.Should().NotBeNull();

            var model = response.Result as ListScheduleDTO;
            model.Should().NotBeNull();
            model.Schedules.Should().HaveCount(mockSchedules.Count);
            model
                .Schedules.All(x => x.ScheduleDate >= startDate && x.ScheduleDate <= endDate)
                .Should()
                .BeTrue();
            model.MaxDate.Should().Be(mockSchedules.Max(x => x.ScheduleDate.Date));
        }

        [Fact]
        public async Task GetAllAsync_ValidRequestWithStartDateAndGetAllFalse_ReturnsFilteredSchedules()
        {
            // Arrange
            var studentProfileId = 1;
            var startDate = DateTime.Now.AddDays(-7); // StartDate is not null
            DateTime? endDate = null; // EndDate is null
            bool getAll = false;

            var tutorId = "12345";

            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, tutorId) })
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Mock repository to return schedules filtered by StartDate and StudentProfileId
            var mockSchedules = new List<Schedule>
            {
                new Schedule
                {
                    Id = 1,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now.AddDays(-3), // Within StartDate range
                    Start = TimeSpan.Parse("09:00"),
                    End = TimeSpan.Parse("10:00"),
                    IsHidden = false,
                },
                new Schedule
                {
                    Id = 2,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now.AddDays(2), // After StartDate
                    Start = TimeSpan.Parse("14:00"),
                    End = TimeSpan.Parse("15:00"),
                    IsHidden = false,
                },
            };

            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<string>(),
                        null,
                        null,
                        true // Reflecting getAll = false
                    )
                )
                .ReturnsAsync((mockSchedules.Count, mockSchedules));

            // Mock dependent repositories
            _studentProfileRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(
                    new StudentProfile
                    {
                        Id = studentProfileId,
                        Status = SD.StudentProfileStatus.Teaching,
                    }
                );

            _childInfoRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new ChildInformation { Id = 1 });

            _syllabusRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new Syllabus { Id = 1 });

            // Act
            var result = await _controller.GetAllAsync(
                studentProfileId,
                startDate,
                endDate,
                getAll
            );

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Result.Should().NotBeNull();

            var model = response.Result as ListScheduleDTO;
            model.Should().NotBeNull();
            model.Schedules.Should().HaveCount(mockSchedules.Count);
            model.Schedules.All(x => x.ScheduleDate >= startDate).Should().BeTrue();
            model.MaxDate.Should().Be(mockSchedules.Max(x => x.ScheduleDate.Date));
        }

        [Fact]
        public async Task GetAllAsync_ValidRequestWithStartDateAndGetAllTrueWithNoStudentProfileId_ReturnsSchedules()
        {
            // Arrange
            var studentProfileId = 0; // StudentProfileId is 0
            var startDate = DateTime.Now.AddDays(-7); // StartDate is not null
            DateTime? endDate = null; // EndDate is null
            bool getAll = true; // GetAll is true

            var tutorId = "12345";

            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, tutorId) })
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Mock repository to return schedules for a specific TutorId (no filtering by StudentProfileId)
            var mockSchedules = new List<Schedule>
            {
                new Schedule
                {
                    Id = 1,
                    StudentProfileId = 1, // StudentProfileId does not match the requested 0
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now,
                    Start = TimeSpan.Parse("10:00"),
                    End = TimeSpan.Parse("11:00"),
                    IsHidden = false,
                },
                new Schedule
                {
                    Id = 2,
                    StudentProfileId = 2, // Another StudentProfileId
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now.AddDays(1),
                    Start = TimeSpan.Parse("14:00"),
                    End = TimeSpan.Parse("15:00"),
                    IsHidden = false,
                },
            };

            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<string>(),
                        null,
                        null,
                        true // Reflecting GetAll = true
                    )
                )
                .ReturnsAsync((mockSchedules.Count, mockSchedules));

            // Mock dependent repositories
            _studentProfileRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(
                    new StudentProfile
                    {
                        Id = studentProfileId,
                        Status = SD.StudentProfileStatus.Teaching,
                    }
                );

            _childInfoRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new ChildInformation { Id = 1 });

            _syllabusRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new Syllabus { Id = 1 });

            // Act
            var result = await _controller.GetAllAsync(
                studentProfileId,
                startDate,
                endDate,
                getAll
            );

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Result.Should().NotBeNull();

            var model = response.Result as ListScheduleDTO;
            model.Should().NotBeNull();
            model.Schedules.Should().HaveCount(mockSchedules.Count); // All schedules are returned since GetAll is true
            model.MaxDate.Should().Be(mockSchedules.Max(x => x.ScheduleDate.Date));
        }

        [Fact]
        public async Task GetAllAsync_ValidRequestWithStartDateAndEndDateAndGetAllTrueWithNoStudentProfileId_ReturnsSchedules()
        {
            // Arrange
            var studentProfileId = 0; // StudentProfileId is 0
            var startDate = DateTime.Now.AddDays(-7); // StartDate is not null
            var endDate = DateTime.Now.AddDays(7); // EndDate is not null
            bool getAll = true; // GetAll is true

            var tutorId = "12345";

            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, tutorId) })
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Mock repository to return schedules for a specific TutorId (no filtering by StudentProfileId)
            var mockSchedules = new List<Schedule>
            {
                new Schedule
                {
                    Id = 1,
                    StudentProfileId = 1, // StudentProfileId does not match the requested 0
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now,
                    Start = TimeSpan.Parse("10:00"),
                    End = TimeSpan.Parse("11:00"),
                    IsHidden = false,
                },
                new Schedule
                {
                    Id = 2,
                    StudentProfileId = 2, // Another StudentProfileId
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now.AddDays(1),
                    Start = TimeSpan.Parse("14:00"),
                    End = TimeSpan.Parse("15:00"),
                    IsHidden = false,
                },
                new Schedule
                {
                    Id = 3,
                    StudentProfileId = 3, // Another StudentProfileId
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now.AddDays(-1),
                    Start = TimeSpan.Parse("09:00"),
                    End = TimeSpan.Parse("10:00"),
                    IsHidden = false,
                },
            };

            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(), // Filter by startDate
                        It.IsAny<Expression<Func<Schedule, object>>>(), // Filter by endDate
                        true // Reflecting GetAll = true
                    )
                )
                .ReturnsAsync((mockSchedules.Count, mockSchedules));

            // Mock dependent repositories
            _studentProfileRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(
                    new StudentProfile
                    {
                        Id = studentProfileId,
                        Status = SD.StudentProfileStatus.Teaching,
                    }
                );

            _childInfoRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new ChildInformation { Id = 1 });

            _syllabusRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new Syllabus { Id = 1 });

            // Act
            var result = await _controller.GetAllAsync(
                studentProfileId,
                startDate,
                endDate,
                getAll
            );

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Result.Should().NotBeNull();

            var model = response.Result as ListScheduleDTO;
            model.Should().NotBeNull();
            model.Schedules.Should().HaveCount(mockSchedules.Count); // All schedules are returned since GetAll is true
            model.MaxDate.Should().Be(mockSchedules.Max(x => x.ScheduleDate.Date)); // MaxDate should reflect the latest schedule
        }

        [Fact]
        public async Task GetAllAsync_StartDateNullEndDateNotNullAndGetAllWithStudentProfileIdZero_ReturnsSchedules()
        {
            // Arrange
            var studentProfileId = 0; // Invalid student profile ID
            DateTime? startDate = null;
            var endDate = DateTime.Now.AddDays(7); // Set an end date in the future
            bool getAll = true;

            var tutorId = "12345";

            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, tutorId) })
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Mock repository to return schedules for a specific TutorId
            var mockSchedules = new List<Schedule>
            {
                new Schedule
                {
                    Id = 1,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now,
                    Start = TimeSpan.Parse("10:00"),
                    End = TimeSpan.Parse("11:00"),
                    IsHidden = false,
                },
                new Schedule
                {
                    Id = 2,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now.AddDays(1),
                    Start = TimeSpan.Parse("14:00"),
                    End = TimeSpan.Parse("15:00"),
                    IsHidden = false,
                },
            };

            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(), // Filter by startDate
                        It.IsAny<Expression<Func<Schedule, object>>>(),
                        true
                    )
                )
                .ReturnsAsync((mockSchedules.Count, mockSchedules));

            // Mock dependent repositories
            _studentProfileRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(
                    new StudentProfile
                    {
                        Id = studentProfileId,
                        Status = SD.StudentProfileStatus.Teaching,
                    }
                );

            _childInfoRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new ChildInformation { Id = 1 });

            _syllabusRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new Syllabus { Id = 1 });

            // Act
            var result = await _controller.GetAllAsync(
                studentProfileId,
                startDate,
                endDate,
                getAll
            );

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Result.Should().NotBeNull();

            var model = response.Result as ListScheduleDTO;
            model.Should().NotBeNull();
            model.Schedules.Should().HaveCount(mockSchedules.Count);
            model.MaxDate.Should().Be(mockSchedules.Max(x => x.ScheduleDate.Date));
        }

        [Fact]
        public async Task GetAllAsync_StartDateNullEndDateNullAndGetAllWithStudentProfileIdZero_ReturnsSchedules()
        {
            // Arrange
            var studentProfileId = 0; // Invalid student profile ID
            DateTime? startDate = null;
            DateTime? endDate = null;
            bool getAll = true;

            var tutorId = "12345";

            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, tutorId) })
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Mock repository to return schedules for the tutor
            var mockSchedules = new List<Schedule>
            {
                new Schedule
                {
                    Id = 1,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now,
                    Start = TimeSpan.Parse("10:00"),
                    End = TimeSpan.Parse("11:00"),
                    IsHidden = false,
                },
                new Schedule
                {
                    Id = 2,
                    StudentProfileId = studentProfileId,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now.AddDays(1),
                    Start = TimeSpan.Parse("14:00"),
                    End = TimeSpan.Parse("15:00"),
                    IsHidden = false,
                },
            };

            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(), // Filter by startDate
                        It.IsAny<Expression<Func<Schedule, object>>>(),
                        true
                    )
                )
                .ReturnsAsync((mockSchedules.Count, mockSchedules));

            // Mock dependent repositories
            _studentProfileRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(
                    new StudentProfile
                    {
                        Id = studentProfileId,
                        Status = SD.StudentProfileStatus.Teaching,
                    }
                );

            _childInfoRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new ChildInformation { Id = 1 });

            _syllabusRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new Syllabus { Id = 1 });

            // Act
            var result = await _controller.GetAllAsync(
                studentProfileId,
                startDate,
                endDate,
                getAll
            );

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Result.Should().NotBeNull();

            var model = response.Result as ListScheduleDTO;
            model.Should().NotBeNull();
            model.Schedules.Should().HaveCount(mockSchedules.Count);
            model.MaxDate.Should().Be(mockSchedules.Max(x => x.ScheduleDate.Date));
        }

        [Fact]
        public async Task GetAllAsync_StartDateNotNullEndDateNullAndGetAllFalseWithStudentProfileIdZero_ReturnsNoSchedules()
        {
            // Arrange
            var studentProfileId = 0; // Invalid student profile ID
            var startDate = DateTime.Now.AddDays(-7); // Some valid start date
            DateTime? endDate = null; // End date is null
            bool getAll = false; // GetAll is false, so only schedules in the date range should be returned

            var tutorId = "12345";

            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, tutorId) })
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Mock repository to return an empty list of schedules for the invalid student profile
            var mockSchedules = new List<Schedule>(); // No schedules for invalid student profile

            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(), // Filter by startDate
                        It.IsAny<Expression<Func<Schedule, object>>>(),
                        true
                    )
                )
                .ReturnsAsync((mockSchedules.Count, mockSchedules));

            // Mock dependent repositories
            _studentProfileRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((StudentProfile)null); // Return null since studentProfileId is 0

            _childInfoRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new ChildInformation { Id = 1 });

            _syllabusRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new Syllabus { Id = 1 });

            // Act
            var result = await _controller.GetAllAsync(
                studentProfileId,
                startDate,
                endDate,
                getAll
            );

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Result.Should().NotBeNull();

            var model = response.Result as ListScheduleDTO;
            model.Should().NotBeNull();
            model.Schedules.Should().BeEmpty(); // Since there are no schedules for studentProfileId 0
        }

        [Fact]
        public async Task GetAllAsync_StartDateNotNullEndDateNotNullAndGetAllFalseWithStudentProfileIdZero_ReturnsNoSchedules()
        {
            // Arrange
            var studentProfileId = 0; // Invalid student profile ID
            var startDate = DateTime.Now.AddDays(-7); // Some valid start date
            var endDate = DateTime.Now; // Some valid end date
            bool getAll = false; // GetAll is false, so only schedules in the date range should be returned

            var tutorId = "12345";

            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, tutorId) })
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Mock repository to return an empty list of schedules for the invalid student profile
            var mockSchedules = new List<Schedule>(); // No schedules for invalid student profile

            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(), // Filter by startDate
                        It.IsAny<Expression<Func<Schedule, object>>>(),
                        true
                    )
                )
                .ReturnsAsync((mockSchedules.Count, mockSchedules));

            // Mock dependent repositories
            _studentProfileRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((StudentProfile)null); // Return null since studentProfileId is 0

            _childInfoRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new ChildInformation { Id = 1 });

            _syllabusRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new Syllabus { Id = 1 });

            // Act
            var result = await _controller.GetAllAsync(
                studentProfileId,
                startDate,
                endDate,
                getAll
            );

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Result.Should().NotBeNull();

            var model = response.Result as ListScheduleDTO;
            model.Should().NotBeNull();
            model.Schedules.Should().BeEmpty(); // Since there are no schedules for studentProfileId 0
        }

        [Fact]
        public async Task GetAllAsync_StartDateNullEndDateNotNullAndGetAllFalseWithStudentProfileIdZero_ReturnsNoSchedules()
        {
            // Arrange
            var studentProfileId = 0; // Invalid student profile ID
            DateTime? startDate = null; // StartDate is null
            var endDate = DateTime.Now; // EndDate is not null
            bool getAll = false; // GetAll is false, so only schedules within the date range should be returned

            var tutorId = "12345";

            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, tutorId) })
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Mock repository to return an empty list of schedules for the invalid student profile
            var mockSchedules = new List<Schedule>(); // No schedules for invalid student profile

            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(), // Filter by startDate
                        It.IsAny<Expression<Func<Schedule, object>>>(),
                        true
                    )
                )
                .ReturnsAsync((mockSchedules.Count, mockSchedules));

            // Mock dependent repositories
            _studentProfileRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((StudentProfile)null); // Return null since studentProfileId is 0

            _childInfoRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new ChildInformation { Id = 1 });

            _syllabusRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new Syllabus { Id = 1 });

            // Act
            var result = await _controller.GetAllAsync(
                studentProfileId,
                startDate,
                endDate,
                getAll
            );

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Result.Should().NotBeNull();

            var model = response.Result as ListScheduleDTO;
            model.Should().NotBeNull();
            model.Schedules.Should().BeEmpty(); // Since there are no schedules for studentProfileId 0
        }

        [Fact]
        public async Task GetAllAsync_StartDateNullEndDateNullAndGetAllFalseWithStudentProfileIdZero_ReturnsNoSchedules()
        {
            // Arrange
            var studentProfileId = 0; // Invalid student profile ID
            DateTime? startDate = null; // StartDate is null
            DateTime? endDate = null; // EndDate is null
            bool getAll = false; // GetAll is false, so only schedules within the date range should be returned

            var tutorId = "12345";

            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, tutorId) })
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Mock repository to return an empty list of schedules for the invalid student profile
            var mockSchedules = new List<Schedule>(); // No schedules for invalid student profile

            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(), // Filter by startDate
                        It.IsAny<Expression<Func<Schedule, object>>>(),
                        true
                    )
                )
                .ReturnsAsync((mockSchedules.Count, mockSchedules));

            // Mock dependent repositories
            _studentProfileRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((StudentProfile)null); // Return null since studentProfileId is 0

            _childInfoRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new ChildInformation { Id = 1 });

            _syllabusRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new Syllabus { Id = 1 });

            // Act
            var result = await _controller.GetAllAsync(
                studentProfileId,
                startDate,
                endDate,
                getAll
            );

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Result.Should().NotBeNull();

            var model = response.Result as ListScheduleDTO;
            model.Should().NotBeNull();
            model.Schedules.Should().BeEmpty(); // Since there are no schedules for studentProfileId 0
        }

        [Fact]
        public async Task ChangeScheduleDateTime_AuthenticatedUserWithInvalidRole_ReturnsForbidden()
        {
            // Arrange
            var updateDTO = new ScheduleDateTimeUpdateDTO
            {
                Id = 1,
                ScheduleDate = DateTime.Now.AddDays(1),
                Start = TimeSpan.Parse("10:00"),
                End = TimeSpan.Parse("11:00"),
            };
            _resourceServiceMock
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbiden access.");
            var userId = "12345"; // Simulated valid user ID
            var invalidRoles = new List<string> { "STUDENT_ROLE" }; // Invalid role (not TUTOR_ROLE)

            // Mock claims principal to simulate an authenticated user with an invalid role
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId),
                        new Claim(ClaimTypes.Role, "STUDENT_ROLE"),
                    }
                )
            );

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Act
            var result = await _controller.ChangeScheduleDateTime(updateDTO);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("Forbiden access.");
        }

        [Fact]
        public async Task ChangeScheduleDateTime_UnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            var updateDTO = new ScheduleDateTimeUpdateDTO
            {
                Id = 1,
                ScheduleDate = DateTime.Now.AddDays(1),
                Start = TimeSpan.Parse("10:00"),
                End = TimeSpan.Parse("11:00"),
            };

            // Mock claims principal to simulate an unauthenticated user (missing NameIdentifier claim)
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity() // No NameIdentifier claim here to simulate unauthenticated user
            );
            _resourceServiceMock
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Act
            var result = await _controller.ChangeScheduleDateTime(updateDTO);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("Unauthorized access.");
        }

        [Fact]
        public async Task ChangeScheduleDateTime_InternalServerError_ReturnsInternalServerError()
        {
            // Arrange
            var updateDTO = new ScheduleDateTimeUpdateDTO
            {
                Id = 1,
                ScheduleDate = DateTime.Now.AddDays(1),
                Start = TimeSpan.Parse("10:00"),
                End = TimeSpan.Parse("11:00"),
            };

            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "tutorId"),
                        new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Simulate a repository or service failure by setting up the repository mock to throw an exception
            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ThrowsAsync(new Exception("Database connection error"));

            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("An internal server error occurred.");

            // Act
            var result = await _controller.ChangeScheduleDateTime(updateDTO);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("An internal server error occurred.");
        }

        [Fact]
        public async Task ChangeScheduleDateTime_InvalidId_ReturnsBadRequest()
        {
            // Arrange
            var updateDTO = new ScheduleDateTimeUpdateDTO
            {
                Id = 0, // Invalid Id to trigger BadRequest
                ScheduleDate = DateTime.Now.AddDays(1),
                Start = TimeSpan.Parse("10:00"),
                End = TimeSpan.Parse("11:00"),
            };

            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "tutorId"),
                        new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Set up the mock for ResourceService to return the error message for BadRequest
            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Invalid ID provided.");

            // Act
            var result = await _controller.ChangeScheduleDateTime(updateDTO);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("Invalid ID provided.");
        }

        [Fact]
        public async Task ChangeScheduleDateTime_NullOrInvalidModel_ReturnsBadRequest()
        {
            // Arrange
            ScheduleDateTimeUpdateDTO updateDTO = new ScheduleDateTimeUpdateDTO
            {
                Id = 3, // Invalid Id to trigger BadRequest
                ScheduleDate = DateTime.Now.AddDays(1),
                Start = TimeSpan.Parse("10:00"),
                End = TimeSpan.Parse("11:00"),
            }; // Simulate a null DTO to trigger BadRequest

            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "tutorId"),
                        new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Set up the mock for ResourceService to return the error message for BadRequest
            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.SCHEDULE))
                .Returns("Invalid schedule data provided.");

            // Manually invalidate ModelState to simulate an invalid model
            _controller.ModelState.AddModelError("ScheduleDate", "ScheduleDate is required.");

            // Act
            var result = await _controller.ChangeScheduleDateTime(updateDTO);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("Invalid schedule data provided.");
        }

        [Fact]
        public async Task ChangeScheduleDateTime_ScheduleNotFound_ReturnsNotFound()
        {
            // Arrange
            var updateDTO = new ScheduleDateTimeUpdateDTO
            {
                Id = 1, // Use a valid ID that will not be found in the database
                ScheduleDate = DateTime.Now.AddDays(1),
                Start = TimeSpan.Parse("10:00"),
                End = TimeSpan.Parse("11:00"),
            };

            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "tutorId"),
                        new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Set up the mock for ResourceService to return the error message for NotFound
            _resourceServiceMock
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.SCHEDULE))
                .Returns("Schedule not found.");

            // Simulate that the schedule is not found by setting up the repository mock to return null
            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((Schedule)null); // Simulate that no schedule is found

            // Act
            var result = await _controller.ChangeScheduleDateTime(updateDTO);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("Schedule not found.");
        }

        [Fact]
        public async Task ChangeScheduleDateTime_ValidUpdate_ReturnsOk()
        {
            // Arrange
            var updateDTO = new ScheduleDateTimeUpdateDTO
            {
                Id = 1, // Valid Id that exists in the database
                ScheduleDate = DateTime.Now.AddDays(1),
                Start = TimeSpan.Parse("10:00"),
                End = TimeSpan.Parse("11:00"),
            };

            // Mock claims principal to simulate an authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "tutorId"),
                        new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
                    }
                )
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Set up the mock for ResourceService to return the success message for valid update

            // Set up the mock to simulate a valid schedule found in the repository
            var existingSchedule = new Schedule
            {
                Id = 1,
                TutorId = "tutorId",
                ScheduleDate = DateTime.Now.AddDays(1),
                Start = TimeSpan.Parse("09:00"),
                End = TimeSpan.Parse("10:00"),
            };

            _scheduleRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(existingSchedule); // Simulate finding the schedule in the repository

            _scheduleRepositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<Schedule>()))
                .ReturnsAsync(existingSchedule); // Simulate success
        }

        [Fact]
        public async Task GetAllAssignedSchedule_InvalidAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var studentProfileId = 1;
            var sort = SD.ORDER_DESC; // Example sort order
            var pageNumber = 1;

            // Mock claims principal to simulate a user without the `tutorId` claim (simulating invalid authentication)
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, SD.TUTOR_ROLE) }) // Missing tutorId claim
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Set up the mock for ResourceService to return the unauthorized message
            _resourceServiceMock
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("You are not authorized to access this resource.");

            // Act
            var result = await _controller.GetAllAssignedSchedule(studentProfileId, sort, pageNumber);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("You are not authorized to access this resource.");
        }


        [Theory]
        [InlineData(SD.ORDER_ASC)]  // Test for ascending order
        [InlineData(SD.ORDER_DESC)] // Test for descending order
        public async Task GetAllAssignedSchedule_ValidTutorAndNoStudentProfile_ReturnsOkWithSchedules(string sort)
        {
            // Arrange
            int studentProfileId = 0; // No specific student profile is provided
            int pageNumber = 1; // First page of results

            var tutorId = "validTutorId"; // Simulate a valid tutor ID
            var scheduleList = new List<Schedule>
            {
                new Schedule { Id = 1, TutorId = tutorId, ScheduleDate = DateTime.Now, Start = DateTime.Now.TimeOfDay, IsHidden = false, ExerciseId = 1 },
                new Schedule { Id = 2, TutorId = tutorId, ScheduleDate = DateTime.Now.AddDays(1), Start = DateTime.Now.AddDays(1).TimeOfDay, IsHidden = false, ExerciseId = 2 }
            };

            var mockSchedules = new List<Schedule>
            {
                new Schedule
                {
                    Id = 1,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now,
                    Start = TimeSpan.FromHours(10),
                    IsHidden = false,
                    ExerciseId = 1,
                    StudentProfileId = 1
                },
                new Schedule
                {
                    Id = 2,
                    TutorId = tutorId,
                    ScheduleDate = DateTime.Now.AddDays(1),
                    Start = TimeSpan.FromHours(11),
                    IsHidden = false,
                    ExerciseId = 2,
                    StudentProfileId = 2
                }
            };


            // Mock data setup for repository
            _scheduleRepositoryMock
                .Setup(repo => repo.GetAllWithIncludeAsync(It.IsAny<Expression<Func<Schedule, bool>>>(),
                                                           It.IsAny<string>(),
                                                           It.IsAny<int>(),
                                                           It.IsAny<int>(),
                                                           It.IsAny<Expression<Func<Schedule, object>>>(),
                                                           It.IsAny<bool>()))
                .ReturnsAsync((scheduleList.Count, scheduleList));

            _scheduleRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Schedule, bool>>>(),
                                                           It.IsAny<string>(),
                                                           It.IsAny<string>(),
                                                           It.IsAny<Expression<Func<Schedule, object>>>(),
                                                           It.IsAny<bool>()))
                .ReturnsAsync((mockSchedules.Count, mockSchedules));

            // Mock data for related entities
            _studentProfileRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<StudentProfile, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new StudentProfile { Id = 1, ChildId = 1 });

            _childInfoRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ChildInformation, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ChildInformation { Id = 1 });

            _syllabusRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<Syllabus, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Syllabus { Id = 1 });

            // Mock ResourceService
            _resourceServiceMock
                .Setup(rs => rs.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("You are not authorized to access this resource.");

            // Set up claims principal for the authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, tutorId), new Claim(ClaimTypes.Role, SD.TUTOR_ROLE) })
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
            scheduleList = scheduleList.OrderBy(x => x.ScheduleDate.Date).ThenBy(x => x.Start).ToList();
            // Act
            var result = await _controller.GetAllAssignedSchedule(studentProfileId, sort, pageNumber);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Result.Should().NotBeNull();
            response.Pagination.Should().NotBeNull();

            // Check that the model contains the schedules and pagination
            var model = response.Result as ListScheduleDTO;
            model.Schedules.Should().HaveCount(2);
            model.MaxDate.Should().Be(scheduleList.Max(s => s.ScheduleDate).Date);

            // Verify that the schedules are ordered according to the provided sort order
            model.Schedules[0].ScheduleDate.Should().BeBefore(model.Schedules[1].ScheduleDate);
            
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]  // Test for ascending order
        [InlineData(SD.ORDER_DESC)] // Test for descending order
        public async Task GetAllAssignedSchedule_ValidStudentProfileId_ReturnsOkWithSchedules(string sort)
        {
            // Arrange
            int studentProfileId = 1; // Specific student profile is provided
            int pageNumber = 1; // First page of results

            var tutorId = "validTutorId"; // Simulate a valid tutor ID

            // List of schedules for a specific student profile
            var scheduleList = new List<Schedule>
    {
        new Schedule
        {
            Id = 1,
            TutorId = tutorId,
            ScheduleDate = DateTime.Now,
            Start = TimeSpan.FromHours(10),
            IsHidden = false,
            ExerciseId = 1,
            StudentProfileId = studentProfileId
        },
        new Schedule
        {
            Id = 2,
            TutorId = tutorId,
            ScheduleDate = DateTime.Now.AddDays(1),
            Start = TimeSpan.FromHours(11),
            IsHidden = false,
            ExerciseId = 2,
            StudentProfileId = studentProfileId
        }
    };
            scheduleList = scheduleList.OrderBy(x => x.ScheduleDate.Date).ThenBy(x => x.Start).ToList();

            // Mock data setup for repository
            _scheduleRepositoryMock
                .Setup(repo => repo.GetAllWithIncludeAsync(It.IsAny<Expression<Func<Schedule, bool>>>(),
                                                           It.IsAny<string>(),
                                                           It.IsAny<int>(),
                                                           It.IsAny<int>(),
                                                           It.IsAny<Expression<Func<Schedule, object>>>(),
                                                           It.IsAny<bool>()))
                .ReturnsAsync((scheduleList.Count, scheduleList));

            _scheduleRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Schedule, bool>>>(),
                                                           It.IsAny<string>(),
                                                           It.IsAny<string>(),
                                                           It.IsAny<Expression<Func<Schedule, object>>>(),
                                                           It.IsAny<bool>()))
                .ReturnsAsync((scheduleList.Count, scheduleList));

            // Mock data for related entities
            _studentProfileRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<StudentProfile, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new StudentProfile { Id = studentProfileId, ChildId = 1 });

            _childInfoRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ChildInformation, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ChildInformation { Id = 1 });

            _syllabusRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<Syllabus, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Syllabus { Id = 1 });

            // Mock ResourceService
            _resourceServiceMock
                .Setup(rs => rs.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("You are not authorized to access this resource.");

            // Set up claims principal for the authenticated user
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, tutorId), new Claim(ClaimTypes.Role, SD.TUTOR_ROLE) })
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.GetAllAssignedSchedule(studentProfileId, sort, pageNumber);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Result.Should().NotBeNull();
            response.Pagination.Should().NotBeNull();

            // Check that the model contains the schedules and pagination
            var model = response.Result as ListScheduleDTO;
            model.Schedules.Should().HaveCount(2);
            model.MaxDate.Should().Be(scheduleList.Max(s => s.ScheduleDate).Date);

            // Verify that the schedules are ordered according to the provided sort order
            
            model.Schedules[0].ScheduleDate.Should().BeBefore(model.Schedules[1].ScheduleDate);
            
        }

    }
}
