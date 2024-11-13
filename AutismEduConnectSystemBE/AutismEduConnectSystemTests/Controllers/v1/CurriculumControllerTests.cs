using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutismEduConnectSystem;
using AutismEduConnectSystem.Controllers.v1;
using AutismEduConnectSystem.Mapper;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.RabbitMQSender;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.SignalR;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.Controllers.v1.Tests
{
    public class CurriculumControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new Mock<IUserRepository>();
        private readonly Mock<ITutorRepository> _tutorRepositoryMock = new Mock<ITutorRepository>();
        private readonly Mock<ICurriculumRepository> _curriculumRepositoryMock =
            new Mock<ICurriculumRepository>();
        private readonly Mock<IRabbitMQMessageSender> _messageBusMock =
            new Mock<IRabbitMQMessageSender>();
        private readonly Mock<IResourceService> _resourceServiceMock = new Mock<IResourceService>();
        private readonly Mock<ILogger<CurriculumController>> _loggerMock =
            new Mock<ILogger<CurriculumController>>();
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
            _configurationMock
                .Setup(config => config["RabbitMQSettings:QueueName"])
                .Returns("TestQueue");
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
                _loggerMock.Object, // Make sure the logger mock is also provided
                _curriculumRepositoryMock.Object,
                _messageBusMock.Object,
                _resourceServiceMock.Object,
                _notificationRepositoryMock.Object,
                _hubContextMock.Object
            );

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            new Claim[] { new Claim(ClaimTypes.NameIdentifier, "testUserId") }
                        )
                    ),
                },
            };
        }

        public Mock<IResourceService> ResourceServiceMock => _resourceServiceMock;

        [Fact]
        public async Task DeleteAsync_ReturnsBadRequest_WhenIdIsZero()
        {
            // Arrange
            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Invalid ID.");

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
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), false, null, null)
                )
                .ReturnsAsync((Curriculum)null);
            _resourceServiceMock
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.CURRICULUM))
                .Returns("Curriculum not found.");

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
            var curriculum = new Curriculum
            {
                Id = 1,
                SubmitterId = "testUserId",
                IsActive = true,
                IsDeleted = false,
            };
            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), false, null, null)
                )
                .ReturnsAsync(curriculum);
            var newCurriculum = new Curriculum
            {
                Id = 1,
                SubmitterId = "testUserId",
                IsActive = true,
                IsDeleted = true,
            };
            _curriculumRepositoryMock
                .Setup(repo => repo.UpdateAsync(It.IsAny<Curriculum>()))
                .ReturnsAsync(newCurriculum);

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
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), false, null, null)
                )
                .ThrowsAsync(new Exception());
            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("An error occurred.");

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
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
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
                    CreatedDate = DateTime.Now.AddDays(-1),
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
                    CreatedDate = DateTime.Now.AddDays(-1),
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
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending
                .ReturnsAsync(pagedResult);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_ALL,
                0,
                SD.AGE,
                SD.ORDER_DESC,
                1
            );
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
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter checks for APPROVE status and SubmitterId
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is ascending
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByAgeASCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
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
                    CreatedDate = DateTime.Now.AddDays(-1),
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
                    CreatedDate = DateTime.Now.AddDays(-1),
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
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_ALL,
                0,
                SD.AGE,
                SD.ORDER_ASC,
                1
            );
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
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status if applied
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateASCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
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
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
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
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
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
                    CreatedDate = DateTime.Now.AddDays(
                        -3
                    ) // Oldest date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_ALL,
                0,
                SD.CREATED_DATE,
                SD.ORDER_ASC,
                1
            );
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
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status if applied
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(expr =>
                            expr.Body.ToString().Contains("CreatedDate")
                        ),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateDESCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
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
                    CreatedDate = DateTime.Now.AddDays(
                        -3
                    ) // Oldest date
                    ,
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
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Middle date
                    ,
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
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newest date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort descending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_ALL,
                0,
                SD.CREATED_DATE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by CreatedDate
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status if applied
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(expr =>
                            expr.Body.ToString().Contains("CreatedDate")
                        ),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsFivePageNumberIsOneOrderByAgeASCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Reason reject",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_ALL,
                5,
                SD.AGE,
                SD.ORDER_ASC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for ascending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status if applied
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsFivePageNumberIsOneOrderByAgeDESCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Reason reject",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort descending by Age
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_ALL,
                5,
                SD.AGE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status if applied
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateDESCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -3
                    ) // Oldest date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Reason reject",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.Is<Expression<Func<Curriculum, object>>>(expr =>
                            expr.Body.ToString().Contains("CreatedDate")
                        ),
                        true
                    )
                ) // Sort descending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_ALL,
                5,
                SD.CREATED_DATE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by CreatedDate
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status if applied
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.Is<Expression<Func<Curriculum, object>>>(expr =>
                            expr.Body.ToString().Contains("CreatedDate")
                        ),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateASCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Middle date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -3
                    ) // Oldest date
                    ,
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Reason reject",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newest date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.Is<Expression<Func<Curriculum, object>>>(expr =>
                            expr.Body.ToString().Contains("CreatedDate")
                        ),
                        false
                    )
                ) // Sort ascending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_ALL,
                5,
                SD.CREATED_DATE,
                SD.ORDER_ASC,
                1
            );
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
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status if applied
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.Is<Expression<Func<Curriculum, object>>>(expr =>
                            expr.Body.ToString().Contains("CreatedDate")
                        ),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsFivePageNumberIsOneOrderByAgeASCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by Age
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_APPROVE,
                5,
                SD.AGE,
                SD.ORDER_ASC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for ascending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsFivePageNumberIsOneOrderByAgeDESCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort descending by Age
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_APPROVE,
                5,
                SD.AGE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateDESCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort descending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_APPROVE,
                5,
                SD.CREATED_DATE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by CreatedDate
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateASCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_APPROVE,
                5,
                SD.CREATED_DATE,
                SD.ORDER_ASC,
                1
            );
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
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateASCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
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
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(expr =>
                            expr.Body.ToString().Contains("CreatedDate")
                        ),
                        false
                    )
                ) // Sort ascending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_APPROVE,
                0,
                SD.CREATED_DATE,
                SD.ORDER_ASC,
                1
            );
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
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(expr =>
                            expr.Body.ToString().Contains("CreatedDate")
                        ),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateDESCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
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
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(expr =>
                            expr.Body.ToString().Contains("CreatedDate")
                        ),
                        false
                    )
                ) // Sort descending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_APPROVE,
                0,
                SD.CREATED_DATE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by CreatedDate
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(expr =>
                            expr.Body.ToString().Contains("CreatedDate")
                        ),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is descending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByAgeDESCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
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
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort descending by Age
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_APPROVE,
                0,
                SD.AGE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is descending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByAgeASCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
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
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by Age
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_APPROVE,
                0,
                SD.AGE,
                SD.ORDER_ASC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for ascending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByAgeASCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by Age
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_REJECT,
                0,
                SD.AGE,
                SD.ORDER_ASC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for ascending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for REJECT status
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByAgeDESCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort descending by Age
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_REJECT,
                0,
                SD.AGE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for REJECT status
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is descending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateDESCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort descending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_REJECT,
                0,
                SD.CREATED_DATE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by CreatedDate
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for REJECT status
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is descending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateASCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_REJECT,
                0,
                SD.CREATED_DATE,
                SD.ORDER_ASC,
                1
            );
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
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for REJECT status
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateASCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_REJECT,
                5,
                SD.CREATED_DATE,
                SD.ORDER_ASC,
                1
            );
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
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for REJECT status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateDESCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort descending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_REJECT,
                5,
                SD.CREATED_DATE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by CreatedDate
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for REJECT status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsFivePageNumberIsOneOrderByAgeDESCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort descending by Age
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_REJECT,
                5,
                SD.AGE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for REJECT status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsFivePageNumberIsOneOrderByAgeASCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort ascending by Age
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_REJECT,
                5,
                SD.AGE,
                SD.ORDER_ASC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for ascending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for REJECT status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is ascending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsFivePageNumberIsOneOrderByAgeASCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort ascending by Age
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_PENDING,
                5,
                SD.AGE,
                SD.ORDER_ASC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for ascending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is ascending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsFivePageNumberIsOneOrderByAgeDESCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort descending by Age
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_PENDING,
                5,
                SD.AGE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateASCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_PENDING,
                5,
                SD.CREATED_DATE,
                SD.ORDER_ASC,
                1
            );
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
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateDESCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort descending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_PENDING,
                5,
                SD.CREATED_DATE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by CreatedDate
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateDESCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort descending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_PENDING,
                0,
                SD.CREATED_DATE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by CreatedDate
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateASCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_PENDING,
                0,
                SD.CREATED_DATE,
                SD.ORDER_ASC,
                1
            );
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
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByAgeASCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by Age
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_PENDING,
                0,
                SD.AGE,
                SD.ORDER_ASC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for ascending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByAgeDESCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort descending by Age
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_PENDING,
                0,
                SD.AGE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is descending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateDESCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort descending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_ALL,
                0,
                SD.CREATED_DATE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by CreatedDate and All status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // No filter for status (All status)
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is descending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateASCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_ALL,
                0,
                SD.CREATED_DATE,
                SD.ORDER_ASC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for ascending sort by CreatedDate and All status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // No filter for status (All status)
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByAgeASCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by AgeFrom
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_ALL,
                0,
                SD.AGE_FROM,
                SD.ORDER_ASC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for ascending sort by AgeFrom and All status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // No filter for status (All status)
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by AgeFrom
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByAgeDESCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort descending by AgeFrom
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_ALL,
                0,
                SD.AGE_FROM,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by AgeFrom and All status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // No filter for status (All status)
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is descending by AgeFrom
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsFivePageNumberIsOneOrderByAgeASCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by AgeFrom
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_ALL,
                5,
                SD.AGE_FROM,
                SD.ORDER_ASC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for ascending sort by AgeFrom and All status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // No filter for status (All status)
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by AgeFrom
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsFivePageNumberIsOneOrderByAgeDESCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort descending by AgeFrom
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_ALL,
                5,
                SD.AGE_FROM,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by AgeFrom and All status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // No filter for status (All status)
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is descending by AgeFrom
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateDESCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort descending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_ALL,
                5,
                SD.CREATED_DATE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by CreatedDate and All status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // No filter for status (All status)
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateASCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort ascending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_ALL,
                5,
                SD.CREATED_DATE,
                SD.ORDER_ASC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for ascending sort by CreatedDate and All status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // No filter for status (All status)
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is ascending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateASCAndStatusIsRejected()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECTED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECTED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort ascending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_REJECT,
                5,
                SD.CREATED_DATE,
                SD.ORDER_ASC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for ascending sort by CreatedDate and Rejected status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for rejected status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is ascending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateDESCAndStatusIsRejected()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECTED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECTED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort descending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_REJECT,
                5,
                SD.CREATED_DATE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by CreatedDate and Rejected status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for rejected status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsFivePageNumberIsOneOrderByAgeDESCAndStatusIsRejected()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECTED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECTED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort descending by AgeFrom
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_REJECT,
                5,
                SD.AGE_FROM,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by AgeFrom and Rejected status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for rejected status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by AgeFrom
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsFivePageNumberIsOneOrderByAgeASCAndStatusIsRejected()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECTED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECTED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by AgeFrom
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_REJECT,
                5,
                SD.AGE_FROM,
                SD.ORDER_ASC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for ascending sort by AgeFrom and Rejected status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for rejected status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by AgeFrom
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateDESCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort descending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_REJECT,
                0,
                SD.CREATED_DATE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by CreatedDate
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for REJECT status
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateASCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_REJECT,
                0,
                SD.CREATED_DATE,
                SD.ORDER_ASC,
                1
            );
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
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for REJECT status
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByAgeDESCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            // Mock repository to return results sorted by Age in descending order
            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort descending by Age (AgeFrom or AgeEnd)
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_REJECT,
                0,
                SD.AGE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for REJECT status
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is descending by Age (AgeFrom or AgeEnd)
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByAgeASCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            // Mock repository to return results sorted by Age in ascending order
            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by Age (AgeFrom or AgeEnd)
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_REJECT,
                0,
                SD.AGE,
                SD.ORDER_ASC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for ascending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for REJECT status
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by Age (AgeFrom or AgeEnd)
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByAgeASCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            // Mock repository to return results sorted by Age in ascending order
            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by Age (AgeFrom or AgeEnd)
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_PENDING,
                0,
                SD.AGE,
                SD.ORDER_ASC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for ascending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by Age (AgeFrom or AgeEnd)
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByAgeDESCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            // Mock repository to return results sorted by Age in descending order
            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort descending by Age (AgeFrom or AgeEnd)
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_PENDING,
                0,
                SD.AGE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is descending by Age (AgeFrom or AgeEnd)
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateASCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            // Mock repository to return results sorted by CreatedDate in ascending order
            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_PENDING,
                0,
                SD.CREATED_DATE,
                SD.ORDER_ASC,
                1
            );
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
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateDESCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            // Mock repository to return results sorted by CreatedDate in descending order
            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort descending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_PENDING,
                0,
                SD.CREATED_DATE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by CreatedDate
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is descending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsFivePageNumberIsOneOrderByAgeASCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            // Mock repository to return results sorted by Age in ascending order
            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by Age
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_PENDING,
                5,
                SD.AGE,
                SD.ORDER_ASC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for ascending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsFivePageNumberIsOneOrderByAgeDESCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            // Mock repository to return results sorted by Age in descending order
            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort descending by Age
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_PENDING,
                5,
                SD.AGE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateASCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            // Mock repository to return results sorted by CreatedDate in ascending order
            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_PENDING,
                5,
                SD.CREATED_DATE,
                SD.ORDER_ASC,
                1
            );
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
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateDESCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            // Mock repository to return results sorted by CreatedDate in descending order
            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort descending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_PENDING,
                5,
                SD.CREATED_DATE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by CreatedDate
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateDESCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVE
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVE
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            // Mock repository to return results sorted by CreatedDate in descending order
            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort descending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_APPROVE,
                5,
                SD.CREATED_DATE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by CreatedDate
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateASCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVE
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVE
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            // Mock repository to return results sorted by CreatedDate in ascending order
            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by CreatedDate
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_APPROVE,
                5,
                SD.CREATED_DATE,
                SD.ORDER_ASC,
                1
            );
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
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsFivePageNumberIsOneOrderByAgeDESCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVE
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVE
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            // Mock repository to return results sorted by Age in descending order
            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort descending by Age
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_APPROVE,
                5,
                SD.AGE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithNoSearchPageSizeIsFivePageNumberIsOneOrderByAgeASCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVE
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVE
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            // Mock repository to return results sorted by Age in ascending order
            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by Age
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_APPROVE,
                5,
                SD.AGE,
                SD.ORDER_ASC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for ascending sort by Age
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByAgeASCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVE
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVE
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            // Mock repository to return results sorted by Age in ascending order
            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        "searchCurriculum", // Search term applied
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by Age
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_APPROVE,
                0,
                SD.AGE,
                SD.ORDER_ASC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for ascending sort by Age and with search term applied
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status
                        "Submitter",
                        "searchCurriculum", // Ensure search term is applied
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByAgeDESCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVE
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVE
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            // Mock repository to return results sorted by Age in descending order
            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        "searchCurriculum", // Search term applied
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort descending by Age (true for descending)
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_APPROVE,
                0,
                SD.AGE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by Age and with search term applied
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status
                        "Submitter",
                        "searchCurriculum", // Ensure search term is applied
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateDESCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVE
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVE
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            // Mock repository to return results sorted by CreatedDate in descending order
            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        "searchCurriculum", // Search term applied
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort descending by CreatedDate (true for descending)
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_APPROVE,
                0,
                SD.CREATED_DATE,
                SD.ORDER_DESC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for descending sort by CreatedDate and with search term applied
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status
                        "Submitter",
                        "searchCurriculum", // Ensure search term is applied
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffWithSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateASCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Set to STAFF_ROLE as user is staff
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVE
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -2
                    ) // Older date
                    ,
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE, // Set status to APPROVE
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    CreatedDate = DateTime.Now.AddDays(
                        -1
                    ) // Newer date
                    ,
                },
            };

            var pagedResult = (curricula.Count, curricula);

            // Mock repository to return results sorted by CreatedDate in ascending order
            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        "searchCurriculum", // Search term applied
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by CreatedDate (false for ascending)
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_APPROVE,
                0,
                SD.CREATED_DATE,
                SD.ORDER_ASC,
                1
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify repository method was called with correct parameters for ascending sort by CreatedDate and with search term applied
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVE status
                        "Submitter",
                        "searchCurriculum", // Ensure search term is applied
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by CreatedDate
        }
    }
}
