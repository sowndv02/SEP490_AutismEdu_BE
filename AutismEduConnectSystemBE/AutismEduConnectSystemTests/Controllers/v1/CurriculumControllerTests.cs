using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using AutismEduConnectSystem.Mapper;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Models.DTOs;
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
using Org.BouncyCastle.Tls;
using Xunit;
using AutismEduConnectSystem.Models.DTOs.UpdateDTOs;
using static AutismEduConnectSystem.SD;
using AutismEduConnectSystem.Repository;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutismEduConnectSystem.Controllers.v1.Tests
{
    public class CurriculumControllerTests
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

        public CurriculumControllerTests()
        {
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

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
            var submitterUser = new ApplicationUser { Id = "testTutorId", UserName = "testTutor" };

            _userRepositoryMock
               .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), false, null))
               .ReturnsAsync(submitterUser);
        }

        public Mock<IResourceService> ResourceServiceMock => _resourceServiceMock;


        [Fact]
        public async Task CreateAsync_ShouldReturnBadRequest_WhenDuplicateCurriculumExists()
        {
            // Arrange
            var existingCurriculums = new List<Curriculum>
            {
                new Curriculum { OriginalCurriculumId = 1, RequestStatus = SD.Status.PENDING }
            };
            var pagedResult = (existingCurriculums.Count, existingCurriculums);
            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), 
                        null, null, null, true
                    )
                )
                .ReturnsAsync(pagedResult);

            _resourceServiceMock
                .Setup(service => service.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.CURRICULUM))
                .Returns("Duplicate curriculum exists");

            var curriculumDto = new CurriculumCreateDTO { OriginalCurriculumId = 1, AgeEnd = 5, AgeFrom = 1, Description = "Update curriculum" };

            // Act
            var result = await _controller.CreateAsync(curriculumDto);
            var badRequestResult = result.Result as BadRequestObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.First().Should().Be("Duplicate curriculum exists");
        }

        [Fact]
        public async Task CreateAsync_ReturnsForbiden_WhenUserDoesNotHaveRequiredRole()
        {
            // Arrange
            _resourceServiceMock
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbidden access.");

            // Simulate a user with no valid claims (unauthorized)
            var claims = new List<Claim>
             {
                 new Claim(ClaimTypes.NameIdentifier, "testUserId")
             };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };



            var requestPayload = new CurriculumCreateDTO
            {
                AgeFrom = 3,
                AgeEnd = 10,
                Description = "Description",
                OriginalCurriculumId = 0
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
            apiResponse.ErrorMessages.First().Should().Be("Forbidden access.");
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
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };


            var requestPayload = new CurriculumCreateDTO
            {
                AgeFrom = 3,
                AgeEnd = 10,
                Description = "Description",
                OriginalCurriculumId = 0
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
            var statusCodeResult = result.Result as NotFoundObjectResult;

            // Assert
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
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
                    repo.GetAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), true, null, null)
                )
                .ReturnsAsync(curriculum);
            var newCurriculum = new Curriculum
            {
                Id = 1,
                SubmitterId = "testUserId",
                IsActive = false,
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
                    repo.GetAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), true, null, null)
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                        true
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
                        true
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                        true
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                        true
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                        true
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                        false
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                        true
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                        true
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                        true
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
            var submitterUser = new ApplicationUser { Id = "testTutorId", UserName = "testTutor" };
            var tutor = new Tutor() { TutorId = "testTutorId" };
            _userRepositoryMock
               .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), false, null))
               .ReturnsAsync(It.IsAny<ApplicationUser>());

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                        false
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                        false
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                        true
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
                        true
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                        true
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
                        true
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                        true
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
                        true
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffNoSearchPageSizeIsZeroPageNumberIsOneOrderByAgeASCAndStatusIsApprove()
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                        null, // Search term applied
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by Age
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
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
                        null, // Ensure search term is applied
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffNoSearchPageSizeIsZeroPageNumberIsOneOrderByAgeDESCAndStatusIsApprove()
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                        null, // Search term applied
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort descending by Age (true for descending)
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
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
                        null, // Ensure search term is applied
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by Age
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffNoSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateDESCAndStatusIsApprove()
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
            var tutor = new Tutor() { TutorId = "testTutorId" };
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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                        null, // Search term applied
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    )
                ) // Sort descending by CreatedDate (true for descending)
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
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
                        null, // Ensure search term is applied
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true
                    ),
                Times.Once
            ); // Ensure sorting is descending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsStaffNoSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateASCAndStatusIsApprove()
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
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
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
                        null, // Search term applied
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    )
                ) // Sort ascending by CreatedDate (false for ascending)
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                null,
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
                        null, // Ensure search term is applied
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false
                    ),
                Times.Once
            ); // Ensure sorting is ascending by CreatedDate
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateASCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
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
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Sort ascending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_ALL, // All status
                0, // PageSize is 0
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber
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
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Ensure sorting is ascending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateDESCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
            };

            var pagedResult = (curricula.Count, curricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Sort descending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_ALL, // All status
                0, // PageSize is 0
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_DESC, // Descending order
                1 // PageNumber
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
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Ensure sorting is descending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsZeroPageNumberIsOneOrderByAgeDESCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
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
                        true // Sort descending by AgeEnd
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_ALL, // All status
                0, // PageSize is 0
                SD.AGE, // Order by Age
                SD.ORDER_DESC, // Descending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for descending sort by AgeEnd
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Ensure sorting is descending by AgeEnd
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsFivePageNumberIsOneOrderByAgeASCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 8,
                    AgeEnd = 12,
                    Description = "Curriculum for Ages 8-12",
                    RequestStatus = SD.Status.REJECT,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var pagedResult = (curricula.Count, curricula.Take(5).ToList());

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Sort ascending by AgeEnd
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_ALL, // All status
                5, // PageSize is 5
                SD.AGE, // Order by AgeEnd
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for ascending sort by AgeEnd
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Ensure sorting is ascending by AgeEnd
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsFivePageNumberIsOneOrderByAgeDESCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 8,
                    AgeEnd = 12,
                    Description = "Curriculum for Ages 8-12",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.REJECT,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
            };

            var pagedResult = (
                curricula.Count,
                curricula.OrderByDescending(c => c.AgeEnd).Take(5).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Sort descending by AgeEnd
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_ALL, // All status
                5, // PageSize is 5
                SD.AGE, // Order by AgeEnd
                SD.ORDER_DESC, // Descending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for descending sort by AgeEnd
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Ensure sorting is descending by AgeEnd
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateDESCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.REJECT,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 8,
                    AgeEnd = 12,
                    Description = "Curriculum for Ages 8-12",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var pagedResult = (
                curricula.Count,
                curricula.OrderByDescending(c => c.CreatedDate).Take(5).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Sort descending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_ALL, // All status
                5, // PageSize is 5
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_DESC, // Descending order
                1 // PageNumber
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
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Ensure sorting is descending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateASCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.REJECT,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 8,
                    AgeEnd = 12,
                    Description = "Curriculum for Ages 8-12",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var pagedResult = (
                curricula.Count,
                curricula.OrderBy(c => c.CreatedDate).Take(5).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Sort ascending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_ALL, // All status
                5, // PageSize is 5
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber
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
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Ensure sorting is ascending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateASCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.REJECT,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 8,
                    AgeEnd = 12,
                    Description = "Curriculum for Ages 8-12",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Filter curricula to include only those with APPROVE status
            var approvedCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.APPROVE)
                .ToList();

            var pagedResult = (
                approvedCurricula.Count,
                approvedCurricula.OrderBy(c => c.CreatedDate).Take(5).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Sort ascending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_APPROVE, // Only APPROVE status
                5, // PageSize is 5
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for ascending sort by CreatedDate and APPROVE status filter
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Ensure sorting is ascending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateDESCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.REJECT,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 8,
                    AgeEnd = 12,
                    Description = "Curriculum for Ages 8-12",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var filteredCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.APPROVE)
                .ToList();
            var pagedResult = (
                filteredCurricula.Count,
                filteredCurricula.OrderByDescending(c => c.CreatedDate).Take(5).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Sort descending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_APPROVE, // Only APPROVE status
                5, // PageSize is 5
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_DESC, // Descending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for descending sort by CreatedDate and APPROVE status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Ensure sorting is descending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsFivePageNumberIsOneOrderByAgeDESCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.REJECT,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 8,
                    AgeEnd = 12,
                    Description = "Curriculum for Ages 8-12",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var filteredCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.APPROVE)
                .ToList();
            var pagedResult = (
                filteredCurricula.Count,
                filteredCurricula.OrderByDescending(c => c.AgeFrom).Take(5).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Sort descending by AgeFrom
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_APPROVE, // Only APPROVE status
                5, // PageSize is 5
                SD.AGE, // Order by Age
                SD.ORDER_DESC, // Descending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for descending sort by AgeFrom
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Ensure sorting is descending by AgeFrom
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsFivePageNumberIsOneOrderByAgeASCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 8,
                    AgeEnd = 12,
                    Description = "Curriculum for Ages 8-12",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var filteredCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.APPROVE)
                .ToList();
            var pagedResult = (
                filteredCurricula.Count,
                filteredCurricula.OrderBy(c => c.AgeFrom).Take(5).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Sort ascending by AgeFrom
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_APPROVE, // Only APPROVE status
                5, // PageSize is 5
                SD.AGE, // Order by Age
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for ascending sort by AgeFrom
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Ensure sorting is ascending by AgeFrom
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateASCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Filter curricula by APPROVE status
            var approvedCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.APPROVE)
                .ToList();
            var pagedResult = (
                approvedCurricula.Count,
                approvedCurricula.OrderBy(c => c.CreatedDate).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Sort ascending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_APPROVE, // Only APPROVE status
                0, // PageSize is 0 (no pagination)
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber (doesn't affect because PageSize is 0)
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull(); // No pagination because PageSize is 0

            // Verify repository method was called with correct parameters for ascending sort by CreatedDate
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Ensure sorting is ascending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateDESCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Filter curricula by APPROVE status
            var approvedCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.APPROVE)
                .ToList();
            var pagedResult = (
                approvedCurricula.Count,
                approvedCurricula.OrderByDescending(c => c.CreatedDate).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Sort descending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_APPROVE, // Only APPROVE status
                0, // PageSize is 0 (no pagination)
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_DESC, // Descending order
                1 // PageNumber (doesn't affect because PageSize is 0)
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull(); // No pagination because PageSize is 0

            // Verify repository method was called with correct parameters for descending sort by CreatedDate
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Ensure sorting is descending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsZeroPageNumberIsOneOrderByAgeDESCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var filteredCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.APPROVE)
                .ToList();
            var pagedResult = (
                filteredCurricula.Count,
                filteredCurricula.OrderByDescending(c => c.AgeFrom).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Sort descending by AgeFrom
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_APPROVE, // Only APPROVE status
                0, // PageSize is 0
                SD.AGE, // Order by Age
                SD.ORDER_DESC, // Descending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for descending sort by AgeFrom
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Ensure sorting is descending by AgeFrom
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsZeroPageNumberIsOneOrderByAgeASCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Filter out curricula with status APPROVE
            var filteredCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.APPROVE)
                .ToList();
            var pagedResult = (
                filteredCurricula.Count,
                filteredCurricula.OrderBy(c => c.AgeFrom).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Sort ascending by AgeFrom
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_APPROVE, // Only APPROVE status
                0, // PageSize is 0
                SD.AGE, // Order by Age
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for ascending sort by AgeFrom
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Ensure sorting is ascending by AgeFrom
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsZeroPageNumberIsOneOrderByAgeASCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.APPROVE,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Filter out curricula with status PENDING
            var filteredCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.PENDING)
                .ToList();
            var pagedResult = (
                filteredCurricula.Count,
                filteredCurricula.OrderBy(c => c.AgeFrom).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Sort ascending by AgeFrom
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_PENDING, // Only PENDING status
                0, // PageSize is 0
                SD.AGE, // Order by Age
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for ascending sort by AgeFrom
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Ensure sorting is ascending by AgeFrom
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsZeroPageNumberIsOneOrderByAgeDESCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var submitterUser = new ApplicationUser { Id = "testTutorId", UserName = "testTutor" };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            _userRepositoryMock
               .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), false, null))
               .ReturnsAsync(submitterUser);
            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.APPROVE, // Not included in the results since status is not PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Filter out curricula with status PENDING
            var filteredCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.PENDING)
                .ToList();
            var pagedResult = (
                filteredCurricula.Count,
                filteredCurricula.OrderByDescending(c => c.AgeFrom).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Sort descending by AgeFrom
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_PENDING, // Only PENDING status
                0, // PageSize is 0
                SD.AGE, // Order by Age
                SD.ORDER_DESC, // Descending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for descending sort by AgeFrom
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Ensure sorting is descending by AgeFrom
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateDESCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.APPROVE, // Not included in the results since status is not PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Filter out curricula with status PENDING
            var filteredCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.PENDING)
                .ToList();
            var pagedResult = (
                filteredCurricula.Count,
                filteredCurricula.OrderByDescending(c => c.CreatedDate).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Sort descending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_PENDING, // Only PENDING status
                0, // PageSize is 0
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_DESC, // Descending order
                1 // PageNumber
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
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Ensure sorting is descending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateASCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.APPROVE, // Not included in the results since status is not PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Filter out curricula with status PENDING
            var filteredCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.PENDING)
                .ToList();
            var pagedResult = (
                filteredCurricula.Count,
                filteredCurricula.OrderBy(c => c.CreatedDate).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Sort ascending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_PENDING, // Only PENDING status
                0, // PageSize is 0
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber
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
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Ensure sorting is ascending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateASCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.APPROVE, // Not included as the status is not PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
            };

            var pagedResult = (curricula.Count, curricula.OrderBy(c => c.CreatedDate).ToList());

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Sort ascending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_PENDING, // Only PENDING status
                5, // PageSize = 5
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber = 1
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
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Ensure sorting is ascending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsFivePageNumberIsOneOrderByAgeDESCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 4,
                    AgeFrom = 15,
                    AgeEnd = 20,
                    Description = "Curriculum for Ages 15-20",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-4),
                },
            };

            var pagedResult = (
                curricula.Count,
                curricula.OrderByDescending(c => c.AgeFrom).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Sort descending by AgeFrom
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_PENDING, // Only PENDING status
                5, // PageSize = 5
                SD.AGE, // Order by AgeFrom
                SD.ORDER_DESC, // Descending order
                1 // PageNumber = 1
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

            // Verify repository method was called with correct parameters for descending sort by AgeFrom
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Ensure sorting is descending by AgeFrom
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateDESCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.APPROVE, // Not included as the status is not PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
            };

            var pagedResult = (
                curricula.Count,
                curricula.OrderByDescending(c => c.CreatedDate).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Sort descending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_PENDING, // Only PENDING status
                5, // PageSize = 5
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_DESC, // Descending order
                1 // PageNumber = 1
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
                       It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Ensure sorting is descending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsFivePageNumberIsOneOrderByAgeASCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

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
                    Submitter = tutor,
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 4,
                    AgeFrom = 15,
                    AgeEnd = 20,
                    Description = "Curriculum for Ages 15-20",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-4),
                },
            };

            var pagedResult = (curricula.Count, curricula.OrderBy(c => c.AgeFrom).ToList());

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Sort ascending by AgeFrom
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_PENDING, // Only PENDING status
                5, // PageSize = 5
                SD.AGE, // Order by AgeFrom
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber = 1
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

            // Verify repository method was called with correct parameters for ascending sort by AgeFrom
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Ensure sorting is ascending by AgeFrom
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsFivePageNumberIsOneOrderByAgeASCAndStatusIsRejected()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECTED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 4,
                    AgeFrom = 15,
                    AgeEnd = 20,
                    Description = "Curriculum for Ages 15-20",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECTED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-4),
                },
            };

            var pagedResult = (curricula.Count, curricula.OrderBy(c => c.AgeFrom).ToList());

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Sort ascending by AgeFrom
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_REJECT, // Only REJECTED status
                5, // PageSize = 5
                SD.AGE, // Order by AgeFrom
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber = 1
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

            // Verify repository method was called with correct parameters for ascending sort by AgeFrom
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Ensure sorting is ascending by AgeFrom
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsFivePageNumberIsOneOrderByAgeDESCAndStatusIsRejected()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECTED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 4,
                    AgeFrom = 15,
                    AgeEnd = 20,
                    Description = "Curriculum for Ages 15-20",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECTED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-4),
                },
            };

            var pagedResult = (
                curricula.Count,
                curricula.OrderByDescending(c => c.AgeFrom).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Sort descending by AgeFrom
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_REJECT, // Only REJECTED status
                5, // PageSize = 5
                SD.AGE, // Order by AgeFrom
                SD.ORDER_DESC, // Descending order
                1 // PageNumber = 1
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

            // Verify repository method was called with correct parameters for descending sort by AgeFrom
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Ensure sorting is descending by AgeFrom
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateDESCAndStatusIsRejected()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECTED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 4,
                    AgeFrom = 15,
                    AgeEnd = 20,
                    Description = "Curriculum for Ages 15-20",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECTED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-4),
                },
            };

            var pagedResult = (
                curricula.Count,
                curricula.OrderByDescending(c => c.CreatedDate).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Order by CreatedDate
                        true // Sort descending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_REJECT, // Only REJECTED status
                5, // PageSize = 5
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_DESC, // Descending order
                1 // PageNumber = 1
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
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Ensure sorting is descending by CreatedDate
                        true // Ensure sorting is descending
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateASCAndStatusIsRejected()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECTED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 4,
                    AgeFrom = 15,
                    AgeEnd = 20,
                    Description = "Curriculum for Ages 15-20",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECTED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-4),
                },
            };

            var pagedResult = (curricula.Count, curricula.OrderBy(c => c.CreatedDate).ToList());

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Order by CreatedDate
                        false // Sort ascending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_REJECT, // Only REJECTED status
                5, // PageSize = 5
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber = 1
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
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        5, // PageSize
                        1, // PageNumber
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Ensure sorting is ascending by CreatedDate
                        false // Ensure sorting is ascending
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateASCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
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
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.REJECT, // Set status to REJECT
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 4,
                    AgeFrom = 15,
                    AgeEnd = 20,
                    Description = "Curriculum for Ages 15-20",
                    RequestStatus = SD.Status.PENDING, // Set status to PENDING (should not be included in result)
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-4),
                },
            };

            // Simulate fetching all curricula with REJECT status, ordered by CreatedDate ascending
            var pagedResult = (
                curricula.Count,
                curricula
                    .Where(c => c.RequestStatus == SD.Status.REJECT)
                    .OrderBy(c => c.CreatedDate)
                    .ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null, // No page size (page size 0 implies no pagination)
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Order by CreatedDate
                        false // Sort ascending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_REJECT, // Only REJECT status
                0, // PageSize = 0 (no pagination)
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber = 1 (not relevant when PageSize is 0, but keeping for consistency)
            );
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull(); // Since page size is 0, no pagination is included

            // Verify repository method was called with correct parameters for ascending sort by CreatedDate and REJECT status filter
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Ensure sorting is ascending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateDESCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var pagedResult = (
                curricula.Count,
                curricula.Where(c => c.RequestStatus == SD.Status.REJECT).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Sort descending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_REJECT, // Status set to REJECT
                0, // PageSize is 0
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_DESC, // Descending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for descending sort by CreatedDate and REJECT status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Ensure sorting is descending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsZeroPageNumberIsOneOrderByAgeDESCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Filter curricula to only include REJECT status
            var rejectedCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.REJECT)
                .ToList();
            var pagedResult = (
                rejectedCurricula.Count,
                rejectedCurricula.OrderByDescending(c => c.AgeFrom).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Sort descending by AgeFrom (Age)
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_REJECT, // Status set to REJECT
                0, // PageSize is 0
                SD.AGE, // Order by Age (AgeFrom)
                SD.ORDER_DESC, // Descending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for descending sort by Age (AgeFrom)
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Ensure sorting is descending by AgeFrom
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithSearchPageSizeIsZeroPageNumberIsOneOrderByAgeASCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Filter curricula to only include REJECT status
            var rejectedCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.REJECT)
                .ToList();
            var pagedResult = (
                rejectedCurricula.Count,
                rejectedCurricula.OrderBy(c => c.AgeFrom).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Sort ascending by Age (AgeFrom)
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_REJECT, // Status set to REJECT
                0, // PageSize is 0
                SD.AGE, // Order by Age (AgeFrom)
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for ascending sort by Age (AgeFrom)
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Ensure sorting is ascending by AgeFrom
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateASCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var pagedResult = (curricula.Count, curricula.OrderBy(c => c.CreatedDate).ToList());

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Sort ascending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_ALL, // All status
                0, // PageSize is 0 (no pagination)
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber
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
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Ensure sorting is ascending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateDESCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var pagedResult = (
                curricula.Count,
                curricula.OrderByDescending(c => c.CreatedDate).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Sort descending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_ALL, // All status
                0, // PageSize is 0 (no pagination)
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_DESC, // Descending order
                1 // PageNumber
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
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Ensure sorting is descending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByAgeDESCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var pagedResult = (
                curricula.Count,
                curricula.OrderByDescending(c => c.AgeFrom).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Sort descending by AgeFrom
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_ALL, // All status
                0, // PageSize is 0 (no pagination)
                SD.AGE, // Order by Age
                SD.ORDER_DESC, // Descending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for descending sort by AgeFrom
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Ensure sorting is descending by AgeFrom
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByAgeASCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var pagedResult = (curricula.Count, curricula.OrderBy(c => c.AgeFrom).ToList());

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Sort ascending by AgeFrom
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_ALL, // All status
                0, // PageSize is 0 (no pagination)
                SD.AGE, // Order by Age
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for ascending sort by AgeFrom
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Ensure sorting is ascending by AgeFrom
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByAgeASCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Filter out only the REJECT status curricula
            var filteredCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.REJECT)
                .ToList();
            var pagedResult = (
                filteredCurricula.Count,
                filteredCurricula.OrderBy(c => c.AgeFrom).ToList()
            );

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Sort ascending by AgeFrom
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_REJECT, // Only rejected status
                0, // PageSize is 0 (no pagination)
                SD.AGE, // Order by Age
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters:
            // - Filtering for "REJECT" status
            // - Sorting by AgeFrom in ascending order
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Ensure sorting is ascending by AgeFrom
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByAgeDESCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            var rejectedCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.REJECT)
                .OrderByDescending(c => c.AgeFrom)
                .ToList();
            var pagedResult = (rejectedCurricula.Count, rejectedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Sort descending by AgeFrom
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_REJECT, // Only REJECT status
                0, // PageSize is 0 (no pagination)
                SD.AGE, // Order by Age
                SD.ORDER_DESC, // Descending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for descending sort by AgeFrom and filtering by REJECT status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Ensure sorting is descending by AgeFrom
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateASCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Filter by REJECT status and order by CreatedDate ASC
            var rejectedCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.REJECT)
                .OrderBy(c => c.CreatedDate)
                .ToList();
            var pagedResult = (rejectedCurricula.Count, rejectedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Sort ascending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_REJECT, // Only REJECT status
                0, // PageSize is 0 (no pagination)
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for ascending sort by CreatedDate and filtering by REJECT status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Ensure sorting is ascending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateDESCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Filter by REJECT status and order by CreatedDate DESC
            var rejectedCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.REJECT)
                .OrderByDescending(c => c.CreatedDate)
                .ToList();
            var pagedResult = (rejectedCurricula.Count, rejectedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Sort descending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_REJECT, // Only REJECT status
                0, // PageSize is 0 (no pagination)
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_DESC, // Descending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for descending sort by CreatedDate and filtering by REJECT status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Ensure sorting is descending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateDESCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Filter by PENDING status and order by CreatedDate DESC
            var pendingCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.PENDING)
                .OrderByDescending(c => c.CreatedDate)
                .ToList();
            var pagedResult = (pendingCurricula.Count, pendingCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Sort descending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_PENDING, // Only PENDING status
                0, // PageSize is 0 (no pagination)
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_DESC, // Descending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for descending sort by CreatedDate and filtering by PENDING status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Ensure sorting is descending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateASCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Filter by PENDING status and order by CreatedDate ASC
            var pendingCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.PENDING)
                .OrderBy(c => c.CreatedDate)
                .ToList();
            var pagedResult = (pendingCurricula.Count, pendingCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Sort ascending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_PENDING, // Only PENDING status
                0, // PageSize is 0 (no pagination)
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for ascending sort by CreatedDate and filtering by PENDING status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Ensure sorting is ascending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByAgeASCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 2,
                    AgeEnd = 7,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Filter by PENDING status and order by AgeFrom ASC
            var pendingCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.PENDING)
                .OrderBy(c => c.AgeFrom)
                .ToList();
            var pagedResult = (pendingCurricula.Count, pendingCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Sort ascending by AgeFrom
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_PENDING, // Only PENDING status
                0, // PageSize is 0 (no pagination)
                SD.AGE_FROM, // Order by AgeFrom
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for ascending sort by AgeFrom and filtering by PENDING status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Ensure sorting is ascending by AgeFrom
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByAgeDESCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Filter by PENDING status and order by Age DESC
            var pendingCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.PENDING)
                .OrderByDescending(c => c.AgeFrom)
                .ToList();
            var pagedResult = (pendingCurricula.Count, pendingCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Sort descending by AgeFrom
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_PENDING, // Only PENDING status
                0, // PageSize is 0 (no pagination)
                SD.AGE, // Order by Age
                SD.ORDER_DESC, // Descending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for descending sort by Age and filtering by PENDING status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Ensure sorting is descending by AgeFrom
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByAgeDescAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Filter by APPROVE status and order by Age DESC
            var approvedCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.APPROVE)
                .OrderByDescending(c => c.AgeFrom)
                .ToList();
            var pagedResult = (approvedCurricula.Count, approvedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(), // Order by Age
                        true // Sort descending by AgeFrom
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_APPROVE, // Only APPROVE status
                0, // PageSize is 0 (no pagination)
                SD.AGE, // Order by Age
                SD.ORDER_DESC, // Descending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for descending sort by AgeFrom and filtering by APPROVE status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        true // Ensure sorting is descending by AgeFrom
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByAgeASCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Filter by APPROVE status and order by Age ASC
            var approvedCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.APPROVE)
                .OrderBy(c => c.AgeFrom) // Order by AgeFrom ASC
                .ToList();
            var pagedResult = (approvedCurricula.Count, approvedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Sort ascending by AgeFrom
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_APPROVE, // Only APPROVE status
                0, // PageSize is 0 (no pagination)
                SD.AGE, // Order by Age
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for ascending sort by AgeFrom and filtering by APPROVE status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.IsAny<Expression<Func<Curriculum, object>>>(),
                        false // Ensure sorting is ascending by AgeFrom
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateASCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.REJECT,
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Filter by APPROVE status and order by CreatedDate ASC
            var approvedCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.APPROVE)
                .OrderBy(c => c.CreatedDate)
                .ToList();
            var pagedResult = (approvedCurricula.Count, approvedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Sort ascending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_APPROVE, // Only APPROVE status
                0, // PageSize is 0 (no pagination)
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for ascending sort by CreatedDate and filtering by APPROVE status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        false // Ensure sorting is ascending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsZeroPageNumberIsOneOrderByCreatedDateDESCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum Description 1",
                    RequestStatus = SD.Status.PENDING,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 2",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum Description 3",
                    RequestStatus = SD.Status.APPROVE,
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Filter by APPROVE status and order by CreatedDate DESC
            var approvedCurricula = curricula
                .Where(c => c.RequestStatus == SD.Status.APPROVE)
                .OrderByDescending(c => c.CreatedDate)
                .ToList();
            var pagedResult = (approvedCurricula.Count, approvedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Sort descending by CreatedDate
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_APPROVE, // Only APPROVE status
                0, // PageSize is 0 (no pagination)
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_DESC, // Descending order
                1 // PageNumber
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

            // Verify repository method was called with correct parameters for descending sort by CreatedDate and filtering by APPROVE status
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(),
                        "Submitter",
                        null,
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ),
                        true // Ensure sorting is descending by CreatedDate
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsFivePageNumberIsOneOrderByAgeASCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING, // Status is not filtered (All status)
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE, // Status is not filtered (All status)
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.REJECT, // Status is not filtered (All status)
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Order by Age in ascending order
            var sortedCurricula = curricula.OrderBy(c => c.AgeFrom).ToList();
            var pagedResult = (sortedCurricula.Count, sortedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // No filtering, all statuses
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.IsAny<Expression<Func<Curriculum, object>>>(), // Sort by Age
                        false // Ascending order
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_ALL, // All status
                5, // PageSize is 5
                SD.AGE_FROM, // Order by AgeFrom
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber is 1
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

            // Verify repository method was called with correct parameters for sorting by Age in ascending order
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // No filtering, all statuses
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.IsAny<Expression<Func<Curriculum, object>>>(), // Sort by Age
                        false // Ensure ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsFivePageNumberIsOneOrderByAgeDESCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING, // Status is not filtered (All status)
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE, // Status is not filtered (All status)
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.REJECT, // Status is not filtered (All status)
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Order by Age in descending order
            var sortedCurricula = curricula.OrderByDescending(c => c.AgeFrom).ToList();
            var pagedResult = (sortedCurricula.Count, sortedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // No filtering, all statuses
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.IsAny<Expression<Func<Curriculum, object>>>(), // Sort by Age
                        true // Descending order
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_ALL, // All status
                5, // PageSize is 5
                SD.AGE_FROM, // Order by AgeFrom
                SD.ORDER_DESC, // Descending order
                1 // PageNumber is 1
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

            // Verify repository method was called with correct parameters for sorting by Age in descending order
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // No filtering, all statuses
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.IsAny<Expression<Func<Curriculum, object>>>(), // Sort by Age
                        true // Ensure descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateDESCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING, // Status is not filtered (All status)
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE, // Status is not filtered (All status)
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.REJECT, // Status is not filtered (All status)
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Order by CreatedDate in descending order
            var sortedCurricula = curricula.OrderByDescending(c => c.CreatedDate).ToList();
            var pagedResult = (sortedCurricula.Count, sortedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // No filtering, all statuses
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Sort by CreatedDate
                        true // Descending order
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_ALL, // All status
                5, // PageSize is 5
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_DESC, // Descending order
                1 // PageNumber is 1
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

            // Verify repository method was called with correct parameters for sorting by CreatedDate in descending order
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // No filtering, all statuses
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Sort by CreatedDate
                        true // Ensure descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateASCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING, // Status is not filtered (All status)
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE, // Status is not filtered (All status)
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.REJECT, // Status is not filtered (All status)
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Order by CreatedDate in ascending order
            var sortedCurricula = curricula.OrderBy(c => c.CreatedDate).ToList();
            var pagedResult = (sortedCurricula.Count, sortedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // No filtering, all statuses
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Sort by CreatedDate
                        false // Ascending order
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_ALL, // All status
                5, // PageSize is 5
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber is 1
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

            // Verify repository method was called with correct parameters for sorting by CreatedDate in ascending order
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // No filtering, all statuses
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Sort by CreatedDate
                        false // Ensure ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateDESCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.REJECT, // Status is REJECT
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.REJECT, // Status is REJECT
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.REJECT, // Status is REJECT
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Order by CreatedDate in descending order
            var sortedCurricula = curricula.OrderByDescending(c => c.CreatedDate).ToList();
            var pagedResult = (sortedCurricula.Count, sortedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for REJECT status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Sort by CreatedDate
                        true // Descending order
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_REJECT, // Filter by REJECT status
                5, // PageSize is 5
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_DESC, // Descending order
                1 // PageNumber is 1
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

            // Verify repository method was called with correct parameters for sorting by CreatedDate in descending order
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for REJECT status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Sort by CreatedDate
                        true // Ensure descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateASCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.REJECT, // Status is REJECT
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.REJECT, // Status is REJECT
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.REJECT, // Status is REJECT
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Order by CreatedDate in ascending order
            var sortedCurricula = curricula.OrderBy(c => c.CreatedDate).ToList();
            var pagedResult = (sortedCurricula.Count, sortedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for REJECT status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Sort by CreatedDate
                        false // Ascending order
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_REJECT, // Filter by REJECT status
                5, // PageSize is 5
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber is 1
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

            // Verify repository method was called with correct parameters for sorting by CreatedDate in ascending order
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for REJECT status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Sort by CreatedDate
                        false // Ensure ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsFivePageNumberIsOneOrderByAgeASCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.REJECT, // Status is REJECT
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.REJECT, // Status is REJECT
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.REJECT, // Status is REJECT
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Order by Age in ascending order
            var sortedCurricula = curricula.OrderBy(c => c.AgeFrom).ToList();
            var pagedResult = (sortedCurricula.Count, sortedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for REJECT status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.IsAny<Expression<Func<Curriculum, object>>>(), // Sort by AgeFrom
                        false // Ascending order
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_REJECT, // Filter by REJECT status
                5, // PageSize is 5
                SD.AGE, // Order by Age
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber is 1
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

            // Verify repository method was called with correct parameters for sorting by Age in ascending order
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for REJECT status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.IsAny<Expression<Func<Curriculum, object>>>(), // Sort by AgeFrom
                        false // Ensure ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsFivePageNumberIsOneOrderByAgeDESCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.REJECT, // Status is REJECT
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.REJECT, // Status is REJECT
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.REJECT, // Status is REJECT
                    RejectionReason = "Rejected reason",
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Order curricula by AgeFrom in descending order
            var sortedCurricula = curricula.OrderByDescending(c => c.AgeFrom).ToList();
            var pagedResult = (sortedCurricula.Count, sortedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for REJECT status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.IsAny<Expression<Func<Curriculum, object>>>(), // Sort by AgeFrom
                        true // Descending order (AgeFrom)
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_REJECT, // Filter by REJECT status
                5, // PageSize is 5
                SD.AGE, // Order by Age (AgeFrom)
                SD.ORDER_DESC, // Descending order
                1 // PageNumber is 1
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

            // Verify repository method was called with correct parameters for sorting by Age in descending order
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for REJECT status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.IsAny<Expression<Func<Curriculum, object>>>(), // Sort by AgeFrom
                        true // Ensure descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsFivePageNumberIsOneOrderByAgeDESCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.PENDING, // Status is PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.PENDING, // Status is PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING, // Status is PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
            };

            // Order curricula by AgeFrom in descending order
            var sortedCurricula = curricula.OrderByDescending(c => c.AgeFrom).ToList();
            var pagedResult = (sortedCurricula.Count, sortedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.IsAny<Expression<Func<Curriculum, object>>>(), // Sort by AgeFrom
                        true // Descending order (AgeFrom)
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_PENDING, // Filter by PENDING status
                5, // PageSize is 5
                SD.AGE, // Order by Age (AgeFrom)
                SD.ORDER_DESC, // Descending order
                1 // PageNumber is 1
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

            // Verify repository method was called with correct parameters for sorting by Age in descending order
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.IsAny<Expression<Func<Curriculum, object>>>(), // Sort by AgeFrom
                        true // Ensure descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsFivePageNumberIsOneOrderByAgeASCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING, // Status is PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1),
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.PENDING, // Status is PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2),
                },
                new Curriculum
                {
                    Id = 3,
                    AgeFrom = 10,
                    AgeEnd = 15,
                    Description = "Curriculum for Ages 10-15",
                    RequestStatus = SD.Status.PENDING, // Status is PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-3),
                },
            };

            // Order curricula by AgeFrom in ascending order
            var sortedCurricula = curricula.OrderBy(c => c.AgeFrom).ToList();
            var pagedResult = (sortedCurricula.Count, sortedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.IsAny<Expression<Func<Curriculum, object>>>(), // Sort by AgeFrom
                        false // Ascending order (AgeFrom)
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_PENDING, // Filter by PENDING status
                5, // PageSize is 5
                SD.AGE, // Order by Age (AgeFrom)
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber is 1
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

            // Verify repository method was called with correct parameters for sorting by Age in ascending order
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.IsAny<Expression<Func<Curriculum, object>>>(), // Sort by AgeFrom
                        false // Ensure ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateASCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING, // Status is PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2), // Older than the second item
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.PENDING, // Status is PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1), // Newer than the first item
                },
            };

            // Sort curricula by CreatedDate in ascending order
            var sortedCurricula = curricula.OrderBy(c => c.CreatedDate).ToList();
            var pagedResult = (sortedCurricula.Count, sortedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Sort by CreatedDate
                        false // Ascending order (CreatedDate)
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_PENDING, // Filter by PENDING status
                5, // PageSize is 5
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber is 1
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

            // Verify repository method was called with correct parameters for sorting by CreatedDate in ascending order
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Sort by CreatedDate
                        false // Ensure ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateDESCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.PENDING, // Status is PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2), // Older than the second item
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.PENDING, // Status is PENDING
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1), // Newer than the first item
                },
            };

            // Sort curricula by CreatedDate in descending order
            var sortedCurricula = curricula.OrderByDescending(c => c.CreatedDate).ToList();
            var pagedResult = (sortedCurricula.Count, sortedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Sort by CreatedDate
                        true // Descending order (CreatedDate)
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_PENDING, // Filter by PENDING status
                5, // PageSize is 5
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_DESC, // Descending order
                1 // PageNumber is 1
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

            // Verify repository method was called with correct parameters for sorting by CreatedDate in descending order
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for PENDING status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Sort by CreatedDate
                        true // Ensure descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateDESCAndStatusIsApproved()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.APPROVE, // Status is APPROVED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2), // Older item
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE, // Status is APPROVED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1), // Newer item
                },
            };

            // Sort curricula by CreatedDate in descending order
            var sortedCurricula = curricula.OrderByDescending(c => c.CreatedDate).ToList();
            var pagedResult = (sortedCurricula.Count, sortedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVED status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Sort by CreatedDate
                        true // Descending order (CreatedDate)
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_APPROVE, // Filter by APPROVED status
                5, // PageSize is 5
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_DESC, // Descending order
                1 // PageNumber is 1
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

            // Verify repository method was called with correct parameters for sorting by CreatedDate in descending order
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVED status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Sort by CreatedDate
                        true // Ensure descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsFivePageNumberIsOneOrderByCreatedDateASCAndStatusIsApproved()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.APPROVE, // Status is APPROVED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2), // Older item
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE, // Status is APPROVED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1), // Newer item
                },
            };

            // Sort curricula by CreatedDate in ascending order
            var sortedCurricula = curricula.OrderBy(c => c.CreatedDate).ToList();
            var pagedResult = (sortedCurricula.Count, sortedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVED status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Sort by CreatedDate
                        false // Ascending order (CreatedDate)
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_APPROVE, // Filter by APPROVED status
                5, // PageSize is 5
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber is 1
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

            // Verify repository method was called with correct parameters for sorting by CreatedDate in ascending order
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVED status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.Is<Expression<Func<Curriculum, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Sort by CreatedDate
                        false // Ensure ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsFivePageNumberIsOneOrderByAgeASCAndStatusIsApproved()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE, // Status is APPROVED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1), // Created recently
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.APPROVE, // Status is APPROVED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2), // Created earlier
                },
            };

            // Sort curricula by AgeFrom in ascending order
            var sortedCurricula = curricula.OrderBy(c => c.AgeFrom).ToList();
            var pagedResult = (sortedCurricula.Count, sortedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVED status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.IsAny<Expression<Func<Curriculum, object>>>(), // Sort by AgeFrom
                        false // Ascending order (AgeFrom)
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_APPROVE, // Filter by APPROVED status
                5, // PageSize is 5
                SD.AGE, // Order by Age (AgeFrom)
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber is 1
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

            // Verify repository method was called with correct parameters for sorting by AgeFrom in ascending order
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVED status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.IsAny<Expression<Func<Curriculum, object>>>(), // Sort by AgeFrom
                        false // Ensure ascending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedCurriculum_WhenUserIsTutorWithNoSearchPageSizeIsFivePageNumberIsOneOrderByAgeDESCAndStatusIsApproved()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Set to TUTOR_ROLE as user is a tutor
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            var tutor = new Tutor() { TutorId = "testTutorId" };

            var curricula = new List<Curriculum>
            {
                new Curriculum
                {
                    Id = 1,
                    AgeFrom = 5,
                    AgeEnd = 10,
                    Description = "Curriculum for Ages 5-10",
                    RequestStatus = SD.Status.APPROVE, // Status is APPROVED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-1), // Created recently
                },
                new Curriculum
                {
                    Id = 2,
                    AgeFrom = 0,
                    AgeEnd = 5,
                    Description = "Curriculum for Ages 0-5",
                    RequestStatus = SD.Status.APPROVE, // Status is APPROVED
                    VersionNumber = 1,
                    SubmitterId = "testTutorId",
                    IsDeleted = false,
                    Submitter = tutor,
                    CreatedDate = DateTime.Now.AddDays(-2), // Created earlier
                },
            };

            // Sort curricula by AgeFrom in descending order
            var sortedCurricula = curricula.OrderByDescending(c => c.AgeFrom).ToList();
            var pagedResult = (sortedCurricula.Count, sortedCurricula);

            _curriculumRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVED status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.IsAny<Expression<Func<Curriculum, object>>>(), // Sort by AgeFrom
                        true // Descending order (AgeFrom)
                    )
                )
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllAsync(
                "", // No search term
                SD.STATUS_APPROVE, // Filter by APPROVED status
                5, // PageSize is 5
                SD.AGE, // Order by Age (AgeFrom)
                SD.ORDER_DESC, // Descending order
                1 // PageNumber is 1
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

            // Verify repository method was called with correct parameters for sorting by AgeFrom in descending order
            _curriculumRepositoryMock.Verify(
                repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<Curriculum, bool>>>(), // Filter for APPROVED status
                        "Submitter",
                        5, // PageSize is 5
                        1, // PageNumber is 1
                        It.IsAny<Expression<Func<Curriculum, object>>>(), // Sort by AgeFrom
                        true // Ensure descending order
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnInternalServerError_WhenExceptionIsThrown()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testTutorId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE) // Set to TUTOR_ROLE as user is a tutor
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            _resourceServiceMock.Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                    .Returns("An internal server error occurred.");
            // Mock the repository to throw an exception when GetAllAsync is called
            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<Curriculum, bool>>>(),
                    "Submitter",
                    It.IsAny<int>(), // Any page size
                    It.IsAny<int>(), // Any page number
                    It.IsAny<Expression<Func<Curriculum, object>>>(), // Any orderBy expression
                    It.IsAny<bool>())) // Any sorting order
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetAllAsync(
                "searchCurriculum",
                SD.STATUS_REJECT, // Only REJECTED status
                5, // PageSize = 5
                SD.CREATED_DATE, // Order by CreatedDate
                SD.ORDER_ASC, // Ascending order
                1 // PageNumber = 1
            );

            // Assert
            result.Should().NotBeNull();
            var statusCodeResult = result.Result as ObjectResult;
            statusCodeResult.Should().NotBeNull();
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var response = statusCodeResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            response.ErrorMessages.FirstOrDefault().Should().Contain("An internal server error occurred.");

        }

        [Fact]
        public async Task CreateAsync_ShouldReturnBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            _controller.ModelState.AddModelError("Title", "Required");

            // Act
            var result = await _controller.CreateAsync(new CurriculumCreateDTO());

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.ErrorMessages.Should().Contain(_resourceServiceMock.Object.GetString(SD.BAD_REQUEST_MESSAGE, SD.CURRICULUM));
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnBadRequest_WhenDuplicateAgeRangeExists()
        {
            // Arrange
            var curriculumDto = new CurriculumCreateDTO { AgeFrom = 5, AgeEnd = 10, OriginalCurriculumId = 0 };
            string userId = "testId";
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                )
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var duplicateCurriculum = new Curriculum { AgeFrom = 5, AgeEnd = 10, SubmitterId = userId, IsActive = true, IsDeleted = false, OriginalCurriculumId = 1 };
            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), null, null, It.IsAny<Expression<Func<Curriculum, object>>>(), false))
                .ReturnsAsync((1, new List<Curriculum> { duplicateCurriculum }));

            // Act
            var result = await _controller.CreateAsync(curriculumDto);

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.ErrorMessages.Should().Contain(_resourceServiceMock.Object.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.AGE));
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnCreated_WhenCurriculumCreatedSuccessfullyWithOriginalIdIsZero()
        {
            // Arrange
            var curriculumDto = new CurriculumCreateDTO { AgeFrom = 5, AgeEnd = 10, OriginalCurriculumId = 0 };
            string userId = "testId";
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                )
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), null, null, It.IsAny<Expression<Func<Curriculum, object>>>(), false))
                .ReturnsAsync((0, new List<Curriculum>()));

            _curriculumRepositoryMock
                .Setup(repo => repo.GetNextVersionNumberAsync(It.IsAny<int>()))
                .ReturnsAsync(1);

            var newCurriculum = new Curriculum { Id = 1, AgeFrom = 5, AgeEnd = 10, SubmitterId = userId, CreatedDate = DateTime.Now };
            _curriculumRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Curriculum>()))
                .ReturnsAsync(newCurriculum);

            // Act
            var result = await _controller.CreateAsync(curriculumDto);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Result.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnCreated_WhenCurriculumCreatedSuccessfullyWithOriginalIdDifZero()
        {
            // Arrange
            var curriculumDto = new CurriculumCreateDTO { AgeFrom = 5, AgeEnd = 10, OriginalCurriculumId = 1 };
            string userId = "testId";
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                )
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), null, null, It.IsAny<Expression<Func<Curriculum, object>>>(), false))
                .ReturnsAsync((0, new List<Curriculum>()));

            _curriculumRepositoryMock
                .Setup(repo => repo.GetNextVersionNumberAsync(It.IsAny<int>()))
                .ReturnsAsync(1);

            var newCurriculum = new Curriculum { Id = 1, AgeFrom = 5, AgeEnd = 10, SubmitterId = userId, CreatedDate = DateTime.Now };
            _curriculumRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Curriculum>()))
                .ReturnsAsync(newCurriculum);

            // Act
            var result = await _controller.CreateAsync(curriculumDto);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Result.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnInternalServerError_WhenExceptionIsThrown()
        {
            // Arrange
            var curriculumDto = new CurriculumCreateDTO { AgeFrom = 5, AgeEnd = 10 };
            string userId = "testId";
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                )
                ,
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), null, null, null, true))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.CreateAsync(curriculumDto);

            // Assert
            var statusCodeResult = result.Result as ObjectResult;
            statusCodeResult.Should().NotBeNull();
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var response = statusCodeResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            response.ErrorMessages.Should().Contain(_resourceServiceMock.Object.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE));
        }


        [Fact]
        public async Task UpdateStatusRequest_ApproveStatus_Succeeds()
        {
            // Arrange
            var curriculumId = 1;
            var userId = "user123";
            var curriculum = new Curriculum { Id = curriculumId, SubmitterId = userId, RequestStatus = Status.PENDING, Description = "Test Curriculum" };
            var tutor = new ApplicationUser { Id = userId, FullName = "Tutor Name", Email = "tutor@example.com" };
            var changeStatusDTO = new ChangeStatusDTO { Id = curriculumId, StatusChange = (int)Status.APPROVE };

            _curriculumRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), true, "Submitter", null))
                .ReturnsAsync(curriculum);
            _userRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(tutor);
            _resourceServiceMock.Setup(r => r.GetString(It.IsAny<string>(), It.IsAny<string>())).Returns("Error message");

            // Set user identity
            var userClaims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Role, STAFF_ROLE) };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));

            // Act
            var result = await _controller.UpdateStatusRequest(curriculumId, changeStatusDTO);
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.Result.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateStatusRequest_RejectStatus_Succeeds()
        {
            // Arrange
            var curriculumId = 1;
            var userId = "user123";
            var submitterUser = new Tutor { TutorId = "testTutorId", AboutMe= "testTutor" };
            var curriculum = new Curriculum { Id = curriculumId, SubmitterId = userId, Submitter = submitterUser, RequestStatus = Status.PENDING, Description = "Test Curriculum" };
            var tutor = new ApplicationUser { Id = userId, FullName = "Tutor Name", Email = "tutor@example.com" };

            var changeStatusDTO = new ChangeStatusDTO { Id = curriculumId, StatusChange = (int)Status.REJECT, RejectionReason = "Invalid document" };

            _curriculumRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), true, "Submitter", null))
                .ReturnsAsync(curriculum);

            _userRepositoryMock
               .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), false, null))
               .ReturnsAsync(tutor);
            // Set user identity
            var userClaims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Role, STAFF_ROLE) };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));

            // Act
            var result = await _controller.UpdateStatusRequest(curriculumId, changeStatusDTO);
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.Result.Should().NotBeNull();
        }


        [Fact]
        public async Task UpdateStatusRequest_ReturnsBadRequest_IfCurriculumIsNull()
        {
            // Arrange
            var curriculumId = 1;
            var changeStatusDTO = new ChangeStatusDTO { Id = 1, StatusChange = (int)Status.APPROVE };

            _curriculumRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), false, null, null))
                .ReturnsAsync((Curriculum)null);
            _resourceServiceMock.Setup(r => r.GetString(It.IsAny<string>(), It.IsAny<string>())).Returns("Error message");

            // Act
            var result = await _controller.UpdateStatusRequest(curriculumId, changeStatusDTO);
            var badRequestResult = result.Result as BadRequestObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Error message");
        }


        [Fact]
        public async Task UpdateStatusRequest_ReturnsBadRequest_IfCurriculumStatusIsNotPending()
        {
            // Arrange
            var curriculumId = 1;
            var curriculum = new Curriculum { Id = 1, RequestStatus = Status.APPROVE };
            var changeStatusDTO = new ChangeStatusDTO { Id = 1, StatusChange = (int)Status.REJECT };

            _curriculumRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), false, null, null))
                .ReturnsAsync(curriculum);
            _resourceServiceMock.Setup(r => r.GetString(It.IsAny<string>(), It.IsAny<string>())).Returns("Error message");

            // Act
            var result = await _controller.UpdateStatusRequest(curriculumId, changeStatusDTO);
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
            var curriculumId = 1;
            var changeStatusDTO = new ChangeStatusDTO { Id = 1, StatusChange = (int)Status.APPROVE };
            _resourceServiceMock.Setup(r => r.GetString(INTERNAL_SERVER_ERROR_MESSAGE)).Returns("Internal server error");
            _curriculumRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), true, "Submitter", null))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateStatusRequest(curriculumId, changeStatusDTO);
            var internalServerErrorResult = result.Result as ObjectResult;

            // Assert
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = internalServerErrorResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Internal server error");
        }

    }

}
