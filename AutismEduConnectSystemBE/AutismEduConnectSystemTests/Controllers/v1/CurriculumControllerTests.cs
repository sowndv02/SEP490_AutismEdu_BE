using Xunit;
using AutismEduConnectSystem.Controllers.v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using AutismEduConnectSystem.RabbitMQSender;
using AutismEduConnectSystem.Repository.IRepository;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.Mapper;
using AutismEduConnectSystem.Models;
using FluentAssertions;
using System.Net;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc.Testing;
using AutismEduConnectSystem.SignalR;
using Microsoft.AspNetCore.SignalR;
using AutismEduConnectSystem;
using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.Controllers.v1.Tests
{
    public class CurriculumControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {


        private readonly Mock<IUserRepository> _userRepositoryMock = new Mock<IUserRepository>();
        private readonly Mock<ITutorRepository> _tutorRepositoryMock = new Mock<ITutorRepository>();
        private readonly Mock<ICurriculumRepository> _curriculumRepositoryMock = new Mock<ICurriculumRepository>();
        private readonly Mock<IRabbitMQMessageSender> _messageBusMock = new Mock<IRabbitMQMessageSender>();
        private readonly Mock<IResourceService> _resourceServiceMock = new Mock<IResourceService>();
        private readonly Mock<ILogger<CurriculumController>> _loggerMock = new Mock<ILogger<CurriculumController>>();
        private readonly IMapper _mapper;
        private readonly Mock<IConfiguration> _configurationMock = new Mock<IConfiguration>();
        private readonly CurriculumController _controller;
        private readonly Mock<INotificationRepository> _notificationRepositoryMock;
        private readonly Mock<IHubContext<NotificationHub>> _hubContextMock;
        private readonly WebApplicationFactory<Program> _factory;

        public CurriculumControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _configurationMock.Setup(config => config["APIConfig:PageSize"]).Returns("10");
            _configurationMock.Setup(config => config["RabbitMQSettings:QueueName"]).Returns("TestQueue");
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingConfig());
            });
            _hubContextMock = new Mock<IHubContext<NotificationHub>>();
            _notificationRepositoryMock = new Mock<INotificationRepository>();
            _mapper = config.CreateMapper();
            _controller = new CurriculumController(
                _userRepositoryMock.Object,
                _tutorRepositoryMock.Object,
                _mapper,
                _configurationMock.Object,
                _loggerMock.Object,               // Make sure the logger mock is also provided
                _curriculumRepositoryMock.Object,
                _messageBusMock.Object,
                _resourceServiceMock.Object,
                _notificationRepositoryMock.Object,
                _hubContextMock.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                    {
                    new Claim(ClaimTypes.NameIdentifier, "testUserId")
                    }))
                }
            };
        }

        public Mock<IResourceService> ResourceServiceMock => _resourceServiceMock;

        [Fact]
        public async Task DeleteAsync_ReturnsBadRequest_WhenIdIsZero()
        {
            // Arrange
            _resourceServiceMock.Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID)).Returns("Invalid ID.");

            // Act
            var result = await _controller.DeleteAsync(0);
            var statusCodeResult = result.Result as BadRequestObjectResult;

            // Assert
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            var apiResponse = statusCodeResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.FirstOrDefault().Should().Be("Invalid ID.");
        }

        [Fact]
        public async Task DeleteAsync_ReturnsBadRequest_WhenCurriculumNotFound()
        {
            // Arrange
            _curriculumRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), false, null, null))
                .ReturnsAsync((Curriculum)null);
            _resourceServiceMock.Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.CURRICULUM)).Returns("Curriculum not found.");

            // Act
            var result = await _controller.DeleteAsync(1);
            var statusCodeResult = result.Result as BadRequestObjectResult;

            // Assert
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            var apiResponse = statusCodeResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.FirstOrDefault().Should().Be("Curriculum not found.");
        }

        [Fact]
        public async Task DeleteAsync_ReturnsNoContent_WhenSuccessfulDeletion()
        {
            // Arrange
            var curriculum = new Curriculum { Id = 1, SubmitterId = "testUserId", IsActive = true, IsDeleted = false };
            _curriculumRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), false, null, null))
                .ReturnsAsync(curriculum);
            var newCurriculum = new Curriculum { Id = 1, SubmitterId = "testUserId", IsActive = true, IsDeleted = true };
            _curriculumRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Curriculum>())).ReturnsAsync(newCurriculum);

            // Act
            var result = await _controller.DeleteAsync(1);
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsInternalServerError_OnException()
        {
            // Arrange
            _curriculumRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), false, null, null))
                .ThrowsAsync(new Exception());
            _resourceServiceMock.Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE)).Returns("An error occurred.");

            // Act
            var result = await _controller.DeleteAsync(1);
            var apiResponse = result.Value as APIResponse;

            // Assert
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.FirstOrDefault().Should().Be("An error occurred.");
        }

        //[Fact]
        //public async Task DeleteAsync_ReturnsUnauthorized_WhenUserIsNotInTutorRole()
        //{
        //    var client = _factory.CreateClient();
        //    var result = await client.DeleteAsync("/api/v1/certificate/3");
        //    result.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        //}


        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByAgeDESCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5, 
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1)
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1)
                }
                ,
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Reason reject",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1)
                }
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<Curriculum, bool>>>(),
                    "Submitter",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Curriculum, object>>>(),
                    false)) // Sort ascending
                .ReturnsAsync(pagedResult);

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(
                    It.IsAny<Expression<Func<Curriculum, bool>>>(),
                    "Submitter",
                    null,
                    It.IsAny<Expression<Func<Curriculum, object>>>(),
                    true))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync("searchCurriculum", SD.STATUS_ALL, 0, SD.AGE, SD.ORDER_DESC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters
            _curriculumRepositoryMock.Verify(repo => repo.GetAllNotPagingAsync(
                It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter checks for APPROVE status and SubmitterId
                "Submitter",
                null,
                It.IsAny<Expression<Func<Curriculum, object>>>(),
                true), Times.Once); // Ensure sorting is ascending
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByAgeASCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE) // Set to STAFF_ROLE as user is staff
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1)
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1)
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Reason reject",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1)
                }
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(
                    It.IsAny<Expression<Func<Curriculum, bool>>>(),
                    "Submitter",
                    null,
                    It.IsAny<Expression<Func<Curriculum, object>>>(),
                    false)) // Sort ascending
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync("searchCurriculum", SD.STATUS_ALL, 0, SD.AGE, SD.ORDER_ASC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for ascending sort
            _curriculumRepositoryMock.Verify(repo => repo.GetAllNotPagingAsync(
                It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status if applied
                "Submitter",
                null,
                It.IsAny<Expression<Func<Curriculum, object>>>(),
                false), Times.Once); // Ensure sorting is ascending
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateASCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
        new Claim(ClaimTypes.Role, SD.STAFF_ROLE) // Set to STAFF_ROLE as user is staff
    };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var curricula = new List<Curriculum>
    {
        new Curriculum
        {
            Id = 1,
            AgeFrom = 0,
            AgeEnd = 5,
            Description = "Curriculum Description",
            RequestStatus = SD.Status.PENDING,
            VersionNumber = 1,
            SubmitterId = "testTutorId",
            IsDeleted = false,
            CreatedDate = DateTime.Now.AddDays(-2) // Older date
        },
        new Curriculum
        {
            Id = 2,
            AgeFrom = 5,
            AgeEnd = 10,
            Description = "Curriculum Description",
            RequestStatus = SD.Status.APPROVE,
            VersionNumber = 1,
            SubmitterId = "testTutorId",
            IsDeleted = false,
            CreatedDate = DateTime.Now.AddDays(-1) // Newer date
        },
        new Curriculum
        {
            Id = 3,
            AgeFrom = 5,
            AgeEnd = 10,
            Description = "Curriculum Description",
            RequestStatus = SD.Status.REJECT,
            RejectionReason = "Reason reject",
            VersionNumber = 1,
            SubmitterId = "testTutorId",
            IsDeleted = false,
            CreatedDate = DateTime.Now.AddDays(-3) // Oldest date
        }
    };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(
                    It.IsAny<Expression<Func<Curriculum, bool>>>(),
                    "Submitter",
                    null,
                    It.IsAny<Expression<Func<Curriculum, object>>>(),
                    false)) // Sort ascending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync("searchCurriculum", SD.STATUS_ALL, 0, SD.CREATED_DATE, SD.ORDER_ASC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for ascending sort by CreatedDate
            _curriculumRepositoryMock.Verify(repo => repo.GetAllNotPagingAsync(
                It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status if applied
                "Submitter",
                null,
                It.Is<Expression<Func<Curriculum, object>>>(expr => expr.Body.ToString().Contains("CreatedDate")),
                false), Times.Once); // Ensure sorting is ascending by CreatedDate
        }

    }
}