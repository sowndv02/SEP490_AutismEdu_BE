using Xunit;
using AutismEduConnectSystem.Controllers.v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using AutismEduConnectSystem.Services.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using FluentAssertions;
using System.Net;
using System.Security.Claims;
using static Org.BouncyCastle.Math.EC.ECCurve;
using AutismEduConnectSystem.Mapper;
using System.Linq.Expressions;
using AutismEduConnectSystem.Models.DTOs;
using static AutismEduConnectSystem.SD;

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
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
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
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<Schedule, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
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
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<Schedule, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database error"));

            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");

            // Act
            var result = await _controller.GetByIdAsync(1);

            // Assert
            var internalServerErrorResult = result.Result as ObjectResult;
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

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
                StudentProfile = new StudentProfile() { Id = 1}
            };

            // Set up mock repository
            _scheduleRepositoryMock
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Schedule, bool>>>(),
                    false,
                    It.IsAny<string>(),
                    null))
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


    }
}