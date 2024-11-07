using AutoMapper;
using backend_api.Mapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Repository.IRepository;
using backend_api.Services.IServices;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace backend_api.Controllers.v1.Tests
{
    public class AvailableTimeControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {

        private readonly WebApplicationFactory<Program> _factory;
        private readonly Mock<IAvailableTimeSlotRepository> _availableTimeSlotRepositoryMock;
        private readonly IMapper _mapper;
        private readonly Mock<IResourceService> _resourceServiceMock;
        private readonly Mock<ILogger<AvailableTimeController>> _loggerMock;
        private readonly AvailableTimeController _controller;
        private readonly APIResponse _response;

        public AvailableTimeControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
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


        //[Fact]
        //public async Task CreateAsync_ReturnsUnauthorized_WhenUserIsNotInStaffRole()
        //{
        //    var client = _factory.CreateClient();
        //    var dto = new AvailableTimeSlotCreateDTO { From = "1", To = "2", Weekday = 1 };
        //    var content = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
        //    var result = await client.PostAsync("/api/v1/assessment", content);
        //    result.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        //}

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


        [Fact]
        public async Task CreateAsync_ReturnsBadRequest_WhenFromTimeIsAfterToTime()
        {
            // Arrange
            var dto = new AvailableTimeSlotCreateDTO {Weekday = 2, From = "14:00", To = "12:00" };
            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.TIME_SLOT))
                .Returns("'Thời gian rảnh' không hợp lệ.");

            // Act
            var result = await _controller.CreateAsync(dto);
            var statusCodeResult = result.Result as BadRequestObjectResult;

            // Assert
            statusCodeResult.Should().NotBeNull();
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = statusCodeResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.First().Should().Be("'Thời gian rảnh' không hợp lệ.");
        }

        [Fact]
        public async Task CreateAsync_ReturnsBadRequest_WhenOverlappingTimeSlotExists()
        {
            // Arrange
            var dto = new AvailableTimeSlotCreateDTO { From = "10:00", To = "12:00" };
            var existingSlots = new List<AvailableTimeSlot>
        {
            new AvailableTimeSlot { From = TimeSpan.Parse("09:00"), To = TimeSpan.Parse("11:00") }
        };

            _resourceServiceMock
                .Setup(r => r.GetString(SD.TIMESLOT_DUPLICATED_MESSAGE, SD.TIME_SLOT, "09:00", "11:00"))
                .Returns("Khung giờ bị trùng với khung giờ đã tồn tại 09:00-11:00");

            _availableTimeSlotRepositoryMock
                .Setup(r => r.GetAllNotPagingAsync(It.IsAny<Expression<Func<AvailableTimeSlot, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<AvailableTimeSlot, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((existingSlots.Count, existingSlots));

            // Act
            var result = await _controller.CreateAsync(dto);
            var statusCodeResult = result.Result as BadRequestObjectResult;

            // Assert
            statusCodeResult.Should().NotBeNull();
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = statusCodeResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.First().Should().Be("Khung giờ bị trùng với khung giờ đã tồn tại 09:00-11:00");
        }

        [Fact]
        public async Task CreateAsync_ReturnsCreated_WhenValidRequest()
        {
            // Arrange
            var dto = new AvailableTimeSlotCreateDTO { From = "10:00", To = "12:00" };
            var availableTimeSlot = new AvailableTimeSlot { Id = 1, From = TimeSpan.Parse("10:00"), To = TimeSpan.Parse("12:00") };

            _availableTimeSlotRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<AvailableTimeSlot>())).ReturnsAsync(availableTimeSlot);

            // Act
            var result = await _controller.CreateAsync(dto);
            var statusCodeResult = result.Result as OkObjectResult;

            // Assert
            statusCodeResult.Should().NotBeNull();
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = statusCodeResult.Value as APIResponse;
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task CreateAsync_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            // Arrange
            var dto = new AvailableTimeSlotCreateDTO { From = "10:00", To = "12:00" };

            _availableTimeSlotRepositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<AvailableTimeSlot>()))
                .ThrowsAsync(new Exception("Lỗi hệ thống. Vui lòng thử lại sau!"));

            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Lỗi hệ thống. Vui lòng thử lại sau!");

            // Act
            var result = await _controller.CreateAsync(dto);
            var statusCodeResult = result.Result as ObjectResult;

            // Assert
            statusCodeResult.Should().NotBeNull();
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = statusCodeResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.First().Should().Be("Lỗi hệ thống. Vui lòng thử lại sau!");
        }


        [Fact]
        public async Task GetAllTimeSlotFromWeekday_ReturnsOk_WhenTimeSlotsExist()
        {
            // Arrange
            var tutorId = "12345";
            var weekday = 3; // For example, Wednesday
            var timeSlots = new List<AvailableTimeSlot>
            {
                new AvailableTimeSlot { Id = 1, TutorId = tutorId, Weekday = weekday, From = TimeSpan.Parse("08:00"), To = TimeSpan.Parse("10:00") },
                new AvailableTimeSlot { Id = 2, TutorId = tutorId, Weekday = weekday, From = TimeSpan.Parse("10:00"), To = TimeSpan.Parse("12:00") }
            };

            var expectedDtos = timeSlots.Select(ts => new AvailableTimeSlotDTO
            {
                TimeSlotId = ts.Id,
                TimeSlot = ts.From.ToString(@"hh\:mm") + "-" + ts.To.ToString(@"hh\:mm")
            }).ToList();

            _availableTimeSlotRepositoryMock
                .Setup(r => r.GetAllNotPagingAsync(It.IsAny<Expression<Func<AvailableTimeSlot, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<AvailableTimeSlot, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((timeSlots.Count, timeSlots));
            // Act
            var result = await _controller.GetAllTimeSlotFromWeekday(tutorId, weekday);
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.Result.Should().BeEquivalentTo(expectedDtos);
        }

        [Fact]
        public async Task GetAllTimeSlotFromWeekday_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            // Arrange
            var tutorId = "12345";
            var weekday = 3;

            _availableTimeSlotRepositoryMock
                .Setup(r => r.GetAllNotPagingAsync(It.IsAny<Expression<Func<AvailableTimeSlot, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<AvailableTimeSlot, object>>>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Lỗi hệ thống. Vui lòng thử lại sau!"));

            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Lỗi hệ thống. Vui lòng thử lại sau!");

            // Act
            var result = await _controller.GetAllTimeSlotFromWeekday(tutorId, weekday);
            var statusCodeResult = result.Result as ObjectResult;

            // Assert
            statusCodeResult.Should().NotBeNull();
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = statusCodeResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Lỗi hệ thống. Vui lòng thử lại sau!");
        }

        [Fact]
        public async Task RemoveTimeSlotFromWeekday_ReturnsNoContent_WhenTimeSlotExists()
        {
            // Arrange
            var timeSlotId = 1;
            var tutorId = "12345";
            var availableTimeSlot = new AvailableTimeSlot { Id = timeSlotId, TutorId = tutorId };

            // Set up the mock to return the timeslot when queried by ID and tutor ID
            _availableTimeSlotRepositoryMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AvailableTimeSlot, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(availableTimeSlot);

            // Set up RemoveAsync to complete without error
            _availableTimeSlotRepositoryMock
                .Setup(r => r.RemoveAsync(It.IsAny<AvailableTimeSlot>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RemoveTimeSlotFromWeekday(timeSlotId);
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task RemoveTimeSlotFromWeekday_ReturnsNotFound_WhenTimeSlotDoesNotExist()
        {
            // Arrange
            var timeSlotId = 1;
            var tutorId = "12345";

            // Set up the mock to return null when the timeslot is not found
            _availableTimeSlotRepositoryMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AvailableTimeSlot, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((AvailableTimeSlot)null);

            _resourceServiceMock
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.AVAILABLE_TIME))
                .Returns("Available time slot not found.");

            // Act
            var result = await _controller.RemoveTimeSlotFromWeekday(timeSlotId);
            var statusCodeResult = result.Result as ObjectResult;

            // Assert
            statusCodeResult.Should().NotBeNull();
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = statusCodeResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Available time slot not found.");
        }

        [Fact]
        public async Task RemoveTimeSlotFromWeekday_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            // Arrange
            var timeSlotId = 1;
            var tutorId = "12345";

            // Set up the mock to throw an exception when attempting to retrieve the timeslot
            _availableTimeSlotRepositoryMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AvailableTimeSlot, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Lỗi hệ thống. Vui lòng thử lại sau!"));

            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Lỗi hệ thống. Vui lòng thử lại sau!");

            // Act
            var result = await _controller.RemoveTimeSlotFromWeekday(timeSlotId);
            var statusCodeResult = result.Result as ObjectResult;

            // Assert
            statusCodeResult.Should().NotBeNull();
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = statusCodeResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Lỗi hệ thống. Vui lòng thử lại sau!");
        }

    }
}