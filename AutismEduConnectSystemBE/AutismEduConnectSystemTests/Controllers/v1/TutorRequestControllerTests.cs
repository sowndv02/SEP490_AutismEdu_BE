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
using Microsoft.AspNetCore.Identity.UI.Services;
using AutismEduConnectSystem.Repository;
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
using static AutismEduConnectSystem.SD;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace AutismEduConnectSystem.Controllers.v1.Tests
{
    public class TutorRequestControllerTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ITutorRequestRepository> _mockTutorRequestRepository;
        private readonly Mock<IChildInformationRepository> _mockChildInformationRepository;
        private readonly IMapper _mockMapper;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IEmailSender> _mockMessageBus;
        private readonly Mock<IResourceService> _mockResourceService;
        private readonly Mock<ILogger<TutorRequestController>> _mockLogger;
        private readonly Mock<INotificationRepository> _mockNotificationRepository;
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private TutorRequestController _controller;

        public TutorRequestControllerTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockTutorRequestRepository = new Mock<ITutorRequestRepository>();
            _mockChildInformationRepository = new Mock<IChildInformationRepository>();
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingConfig());
            });
            _mockMapper = config.CreateMapper();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockMessageBus = new Mock<IEmailSender>();
            _mockResourceService = new Mock<IResourceService>();
            _mockLogger = new Mock<ILogger<TutorRequestController>>();
            _mockNotificationRepository = new Mock<INotificationRepository>();
            _mockHubContext = new Mock<IHubContext<NotificationHub>>();

            // Set up configuration mock (e.g., PageSize, RabbitMQ queue name)
            _mockConfiguration.Setup(c => c["APIConfig:PageSize"]).Returns("10");
            _mockConfiguration.Setup(c => c["RabbitMQSettings:QueueName"]).Returns("TestQueue");

            // Initialize the controller with mocked dependencies
            _controller = new TutorRequestController(
                _mockUserRepository.Object,
                _mockTutorRequestRepository.Object,
                _mockMapper,
                _mockConfiguration.Object,
                _mockMessageBus.Object,
                _mockResourceService.Object,
                _mockLogger.Object,
                _mockNotificationRepository.Object,
                _mockHubContext.Object,
                _mockChildInformationRepository.Object
            );
        }

        [Fact]
        public async Task GetAllRequestNoStudentProfileAsync_Unauthorized_ReturnsUnauthorizedResponse()
        {
            _mockResourceService
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            };

            // Act
            var result = await _controller.GetAllRequestNoStudentProfileAsync();

            // Assert
            var unauthorizedResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            unauthorizedResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
            var apiResponse = unauthorizedResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            apiResponse.ErrorMessages.Should().Contain("Unauthorized access.");
        }

        [Fact]
        public async Task GetAllRequestNoStudentProfileAsync_Forbidden_ReturnsForbiddenResponse()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbiden access.");
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "valid-user-id"),
                new Claim(
                    ClaimTypes.Role,
                    SD.PARENT_ROLE
                ) // User has an invalid role
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Act
            var result = await _controller.GetAllRequestNoStudentProfileAsync();

            // Assert
            var forbiddenResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            forbiddenResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
            var apiResponse = forbiddenResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            apiResponse.ErrorMessages.Should().Contain("Forbiden access.");
        }

        [Fact]
        public async Task GetAllRequestNoStudentProfileAsync_WhenExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            var userId = "test-tutor-id";
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("An unexpected error occurred.");
            // Mock user claims to simulate authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Set the User for the Controller
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock GetAllNotPagingAsync to throw an exception
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.GetAllRequestNoStudentProfileAsync();

            // Assert
            var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var response = objectResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            response
                .ErrorMessages.Should()
                .ContainSingle(error => error == "An unexpected error occurred.");
        }

        [Fact]
        public async Task GetAllRequestNoStudentProfileAsync_ValidOrderByCreatedDate_AscendingOrder_ReturnsOrderedList()
        {
            // Arrange
            var userId = "test-tutor-id";

            // Mock user claims to simulate authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var mockData = new List<TutorRequest>
            {
                new TutorRequest { Id = 1, CreatedDate = DateTime.UtcNow.AddDays(-2) },
                new TutorRequest { Id = 2, CreatedDate = DateTime.UtcNow.AddDays(-1) },
                new TutorRequest { Id = 3, CreatedDate = DateTime.UtcNow },
            };

            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((mockData.Count, mockData.OrderBy(x => x.CreatedDate).ToList()));

            // Act
            var result = await _controller.GetAllRequestNoStudentProfileAsync(
                SD.CREATED_DATE,
                SD.ORDER_ASC
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var tutorRequests = apiResponse.Result as List<TutorRequestDTO>;
            tutorRequests.Should().NotBeNull();
            tutorRequests.Should().HaveCount(mockData.Count);
            tutorRequests.Should().BeInAscendingOrder(x => x.CreatedDate);
        }

        [Fact]
        public async Task GetAllRequestNoStudentProfileAsync_ValidOrderByCreatedDate_DescendingOrder_ReturnsOrderedList()
        {
            // Arrange
            var userId = "test-tutor-id";

            // Mock user claims to simulate authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var mockData = new List<TutorRequest>
            {
                new TutorRequest { Id = 1, CreatedDate = DateTime.UtcNow.AddDays(-2) },
                new TutorRequest { Id = 2, CreatedDate = DateTime.UtcNow.AddDays(-1) },
                new TutorRequest { Id = 3, CreatedDate = DateTime.UtcNow },
            };

            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync(
                    (mockData.Count, mockData.OrderByDescending(x => x.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllRequestNoStudentProfileAsync(
                SD.CREATED_DATE,
                SD.ORDER_DESC
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var tutorRequests = apiResponse.Result as List<TutorRequestDTO>;
            tutorRequests.Should().NotBeNull();
            tutorRequests.Should().HaveCount(mockData.Count);
            tutorRequests.Should().BeInDescendingOrder(x => x.CreatedDate);
        }

        [Fact]
        public async Task GetAllRequestNoStudentProfileAsync_OrderByNull_AscendingOrder_ReturnsOrderedList()
        {
            // Arrange
            var userId = "test-tutor-id";

            // Mock user claims to simulate authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var mockData = new List<TutorRequest>
            {
                new TutorRequest { Id = 1, CreatedDate = DateTime.UtcNow.AddDays(-2) },
                new TutorRequest { Id = 2, CreatedDate = DateTime.UtcNow.AddDays(-1) },
                new TutorRequest { Id = 3, CreatedDate = DateTime.UtcNow },
            };

            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((mockData.Count, mockData.OrderBy(x => x.CreatedDate).ToList()));

            // Act
            var result = await _controller.GetAllRequestNoStudentProfileAsync(null, SD.ORDER_ASC);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var tutorRequests = apiResponse.Result as List<TutorRequestDTO>;
            tutorRequests.Should().NotBeNull();
            tutorRequests.Should().HaveCount(mockData.Count);
            tutorRequests.Should().BeInAscendingOrder(x => x.CreatedDate);
        }

        [Fact]
        public async Task GetAllRequestNoStudentProfileAsync_OrderByNull_DescendingOrder_ReturnsOrderedList()
        {
            // Arrange
            var userId = "test-tutor-id";

            // Mock user claims to simulate authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var mockData = new List<TutorRequest>
            {
                new TutorRequest { Id = 1, CreatedDate = DateTime.UtcNow.AddDays(-2) },
                new TutorRequest { Id = 2, CreatedDate = DateTime.UtcNow.AddDays(-1) },
                new TutorRequest { Id = 3, CreatedDate = DateTime.UtcNow },
            };

            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync(
                    (mockData.Count, mockData.OrderByDescending(x => x.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllRequestNoStudentProfileAsync(null, SD.ORDER_DESC);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var tutorRequests = apiResponse.Result as List<TutorRequestDTO>;
            tutorRequests.Should().NotBeNull();
            tutorRequests.Should().HaveCount(mockData.Count);
            tutorRequests.Should().BeInDescendingOrder(x => x.CreatedDate);
        }

        [Fact]
        public async Task GetAllHistoryRequestAsync_UserNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            var userId = string.Empty; // Simulate unauthenticated user (no userId)
            var claims = new List<Claim>(); // No claims to represent unauthenticated state
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _mockResourceService
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Act
            var result = await _controller.GetAllHistoryRequestAsync();

            // Assert
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
        public async Task GetAllHistoryRequestAsync_UserHasInvalidRole_ReturnsForbidden()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbiden access.");
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "testUserId") };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Act
            var result = await _controller.GetAllHistoryRequestAsync();

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
        public async Task GetAllHistoryRequestAsync_WhenExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            var userId = "test-tutor-id";
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("An unexpected error occurred.");
            // Mock user claims to simulate authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.PARENT_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Set the User for the Controller
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock GetAllNotPagingAsync to throw an exception
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.GetAllHistoryRequestAsync();

            // Assert
            var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var response = objectResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            response
                .ErrorMessages.Should()
                .ContainSingle(error => error == "An unexpected error occurred.");
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Ascending order
        [InlineData(SD.ORDER_DESC)] // Descending order
        public async Task GetAllHistoryRequestAsync_ValidStatusApprove_OrderByCreatedDate_ReturnsCorrectResults(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-user-id"; // Simulate an authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(
                    ClaimTypes.Role,
                    SD.PARENT_ROLE
                ) // Simulate a parent role
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.UtcNow,
                },
            };
            var sortedTutorRequests =
                sortOrder == SD.ORDER_DESC
                    ? tutorRequests.OrderByDescending(x => x.CreatedDate).ToList()
                    : tutorRequests.OrderBy(x => x.CreatedDate).ToList();

            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5,
                        1,
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((tutorRequests.Count, sortedTutorRequests));

            // Act
            var result = await _controller.GetAllHistoryRequestAsync(
                status: STATUS_APPROVE,
                orderBy: SD.CREATED_DATE,
                sort: sortOrder
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Ensure that the result list is ordered by CreatedDate as per the sortOrder
            var resultList = apiResponse.Result.Should().BeOfType<List<TutorRequestDTO>>().Subject;
            if (sortOrder == SD.ORDER_DESC)
            {
                resultList[0].CreatedDate.Should().BeAfter(resultList[1].CreatedDate);
            }
            else
            {
                resultList[0].CreatedDate.Should().BeBefore(resultList[1].CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Ascending order
        [InlineData(SD.ORDER_DESC)] // Descending order
        public async Task GetAllHistoryRequestAsync_StatusApprove_OrderByCreatedDate_ReturnsCorrectResults(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-user-id"; // Simulate an authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(
                    ClaimTypes.Role,
                    SD.PARENT_ROLE
                ) // Simulate a parent role
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.UtcNow.AddDays(
                        -2
                    ) // Oldest date
                    ,
                },
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.UtcNow.AddDays(
                        -1
                    ) // Middle date
                    ,
                },
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.APPROVE,
                    CreatedDate =
                        DateTime.UtcNow // Most recent date
                    ,
                },
            };

            // Sort the list based on sortOrder
            var sortedTutorRequests =
                sortOrder == SD.ORDER_DESC
                    ? tutorRequests.OrderByDescending(x => x.CreatedDate).ToList()
                    : tutorRequests.OrderBy(x => x.CreatedDate).ToList();

            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5,
                        1,
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((tutorRequests.Count, sortedTutorRequests));

            // Act
            var result = await _controller.GetAllHistoryRequestAsync(
                status: STATUS_APPROVE,
                orderBy: SD.CREATED_DATE,
                sort: sortOrder
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Ensure that the result list is ordered by CreatedDate as per the sortOrder
            var resultList = apiResponse.Result.Should().BeOfType<List<TutorRequestDTO>>().Subject;
            if (sortOrder == SD.ORDER_DESC)
            {
                resultList.Should().BeInDescendingOrder(x => x.CreatedDate);
            }
            else
            {
                resultList.Should().BeInAscendingOrder(x => x.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Ascending order
        [InlineData(SD.ORDER_DESC)] // Descending order
        public async Task GetAllHistoryRequestAsync_StatusReject_OrderByCreatedDate_ReturnsCorrectResults(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-user-id"; // Simulate an authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(
                    ClaimTypes.Role,
                    SD.PARENT_ROLE
                ) // Simulate a parent role
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.UtcNow.AddDays(
                        -2
                    ) // Oldest date
                    ,
                },
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.UtcNow.AddDays(
                        -1
                    ) // Middle date
                    ,
                },
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.REJECT,
                    CreatedDate =
                        DateTime.UtcNow // Most recent date
                    ,
                },
            };

            // Sort the list based on sortOrder
            var sortedTutorRequests =
                sortOrder == SD.ORDER_DESC
                    ? tutorRequests.OrderByDescending(x => x.CreatedDate).ToList()
                    : tutorRequests.OrderBy(x => x.CreatedDate).ToList();

            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5,
                        1,
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((tutorRequests.Count, sortedTutorRequests));

            // Act
            var result = await _controller.GetAllHistoryRequestAsync(
                status: STATUS_REJECT,
                orderBy: SD.CREATED_DATE,
                sort: sortOrder
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Ensure that the result list is ordered by CreatedDate as per the sortOrder
            var resultList = apiResponse.Result.Should().BeOfType<List<TutorRequestDTO>>().Subject;
            if (sortOrder == SD.ORDER_DESC)
            {
                resultList.Should().BeInDescendingOrder(x => x.CreatedDate);
            }
            else
            {
                resultList.Should().BeInAscendingOrder(x => x.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Ascending order
        [InlineData(SD.ORDER_DESC)] // Descending order
        public async Task GetAllHistoryRequestAsync_StatusReject_OrderByNull_ReturnsCorrectResults(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-user-id"; // Simulate an authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(
                    ClaimTypes.Role,
                    SD.PARENT_ROLE
                ) // Simulate a parent role
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.UtcNow.AddDays(
                        -2
                    ) // Oldest date
                    ,
                },
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.UtcNow.AddDays(
                        -1
                    ) // Middle date
                    ,
                },
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.REJECT,
                    CreatedDate =
                        DateTime.UtcNow // Most recent date
                    ,
                },
            };

            // Since orderBy is null, sorting logic should default to some predefined behavior (e.g., no sorting).
            // For the test, we assume the repository does not sort when orderBy is null.
            var sortedTutorRequests =
                sortOrder == SD.ORDER_DESC
                    ? tutorRequests.OrderByDescending(x => x.CreatedDate).ToList()
                    : tutorRequests.OrderBy(x => x.CreatedDate).ToList();
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5,
                        1,
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((tutorRequests.Count, sortedTutorRequests));

            // Act
            var result = await _controller.GetAllHistoryRequestAsync(
                status: STATUS_REJECT,
                orderBy: null,
                sort: sortOrder
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Ensure the result list matches the unsorted expectedTutorRequests since orderBy is null
            var resultList = apiResponse.Result.Should().BeOfType<List<TutorRequestDTO>>().Subject;
            resultList.Should().HaveCount(tutorRequests.Count);

            // Validate that the result maintains the same order as the original input
            resultList
                .Select(x => x.CreatedDate)
                .Should()
                .BeEquivalentTo(
                    sortedTutorRequests.Select(x => x.CreatedDate),
                    options => options.WithStrictOrdering()
                );
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Ascending order
        [InlineData(SD.ORDER_DESC)] // Descending order
        public async Task GetAllHistoryRequestAsync_StatusPending_OrderByNull_ReturnsCorrectResults(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-user-id"; // Simulate an authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(
                    ClaimTypes.Role,
                    SD.PARENT_ROLE
                ) // Simulate a parent role
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.UtcNow.AddDays(
                        -2
                    ) // Oldest date
                    ,
                },
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.UtcNow.AddDays(
                        -1
                    ) // Middle date
                    ,
                },
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.PENDING,
                    CreatedDate =
                        DateTime.UtcNow // Most recent date
                    ,
                },
            };

            // Since orderBy is null, the sorting logic should default to some predefined behavior (no sorting).
            // For the test, we assume the repository does not sort when orderBy is null.
            var sortedTutorRequests =
                sortOrder == SD.ORDER_DESC
                    ? tutorRequests.OrderByDescending(x => x.CreatedDate).ToList()
                    : tutorRequests.OrderBy(x => x.CreatedDate).ToList();
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5,
                        1,
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((tutorRequests.Count, sortedTutorRequests));

            // Act
            var result = await _controller.GetAllHistoryRequestAsync(
                status: STATUS_PENDING,
                orderBy: null,
                sort: sortOrder
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Ensure the result list matches the unsorted expectedTutorRequests since orderBy is null
            var resultList = apiResponse.Result.Should().BeOfType<List<TutorRequestDTO>>().Subject;
            resultList.Should().HaveCount(tutorRequests.Count);

            // Validate that the result maintains the same order as the original input
            resultList
                .Select(x => x.CreatedDate)
                .Should()
                .BeEquivalentTo(
                    sortedTutorRequests.Select(x => x.CreatedDate),
                    options => options.WithStrictOrdering()
                );
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Ascending order
        [InlineData(SD.ORDER_DESC)] // Descending order
        public async Task GetAllHistoryRequestAsync_StatusPending_OrderByCreatedDate_ReturnsCorrectResults(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-user-id"; // Simulate an authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(
                    ClaimTypes.Role,
                    SD.PARENT_ROLE
                ) // Simulate a parent role
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.UtcNow.AddDays(
                        -2
                    ) // Oldest date
                    ,
                },
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.UtcNow.AddDays(
                        -1
                    ) // Middle date
                    ,
                },
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.PENDING,
                    CreatedDate =
                        DateTime.UtcNow // Most recent date
                    ,
                },
            };

            // Sort the list based on sortOrder
            var sortedTutorRequests =
                sortOrder == SD.ORDER_DESC
                    ? tutorRequests.OrderByDescending(x => x.CreatedDate).ToList()
                    : tutorRequests.OrderBy(x => x.CreatedDate).ToList();

            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5,
                        1,
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((tutorRequests.Count, sortedTutorRequests));

            // Act
            var result = await _controller.GetAllHistoryRequestAsync(
                status: STATUS_PENDING,
                orderBy: SD.CREATED_DATE,
                sort: sortOrder
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Ensure that the result list is ordered by CreatedDate as per the sortOrder
            var resultList = apiResponse.Result.Should().BeOfType<List<TutorRequestDTO>>().Subject;
            if (sortOrder == SD.ORDER_DESC)
            {
                resultList.Should().BeInDescendingOrder(x => x.CreatedDate);
            }
            else
            {
                resultList.Should().BeInAscendingOrder(x => x.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Ascending order
        [InlineData(SD.ORDER_DESC)] // Descending order
        public async Task GetAllHistoryRequestAsync_StatusAll_OrderByCreatedDate_ReturnsCorrectResults(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-user-id"; // Simulate an authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(
                    ClaimTypes.Role,
                    SD.PARENT_ROLE
                ) // Simulate a parent role
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.UtcNow.AddDays(
                        -2
                    ) // Oldest date
                    ,
                },
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.UtcNow.AddDays(
                        -1
                    ) // Middle date
                    ,
                },
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.REJECT,
                    CreatedDate =
                        DateTime.UtcNow // Most recent date
                    ,
                },
            };

            // Sort the list based on sortOrder
            var sortedTutorRequests =
                sortOrder == SD.ORDER_DESC
                    ? tutorRequests.OrderByDescending(x => x.CreatedDate).ToList()
                    : tutorRequests.OrderBy(x => x.CreatedDate).ToList();

            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5,
                        1,
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((tutorRequests.Count, sortedTutorRequests));

            // Act
            var result = await _controller.GetAllHistoryRequestAsync(
                status: STATUS_ALL,
                orderBy: SD.CREATED_DATE,
                sort: sortOrder
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Ensure that the result list is ordered by CreatedDate as per the sortOrder
            var resultList = apiResponse.Result.Should().BeOfType<List<TutorRequestDTO>>().Subject;
            if (sortOrder == SD.ORDER_DESC)
            {
                resultList.Should().BeInDescendingOrder(x => x.CreatedDate);
            }
            else
            {
                resultList.Should().BeInAscendingOrder(x => x.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Ascending order
        [InlineData(SD.ORDER_DESC)] // Descending order
        public async Task GetAllHistoryRequestAsync_StatusAll_OrderByNull_ReturnsCorrectResults(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-user-id"; // Simulate an authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(
                    ClaimTypes.Role,
                    SD.PARENT_ROLE
                ) // Simulate a parent role
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.UtcNow.AddDays(
                        -2
                    ) // Oldest date
                    ,
                },
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.UtcNow.AddDays(
                        -1
                    ) // Middle date
                    ,
                },
                new TutorRequest
                {
                    ParentId = userId,
                    RequestStatus = Status.APPROVE,
                    CreatedDate =
                        DateTime.UtcNow // Most recent date
                    ,
                },
            };

            // Sort the list based on sortOrder (null means no sorting)
            var sortedTutorRequests = tutorRequests;

            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5,
                        1,
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((tutorRequests.Count, sortedTutorRequests));

            // Act
            var result = await _controller.GetAllHistoryRequestAsync(
                status: "ALL",
                orderBy: null,
                sort: sortOrder
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Ensure that the result list is not sorted since orderBy is null
            var resultList = apiResponse.Result.Should().BeOfType<List<TutorRequestDTO>>().Subject;
            resultList
                .Should()
                .BeEquivalentTo(
                    tutorRequests
                        .Select(x => new TutorRequestDTO
                        {
                            RequestStatus = x.RequestStatus,
                            CreatedDate = x.CreatedDate,
                        })
                        .ToList()
                );
        }

        [Fact]
        public async Task GetAllAsync_WhenUserNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext() // No user added
                ,
            };
            _mockResourceService
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");
            // Act
            var result = await _controller.GetAllAsync(
                search: null,
                status: SD.STATUS_ALL,
                orderBy: SD.CREATED_DATE,
                sort: SD.ORDER_DESC,
                pageNumber: 1
            );

            // Assert
            var unauthorizedResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            unauthorizedResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            var response = unauthorizedResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            response.ErrorMessages.Should().Contain("Unauthorized access.");
        }

        [Fact]
        public async Task GetAllAsync_WhenUserNotAuthorized_ReturnsForbidden()
        {
            // Arrange
            var userId = "test-user-id";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(
                    ClaimTypes.Role,
                    SD.PARENT_ROLE
                ) // Simulate an unauthorized role
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbiden access.");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Act
            var result = await _controller.GetAllAsync(
                search: null,
                status: SD.STATUS_ALL,
                orderBy: SD.CREATED_DATE,
                sort: SD.ORDER_DESC,
                pageNumber: 1
            );

            // Assert
            var forbiddenResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            forbiddenResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

            var response = forbiddenResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            response.ErrorMessages.Should().Contain("Forbiden access.");
        }

        [Fact]
        public async Task GetAllAsync_WhenInternalServerErrorOccurs_ReturnsInternalServerError()
        {
            // Arrange
            var userId = "test-user-id";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5,
                        1,
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ThrowsAsync(new Exception("Database connection error"));

            // Act
            var result = await _controller.GetAllAsync(
                search: null,
                status: SD.STATUS_ALL,
                orderBy: SD.CREATED_DATE,
                sort: SD.ORDER_DESC,
                pageNumber: 1
            );

            // Assert
            var errorResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            errorResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var response = errorResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            response.ErrorMessages.Should().Contain("Internal server error");
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Test ascending order
        [InlineData(SD.ORDER_DESC)] // Test descending order
        public async Task GetAllAsync_ValidRequestWithSearchAndOrderByCreatedDate_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-tutor-id";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock data
            var mockTutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    Parent = new ApplicationUser
                    {
                        FullName = "John Doe",
                        Email = "john.doe@example.com",
                    },
                    RequestStatus = Status.APPROVE,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    Parent = new ApplicationUser
                    {
                        FullName = "Jane Doe",
                        Email = "jane.doe@example.com",
                    },
                    RequestStatus = Status.PENDING,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow,
                    Parent = new ApplicationUser
                    {
                        FullName = "Alice Smith",
                        Email = "alice.smith@example.com",
                    },
                    RequestStatus = Status.REJECT,
                },
            };

            bool isDescending = sortOrder == SD.ORDER_DESC;

            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5, // pageSize
                        1, // pageNumber
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        isDescending
                    )
                )
                .ReturnsAsync(
                    (
                        mockTutorRequests.Count,
                        isDescending
                            ? mockTutorRequests.OrderByDescending(r => r.CreatedDate).ToList()
                            : mockTutorRequests.OrderBy(r => r.CreatedDate).ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllAsync(
                "doe",
                SD.STATUS_ALL,
                SD.CREATED_DATE,
                sortOrder,
                1
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response
                .Result.Should()
                .BeOfType<List<TutorRequestDTO>>()
                .Subject.Should()
                .HaveCount(3);

            // Verify the sorting order
            var resultList = response.Result as List<TutorRequestDTO>;
            if (isDescending)
            {
                resultList.Should().BeInDescendingOrder(x => x.CreatedDate);
            }
            else
            {
                resultList.Should().BeInAscendingOrder(x => x.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Test ascending order
        [InlineData(SD.ORDER_DESC)] // Test descending order
        public async Task GetAllAsync_ValidRequestWithSearchAndOrderByNull_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-tutor-id";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock data
            var mockTutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    Parent = new ApplicationUser
                    {
                        FullName = "John Doe",
                        Email = "john.doe@example.com",
                    },
                    RequestStatus = Status.APPROVE,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    Parent = new ApplicationUser
                    {
                        FullName = "Jane Doe",
                        Email = "jane.doe@example.com",
                    },
                    RequestStatus = Status.PENDING,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow,
                    Parent = new ApplicationUser
                    {
                        FullName = "Alice Smith",
                        Email = "alice.smith@example.com",
                    },
                    RequestStatus = Status.REJECT,
                },
            };

            bool isDescending = sortOrder == SD.ORDER_DESC;

            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5, // pageSize
                        1, // pageNumber
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        isDescending
                    )
                )
                .ReturnsAsync(
                    (
                        mockTutorRequests.Count,
                        isDescending
                            ? mockTutorRequests.OrderByDescending(r => r.CreatedDate).ToList()
                            : mockTutorRequests.OrderBy(r => r.CreatedDate).ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllAsync("doe", SD.STATUS_ALL, null, sortOrder, 1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response
                .Result.Should()
                .BeOfType<List<TutorRequestDTO>>()
                .Subject.Should()
                .HaveCount(3);

            // Verify the sorting order
            var resultList = response.Result as List<TutorRequestDTO>;
            if (isDescending)
            {
                resultList.Should().BeInDescendingOrder(x => x.CreatedDate);
            }
            else
            {
                resultList.Should().BeInAscendingOrder(x => x.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Test ascending order
        [InlineData(SD.ORDER_DESC)] // Test descending order
        public async Task GetAllAsync_ValidRequestWithSearchStatusApproveAndOrderByNull_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-tutor-id";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock data
            var mockTutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    Parent = new ApplicationUser
                    {
                        FullName = "John Doe",
                        Email = "john.doe@example.com",
                    },
                    RequestStatus = Status.APPROVE,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    Parent = new ApplicationUser
                    {
                        FullName = "Jane Doe",
                        Email = "jane.doe@example.com",
                    },
                    RequestStatus = Status.PENDING,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow,
                    Parent = new ApplicationUser
                    {
                        FullName = "Alice Smith",
                        Email = "alice.smith@example.com",
                    },
                    RequestStatus = Status.APPROVE,
                },
            };

            bool isDescending = sortOrder == SD.ORDER_DESC;

            // Mock repository filtering and sorting
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5, // pageSize
                        1, // pageNumber
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        isDescending
                    )
                )
                .ReturnsAsync(
                    (
                        mockTutorRequests.Count,
                        isDescending
                            ? mockTutorRequests
                                .Where(r => r.RequestStatus == Status.APPROVE) // Filter by Approve
                                .OrderByDescending(r => r.CreatedDate)
                                .ToList()
                            : mockTutorRequests
                                .Where(r => r.RequestStatus == Status.APPROVE) // Filter by Approve
                                .OrderBy(r => r.CreatedDate)
                                .ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllAsync(
                "doe",
                SD.STATUS_APPROVE,
                null,
                sortOrder,
                1
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response
                .Result.Should()
                .BeOfType<List<TutorRequestDTO>>()
                .Subject.Should()
                .HaveCount(2); // Only Approve items

            // Verify the sorting order
            var resultList = response.Result as List<TutorRequestDTO>;
            if (isDescending)
            {
                resultList.Should().BeInDescendingOrder(x => x.CreatedDate);
            }
            else
            {
                resultList.Should().BeInAscendingOrder(x => x.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Test ascending order
        [InlineData(SD.ORDER_DESC)] // Test descending order
        public async Task GetAllAsync_ValidRequestWithSearchStatusApproveAndOrderByCreatedDate_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-tutor-id";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock data
            var mockTutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    Parent = new ApplicationUser
                    {
                        FullName = "John Doe",
                        Email = "john.doe@example.com",
                    },
                    RequestStatus = Status.APPROVE,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    Parent = new ApplicationUser
                    {
                        FullName = "Jane Doe",
                        Email = "jane.doe@example.com",
                    },
                    RequestStatus = Status.PENDING,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow,
                    Parent = new ApplicationUser
                    {
                        FullName = "Alice Smith",
                        Email = "alice.smith@example.com",
                    },
                    RequestStatus = Status.APPROVE,
                },
            };

            // Only include approved requests
            var filteredRequests = mockTutorRequests
                .Where(r => r.RequestStatus == Status.APPROVE)
                .ToList();

            bool isDescending = sortOrder == SD.ORDER_DESC;

            // Mock repository behavior
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5, // pageSize
                        1, // pageNumber
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        isDescending
                    )
                )
                .ReturnsAsync(
                    (
                        filteredRequests.Count,
                        isDescending
                            ? filteredRequests.OrderByDescending(r => r.CreatedDate).ToList()
                            : filteredRequests.OrderBy(r => r.CreatedDate).ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllAsync(
                "doe",
                SD.STATUS_APPROVE,
                SD.CREATED_DATE,
                sortOrder,
                1
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response
                .Result.Should()
                .BeOfType<List<TutorRequestDTO>>()
                .Subject.Should()
                .HaveCount(2); // Only 2 approved requests

            // Verify the sorting order
            var resultList = response.Result as List<TutorRequestDTO>;
            if (isDescending)
            {
                resultList.Should().BeInDescendingOrder(x => x.CreatedDate);
            }
            else
            {
                resultList.Should().BeInAscendingOrder(x => x.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Test ascending order
        [InlineData(SD.ORDER_DESC)] // Test descending order
        public async Task GetAllAsync_ValidRequestWithSearchStatusRejectAndOrderByCreatedDate_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-tutor-id";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock data
            var mockTutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    Parent = new ApplicationUser
                    {
                        FullName = "John Doe",
                        Email = "john.doe@example.com",
                    },
                    RequestStatus = Status.APPROVE,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    Parent = new ApplicationUser
                    {
                        FullName = "Jane Doe",
                        Email = "jane.doe@example.com",
                    },
                    RequestStatus = Status.REJECT,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow,
                    Parent = new ApplicationUser
                    {
                        FullName = "Alice Smith",
                        Email = "alice.smith@example.com",
                    },
                    RequestStatus = Status.REJECT,
                },
            };

            // Filter the data to include only "Reject" status
            var filteredRequests = mockTutorRequests
                .Where(r => r.RequestStatus == Status.REJECT)
                .ToList();

            bool isDescending = sortOrder == SD.ORDER_DESC;

            // Mock repository behavior
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5, // pageSize
                        1, // pageNumber
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        isDescending
                    )
                )
                .ReturnsAsync(
                    (
                        filteredRequests.Count,
                        isDescending
                            ? filteredRequests.OrderByDescending(r => r.CreatedDate).ToList()
                            : filteredRequests.OrderBy(r => r.CreatedDate).ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllAsync(
                "doe",
                SD.STATUS_REJECT,
                SD.CREATED_DATE,
                sortOrder,
                1
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response
                .Result.Should()
                .BeOfType<List<TutorRequestDTO>>()
                .Subject.Should()
                .HaveCount(2); // Only 2 reject requests

            // Verify the sorting order
            var resultList = response.Result as List<TutorRequestDTO>;
            if (isDescending)
            {
                resultList.Should().BeInDescendingOrder(x => x.CreatedDate);
            }
            else
            {
                resultList.Should().BeInAscendingOrder(x => x.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Test ascending order
        [InlineData(SD.ORDER_DESC)] // Test descending order
        public async Task GetAllAsync_ValidRequestWithSearchStatusRejectAndOrderByNull_ShouldReturnResultsWithoutSorting(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-tutor-id";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock data
            var mockTutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    Parent = new ApplicationUser
                    {
                        FullName = "John Doe",
                        Email = "john.doe@example.com",
                    },
                    RequestStatus = Status.REJECT,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    Parent = new ApplicationUser
                    {
                        FullName = "Jane Doe",
                        Email = "jane.doe@example.com",
                    },
                    RequestStatus = Status.REJECT,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow,
                    Parent = new ApplicationUser
                    {
                        FullName = "Alice Smith",
                        Email = "alice.smith@example.com",
                    },
                    RequestStatus = Status.REJECT,
                },
            };

            // No specific ordering field (null order)
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5, // pageSize
                        1, // pageNumber
                        It.IsAny<Expression<Func<TutorRequest, object>>>(), // No ordering field
                        sortOrder == SD.ORDER_DESC // Whether to sort descending
                    )
                )
                .ReturnsAsync((mockTutorRequests.Count, mockTutorRequests)); // Return data without sorting

            // Act
            var result = await _controller.GetAllAsync("doe", SD.STATUS_REJECT, null, sortOrder, 1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response
                .Result.Should()
                .BeOfType<List<TutorRequestDTO>>()
                .Subject.Should()
                .HaveCount(3);

            // Verify that the results are returned as is (unsorted)
            var resultList = response.Result as List<TutorRequestDTO>;
            resultList.Should().HaveCount(3); // Verify the count matches

            // Since no sorting is applied, the order should match the mock data's insertion order
            resultList[0].Parent.FullName.Should().Be("John Doe");
            resultList[1].Parent.FullName.Should().Be("Jane Doe");
            resultList[2].Parent.FullName.Should().Be("Alice Smith");
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Test ascending order
        [InlineData(SD.ORDER_DESC)] // Test descending order
        public async Task GetAllAsync_ValidRequestWithSearchStatusPendingAndOrderByNull_ShouldReturnResultsWithoutSorting(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-tutor-id";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock data
            var mockTutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    Parent = new ApplicationUser
                    {
                        FullName = "John Doe",
                        Email = "john.doe@example.com",
                    },
                    RequestStatus = Status.PENDING,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    Parent = new ApplicationUser
                    {
                        FullName = "Jane Doe",
                        Email = "jane.doe@example.com",
                    },
                    RequestStatus = Status.PENDING,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow,
                    Parent = new ApplicationUser
                    {
                        FullName = "Alice Smith",
                        Email = "alice.smith@example.com",
                    },
                    RequestStatus = Status.PENDING,
                },
            };

            // No specific ordering field (null order)
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5, // pageSize
                        1, // pageNumber
                        It.IsAny<Expression<Func<TutorRequest, object>>>(), // No ordering field
                        sortOrder == SD.ORDER_DESC // Whether to sort descending
                    )
                )
                .ReturnsAsync((mockTutorRequests.Count, mockTutorRequests)); // Return data without sorting

            // Act
            var result = await _controller.GetAllAsync(
                "doe",
                SD.STATUS_PENDING,
                null,
                sortOrder,
                1
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response
                .Result.Should()
                .BeOfType<List<TutorRequestDTO>>()
                .Subject.Should()
                .HaveCount(3);

            // Verify that the results are returned as is (unsorted)
            var resultList = response.Result as List<TutorRequestDTO>;
            resultList.Should().HaveCount(3); // Verify the count matches

            // Since no sorting is applied, the order should match the mock data's insertion order
            resultList[0].Parent.FullName.Should().Be("John Doe");
            resultList[1].Parent.FullName.Should().Be("Jane Doe");
            resultList[2].Parent.FullName.Should().Be("Alice Smith");
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Test ascending order
        [InlineData(SD.ORDER_DESC)] // Test descending order
        public async Task GetAllAsync_ValidRequestWithSearchStatusPendingAndOrderByCreatedDate_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-tutor-id";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock data with Status.PENDING
            var mockTutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    Parent = new ApplicationUser
                    {
                        FullName = "John Doe",
                        Email = "john.doe@example.com",
                    },
                    RequestStatus = Status.PENDING,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    Parent = new ApplicationUser
                    {
                        FullName = "Jane Doe",
                        Email = "jane.doe@example.com",
                    },
                    RequestStatus = Status.PENDING,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow,
                    Parent = new ApplicationUser
                    {
                        FullName = "Alice Smith",
                        Email = "alice.smith@example.com",
                    },
                    RequestStatus = Status.PENDING,
                },
            };

            bool isDescending = sortOrder == SD.ORDER_DESC;

            // Mock the repository method for GetAllWithIncludeAsync
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5, // pageSize
                        1, // pageNumber
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        isDescending
                    )
                )
                .ReturnsAsync(
                    (
                        mockTutorRequests.Count,
                        isDescending
                            ? mockTutorRequests.OrderByDescending(r => r.CreatedDate).ToList()
                            : mockTutorRequests.OrderBy(r => r.CreatedDate).ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllAsync(
                "doe",
                SD.STATUS_PENDING,
                SD.CREATED_DATE,
                sortOrder,
                1
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response
                .Result.Should()
                .BeOfType<List<TutorRequestDTO>>()
                .Subject.Should()
                .HaveCount(3);

            // Verify the sorting order based on CreatedDate
            var resultList = response.Result as List<TutorRequestDTO>;
            if (isDescending)
            {
                resultList.Should().BeInDescendingOrder(x => x.CreatedDate);
            }
            else
            {
                resultList.Should().BeInAscendingOrder(x => x.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Test ascending order
        [InlineData(SD.ORDER_DESC)] // Test descending order
        public async Task GetAllAsync_ValidRequestWithNoSearchStatusPendingAndOrderByCreatedDate_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-tutor-id";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock data with Status.PENDING
            var mockTutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    Parent = new ApplicationUser
                    {
                        FullName = "John Doe",
                        Email = "john.doe@example.com",
                    },
                    RequestStatus = Status.PENDING,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    Parent = new ApplicationUser
                    {
                        FullName = "Jane Doe",
                        Email = "jane.doe@example.com",
                    },
                    RequestStatus = Status.PENDING,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow,
                    Parent = new ApplicationUser
                    {
                        FullName = "Alice Smith",
                        Email = "alice.smith@example.com",
                    },
                    RequestStatus = Status.PENDING,
                },
            };

            bool isDescending = sortOrder == SD.ORDER_DESC;

            // Mock the repository method for GetAllWithIncludeAsync
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5, // pageSize
                        1, // pageNumber
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        isDescending
                    )
                )
                .ReturnsAsync(
                    (
                        mockTutorRequests.Count,
                        isDescending
                            ? mockTutorRequests.OrderByDescending(r => r.CreatedDate).ToList()
                            : mockTutorRequests.OrderBy(r => r.CreatedDate).ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_PENDING,
                SD.CREATED_DATE,
                sortOrder,
                1
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response
                .Result.Should()
                .BeOfType<List<TutorRequestDTO>>()
                .Subject.Should()
                .HaveCount(3);

            // Verify the sorting order based on CreatedDate
            var resultList = response.Result as List<TutorRequestDTO>;
            if (isDescending)
            {
                resultList.Should().BeInDescendingOrder(x => x.CreatedDate);
            }
            else
            {
                resultList.Should().BeInAscendingOrder(x => x.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Test ascending order
        [InlineData(SD.ORDER_DESC)] // Test descending order
        public async Task GetAllAsync_ValidRequestWithNoSearchStatusApproveAndOrderByCreatedDate_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-tutor-id";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock data with Status.APPROVE
            var mockTutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    Parent = new ApplicationUser
                    {
                        FullName = "John Doe",
                        Email = "john.doe@example.com",
                    },
                    RequestStatus = Status.APPROVE,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    Parent = new ApplicationUser
                    {
                        FullName = "Jane Doe",
                        Email = "jane.doe@example.com",
                    },
                    RequestStatus = Status.APPROVE,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow,
                    Parent = new ApplicationUser
                    {
                        FullName = "Alice Smith",
                        Email = "alice.smith@example.com",
                    },
                    RequestStatus = Status.APPROVE,
                },
            };

            bool isDescending = sortOrder == SD.ORDER_DESC;

            // Mock the repository method for GetAllWithIncludeAsync
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5, // pageSize
                        1, // pageNumber
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        isDescending
                    )
                )
                .ReturnsAsync(
                    (
                        mockTutorRequests.Count,
                        isDescending
                            ? mockTutorRequests.OrderByDescending(r => r.CreatedDate).ToList()
                            : mockTutorRequests.OrderBy(r => r.CreatedDate).ToList()
                    )
                );

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_APPROVE,
                SD.CREATED_DATE,
                sortOrder,
                1
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response
                .Result.Should()
                .BeOfType<List<TutorRequestDTO>>()
                .Subject.Should()
                .HaveCount(3);

            // Verify the sorting order based on CreatedDate
            var resultList = response.Result as List<TutorRequestDTO>;
            if (isDescending)
            {
                resultList.Should().BeInDescendingOrder(x => x.CreatedDate);
            }
            else
            {
                resultList.Should().BeInAscendingOrder(x => x.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Test ascending order
        [InlineData(SD.ORDER_DESC)] // Test descending order
        public async Task GetAllAsync_ValidRequestWithNoSearchStatusPendingAndOrderByNull_ShouldReturnResultsWithoutSorting(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-tutor-id";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock data with Status.PENDING
            var mockTutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    Parent = new ApplicationUser
                    {
                        FullName = "John Doe",
                        Email = "john.doe@example.com",
                    },
                    RequestStatus = Status.PENDING,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    Parent = new ApplicationUser
                    {
                        FullName = "Jane Doe",
                        Email = "jane.doe@example.com",
                    },
                    RequestStatus = Status.PENDING,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow,
                    Parent = new ApplicationUser
                    {
                        FullName = "Alice Smith",
                        Email = "alice.smith@example.com",
                    },
                    RequestStatus = Status.PENDING,
                },
            };

            // Mock the repository method for GetAllWithIncludeAsync with no sorting
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5, // pageSize
                        1, // pageNumber
                        It.IsAny<Expression<Func<TutorRequest, object>>>(), // No ordering field (i.e., null orderBy)
                        sortOrder == SD.ORDER_DESC // No sorting (null order or false flag for sort order)
                    )
                )
                .ReturnsAsync((mockTutorRequests.Count, mockTutorRequests)); // Return data without sorting

            // Act
            var result = await _controller.GetAllAsync(null, SD.STATUS_PENDING, null, sortOrder, 1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response
                .Result.Should()
                .BeOfType<List<TutorRequestDTO>>()
                .Subject.Should()
                .HaveCount(3);

            // Verify that the results are returned as is (unsorted)
            var resultList = response.Result as List<TutorRequestDTO>;
            resultList.Should().HaveCount(3); // Verify the count matches

            // Since no sorting is applied, the order should match the mock data's insertion order
            resultList[0].Parent.FullName.Should().Be("John Doe");
            resultList[1].Parent.FullName.Should().Be("Jane Doe");
            resultList[2].Parent.FullName.Should().Be("Alice Smith");
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Test ascending order
        [InlineData(SD.ORDER_DESC)] // Test descending order
        public async Task GetAllAsync_ValidRequestWithNoSearchStatusApprovedAndOrderByNull_ShouldReturnResultsWithoutSorting(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-tutor-id";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock data with Status.APPROVED
            var mockTutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    Parent = new ApplicationUser
                    {
                        FullName = "John Doe",
                        Email = "john.doe@example.com",
                    },
                    RequestStatus = Status.APPROVE,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    Parent = new ApplicationUser
                    {
                        FullName = "Jane Doe",
                        Email = "jane.doe@example.com",
                    },
                    RequestStatus = Status.APPROVE,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow,
                    Parent = new ApplicationUser
                    {
                        FullName = "Alice Smith",
                        Email = "alice.smith@example.com",
                    },
                    RequestStatus = Status.APPROVE,
                },
            };

            // Mock the repository method for GetAllWithIncludeAsync with no sorting
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5, // pageSize
                        1, // pageNumber
                        It.IsAny<Expression<Func<TutorRequest, object>>>(), // No ordering field (null orderBy)
                        sortOrder == SD.ORDER_DESC // No sorting (null order or false flag for sort order)
                    )
                )
                .ReturnsAsync((mockTutorRequests.Count, mockTutorRequests)); // Return data without sorting

            // Act
            var result = await _controller.GetAllAsync(null, SD.STATUS_APPROVE, null, sortOrder, 1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response
                .Result.Should()
                .BeOfType<List<TutorRequestDTO>>()
                .Subject.Should()
                .HaveCount(3);

            // Verify that the results are returned as is (unsorted)
            var resultList = response.Result as List<TutorRequestDTO>;
            resultList.Should().HaveCount(3); // Verify the count matches

            // Since no sorting is applied, the order should match the mock data's insertion order
            resultList[0].Parent.FullName.Should().Be("John Doe");
            resultList[1].Parent.FullName.Should().Be("Jane Doe");
            resultList[2].Parent.FullName.Should().Be("Alice Smith");
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Test ascending order
        [InlineData(SD.ORDER_DESC)] // Test descending order
        public async Task GetAllAsync_ValidRequestWithNoSearchStatusRejectedAndOrderByNull_ShouldReturnResultsWithoutSorting(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-tutor-id";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock data with Status.REJECTED
            var mockTutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    Parent = new ApplicationUser
                    {
                        FullName = "John Doe",
                        Email = "john.doe@example.com",
                    },
                    RequestStatus = Status.REJECT,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    Parent = new ApplicationUser
                    {
                        FullName = "Jane Doe",
                        Email = "jane.doe@example.com",
                    },
                    RequestStatus = Status.REJECT,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow,
                    Parent = new ApplicationUser
                    {
                        FullName = "Alice Smith",
                        Email = "alice.smith@example.com",
                    },
                    RequestStatus = Status.REJECT,
                },
            };

            // Mock the repository method for GetAllWithIncludeAsync with no sorting
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5, // pageSize
                        1, // pageNumber
                        It.IsAny<Expression<Func<TutorRequest, object>>>(), // No ordering field (null orderBy)
                        sortOrder == SD.ORDER_DESC // No sorting (null order or false flag for sort order)
                    )
                )
                .ReturnsAsync((mockTutorRequests.Count, mockTutorRequests)); // Return data without sorting

            // Act
            var result = await _controller.GetAllAsync(null, SD.STATUS_REJECT, null, sortOrder, 1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response
                .Result.Should()
                .BeOfType<List<TutorRequestDTO>>()
                .Subject.Should()
                .HaveCount(3);

            // Verify that the results are returned as is (unsorted)
            var resultList = response.Result as List<TutorRequestDTO>;
            resultList.Should().HaveCount(3); // Verify the count matches

            // Since no sorting is applied, the order should match the mock data's insertion order
            resultList[0].Parent.FullName.Should().Be("John Doe");
            resultList[1].Parent.FullName.Should().Be("Jane Doe");
            resultList[2].Parent.FullName.Should().Be("Alice Smith");
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Test ascending order
        [InlineData(SD.ORDER_DESC)] // Test descending order
        public async Task GetAllAsync_ValidRequestWithNoSearchStatusRejectedAndOrderByCreatedDate_ShouldReturnResultsSortedByCreatedDate(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-tutor-id";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock data with Status.REJECTED
            var mockTutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    Parent = new ApplicationUser
                    {
                        FullName = "John Doe",
                        Email = "john.doe@example.com",
                    },
                    RequestStatus = Status.REJECT,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    Parent = new ApplicationUser
                    {
                        FullName = "Jane Doe",
                        Email = "jane.doe@example.com",
                    },
                    RequestStatus = Status.REJECT,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow,
                    Parent = new ApplicationUser
                    {
                        FullName = "Alice Smith",
                        Email = "alice.smith@example.com",
                    },
                    RequestStatus = Status.REJECT,
                },
            };

            // Determine whether to sort ascending or descending
            bool sortDescending = sortOrder == SD.ORDER_DESC;
            var sortedList = sortDescending
                ? mockTutorRequests.OrderByDescending(x => x.CreatedDate).ToList()
                : mockTutorRequests.OrderBy(x => x.CreatedDate).ToList();

            // Mock the repository method for GetAllWithIncludeAsync with sorting by CreatedDate
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        5, // pageSize
                        1, // pageNumber
                        It.Is<Expression<Func<TutorRequest, object>>>(x =>
                            x.Body.ToString().Contains("CreatedDate")
                        ), // Sort by CreatedDate
                        sortDescending
                    )
                )
                .ReturnsAsync((mockTutorRequests.Count, sortedList)); // Return data sorted by CreatedDate

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_REJECT,
                "CreatedDate",
                sortOrder,
                1
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response
                .Result.Should()
                .BeOfType<List<TutorRequestDTO>>()
                .Subject.Should()
                .HaveCount(3);

            // Verify the results are sorted by CreatedDate
            var resultList = response.Result as List<TutorRequestDTO>;
            resultList.Should().HaveCount(3); // Verify the count matches

            if (sortOrder == SD.ORDER_ASC)
            {
                // Verify ascending order
                resultList[0].Parent.FullName.Should().Be("John Doe");
                resultList[1].Parent.FullName.Should().Be("Jane Doe");
                resultList[2].Parent.FullName.Should().Be("Alice Smith");
            }
            else
            {
                // Verify descending order
                resultList[0].Parent.FullName.Should().Be("Alice Smith");
                resultList[1].Parent.FullName.Should().Be("Jane Doe");
                resultList[2].Parent.FullName.Should().Be("John Doe");
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Test ascending order
        [InlineData(SD.ORDER_DESC)] // Test descending order
        public async Task GetAllAsync_ValidRequestWithNoSearchStatusAllAndOrderByCreatedDate_ShouldReturnResultsSortedByCreatedDate(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-tutor-id";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock data with mixed statuses
            var mockTutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    Parent = new ApplicationUser
                    {
                        FullName = "John Doe",
                        Email = "john.doe@example.com",
                    },
                    RequestStatus = Status.PENDING,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    Parent = new ApplicationUser
                    {
                        FullName = "Jane Doe",
                        Email = "jane.doe@example.com",
                    },
                    RequestStatus = Status.APPROVE,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow,
                    Parent = new ApplicationUser
                    {
                        FullName = "Alice Smith",
                        Email = "alice.smith@example.com",
                    },
                    RequestStatus = Status.REJECT,
                },
            };

            // Determine whether to sort ascending or descending
            bool sortDescending = sortOrder == SD.ORDER_DESC;
            var sortedList = sortDescending
                ? mockTutorRequests.OrderByDescending(x => x.CreatedDate).ToList()
                : mockTutorRequests.OrderBy(x => x.CreatedDate).ToList();
            // Mock the repository method for GetAllWithIncludeAsync with sorting by CreatedDate
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(), // No filtering by status (get all statuses)
                        It.IsAny<string>(),
                        5, // pageSize
                        1, // pageNumber
                        It.IsAny<Expression<Func<TutorRequest, object>>>(), // Sort by CreatedDate
                        sortDescending
                    )
                )
                .ReturnsAsync((mockTutorRequests.Count, sortedList)); // Return data sorted by CreatedDate

            // Act
            var result = await _controller.GetAllAsync(
                null,
                SD.STATUS_ALL,
                SD.CREATED_DATE,
                sortOrder,
                1
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response
                .Result.Should()
                .BeOfType<List<TutorRequestDTO>>()
                .Subject.Should()
                .HaveCount(3);

            // Verify the results are sorted by CreatedDate
            var resultList = response.Result as List<TutorRequestDTO>;
            resultList.Should().HaveCount(3); // Verify the count matches

            if (sortOrder == SD.ORDER_ASC)
            {
                // Verify ascending order
                resultList[0].Parent.FullName.Should().Be("John Doe");
                resultList[1].Parent.FullName.Should().Be("Jane Doe");
                resultList[2].Parent.FullName.Should().Be("Alice Smith");
            }
            else
            {
                // Verify descending order
                resultList[0].Parent.FullName.Should().Be("Alice Smith");
                resultList[1].Parent.FullName.Should().Be("Jane Doe");
                resultList[2].Parent.FullName.Should().Be("John Doe");
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Test ascending order
        [InlineData(SD.ORDER_DESC)] // Test descending order
        public async Task GetAllAsync_ValidRequestWithNoSearchStatusAllAndOrderByNull_ShouldReturnResultsSortedByCreatedDate(
            string sortOrder
        )
        {
            // Arrange
            var userId = "test-tutor-id";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock data with various request statuses (all statuses included)
            var mockTutorRequests = new List<TutorRequest>
            {
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    Parent = new ApplicationUser
                    {
                        FullName = "John Doe",
                        Email = "john.doe@example.com",
                    },
                    RequestStatus = Status.PENDING,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    Parent = new ApplicationUser
                    {
                        FullName = "Jane Doe",
                        Email = "jane.doe@example.com",
                    },
                    RequestStatus = Status.APPROVE,
                },
                new TutorRequest
                {
                    TutorId = userId,
                    CreatedDate = DateTime.UtcNow,
                    Parent = new ApplicationUser
                    {
                        FullName = "Alice Smith",
                        Email = "alice.smith@example.com",
                    },
                    RequestStatus = Status.REJECT,
                },
            };

            // Determine whether to sort ascending or descending
            bool sortDescending = sortOrder == SD.ORDER_DESC;
            var sortedList = sortDescending
                ? mockTutorRequests.OrderByDescending(x => x.CreatedDate).ToList()
                : mockTutorRequests.OrderBy(x => x.CreatedDate).ToList();
            // Mock repository method to return data sorted by CreatedDate
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(), // No filtering by status (get all statuses)
                        It.IsAny<string>(),
                        5, // pageSize
                        1, // pageNumber
                        It.IsAny<Expression<Func<TutorRequest, object>>>(), // Sort by CreatedDate
                        sortDescending
                    )
                )
                .ReturnsAsync((mockTutorRequests.Count, sortedList)); // Return data sorted by CreatedDate

            // Act
            var result = await _controller.GetAllAsync(null, SD.STATUS_ALL, null, sortOrder, 1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response
                .Result.Should()
                .BeOfType<List<TutorRequestDTO>>()
                .Subject.Should()
                .HaveCount(3);

            // Verify the results are sorted by CreatedDate
            var resultList = response.Result as List<TutorRequestDTO>;
            resultList.Should().HaveCount(3); // Verify the count matches

            if (sortOrder == SD.ORDER_ASC)
            {
                // Verify ascending order
                resultList[0].Parent.FullName.Should().Be("John Doe");
                resultList[1].Parent.FullName.Should().Be("Jane Doe");
                resultList[2].Parent.FullName.Should().Be("Alice Smith");
            }
            else
            {
                // Verify descending order
                resultList[0].Parent.FullName.Should().Be("Alice Smith");
                resultList[1].Parent.FullName.Should().Be("Jane Doe");
                resultList[2].Parent.FullName.Should().Be("John Doe");
            }
        }

        [Fact]
        public async Task CreateAsync_AuthenticationFailure_ShouldReturnUnauthorized()
        {
            // Arrange
            var tutorRequestCreateDTO = new TutorRequestCreateDTO
            {
                ChildId = 1,
                TutorId = "tutor-id",
            };
            _mockResourceService
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");
            // Simulate an unauthenticated user
            var claims = new List<Claim>(); // No NameIdentifier
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Act
            var result = await _controller.CreateAsync(tutorRequestCreateDTO);

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
        public async Task CreateAsync_AuthorizationFailure_ShouldReturnForbidden()
        {
            // Arrange
            var tutorRequestCreateDTO = new TutorRequestCreateDTO
            {
                ChildId = 1,
                TutorId = "tutor-id",
            };
            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbiden access.");
            // Simulate a user without the "Parent" role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(
                    ClaimTypes.Role,
                    "OtherRole"
                ) // Not "ParentRole"
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Act
            var result = await _controller.CreateAsync(tutorRequestCreateDTO);

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
            var tutorRequestCreateDTO = new TutorRequestCreateDTO
            {
                ChildId = 1,
                TutorId = "tutor-id",
            };

            // Simulate a valid user (Parent role)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.PARENT_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");

            // Simulate an exception thrown during the process
            _mockTutorRequestRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<TutorRequest>()))
                .ThrowsAsync(new Exception("Database error"));
            _mockUserRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>()
                    )
                )
                .ThrowsAsync(new Exception("Database error"));
            // Act
            var result = await _controller.CreateAsync(tutorRequestCreateDTO);
            var internalServerErrorResult = result.Result as ObjectResult;
            // Assert
            internalServerErrorResult
                .StatusCode.Should()
                .Be((int)HttpStatusCode.InternalServerError);

            var response = internalServerErrorResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            response.ErrorMessages.Should().Contain("Internal server error");
        }

        [Fact]
        public async Task CreateAsync_ModelStateInvalid_ShouldReturnBadRequest()
        {
            // Arrange
            var tutorRequestCreateDTO = new TutorRequestCreateDTO
            {
                // Simulating invalid input, for example, an invalid or missing required field
                ChildId = 0, // Assuming ChildId is required and must be greater than 0
                TutorId = "tutor-id",
            };

            // Simulate a valid user with the "Parent" role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.PARENT_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.TUTOR_REQUEST))
                .Returns("Tutor request invalid.");

            // Manually adding a validation error to the ModelState (simulate invalid model state)
            _controller.ModelState.AddModelError(
                "ChildId",
                "ChildId is required and must be greater than 0"
            );

            // Act
            var result = await _controller.CreateAsync(tutorRequestCreateDTO);
            var badRequestResult = result.Result as ObjectResult;

            // Assert
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.ErrorMessages.Should().Contain("Tutor request invalid.");
        }

        [Fact]
        public async Task CreateAsync_ChildIdInvalid_ShouldReturnNotFound()
        {
            // Arrange
            var tutorRequestCreateDTO = new TutorRequestCreateDTO
            {
                ChildId = 0, // Invalid ChildId, should trigger NotFound
                TutorId = "tutor-id",
            };

            // Simulate a valid user with the "Parent" role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.PARENT_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            _mockResourceService
                .Setup(r => r.GetString(It.IsAny<string>(), It.IsAny<string>()))
                .Returns("Child not found.");
            // Act
            var result = await _controller.CreateAsync(tutorRequestCreateDTO);
            var notFoundResult = result.Result as ObjectResult;

            // Assert
            notFoundResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            var response = notFoundResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ErrorMessages.Should().Contain("Child not found.");
        }

        [Fact]
        public async Task CreateAsync_DuplicateRequest_ShouldReturnBadRequest()
        {
            // Arrange
            var tutorRequestCreateDTO = new TutorRequestCreateDTO
            {
                ChildId = 1, // Valid ChildId
                TutorId = "tutor-id",
            };

            // Simulate a valid user with the "Parent" role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.PARENT_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock the repository to return a list indicating an existing request
            var existingRequest = new List<TutorRequest>
            {
                new TutorRequest
                {
                    ParentId = "test-user-id",
                    TutorId = "tutor-id",
                    ChildId = 1,
                    RequestStatus = SD.Status.PENDING,
                },
            };
            _mockTutorRequestRepository
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((existingRequest.Count, existingRequest));

            // Mock the resource service to return the duplication error message
            _mockResourceService
                .Setup(r => r.GetString(It.IsAny<string>(), It.IsAny<string>()))
                .Returns("Duplicate tutor request.");

            // Act
            var result = await _controller.CreateAsync(tutorRequestCreateDTO);
            var badRequestResult = result.Result as ObjectResult;

            // Assert
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            var response = badRequestResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.ErrorMessages.Should().Contain("Duplicate tutor request.");
        }

        [Fact]
        public async Task CreateAsync_TutorNotFound_ShouldReturnNotFound()
        {
            // Arrange
            var tutorRequestCreateDTO = new TutorRequestCreateDTO
            {
                ChildId = 1, // Valid ChildId
                TutorId =
                    "non-existing-tutor-id" // Invalid TutorId, should trigger NotFound
                ,
            };

            // Simulate a valid user with the "Parent" role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.PARENT_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock the repository to return null indicating the tutor does not exist
            _mockUserRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((ApplicationUser)null); // No tutor found

            // Mock the resource service to return the error message
            _mockResourceService
                .Setup(r => r.GetString(It.IsAny<string>(), It.IsAny<string>()))
                .Returns("Tutor not found.");

            // Act
            var result = await _controller.CreateAsync(tutorRequestCreateDTO);
            var notFoundResult = result.Result as ObjectResult;

            // Assert
            notFoundResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            var response = notFoundResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ErrorMessages.Should().Contain("Tutor not found.");
        }

        [Fact]
        public async Task CreateAsync_ChildNotFound_ShouldReturnNotFound()
        {
            // Arrange
            var tutorRequestCreateDTO = new TutorRequestCreateDTO
            {
                ChildId = 1, // Valid ChildId
                TutorId = "tutor-id",
            };

            // Simulate a valid user with the "Parent" role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.PARENT_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock the repository to return null indicating the child does not exist for the given parent
            _mockChildInformationRepository
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((ChildInformation)null); // No child found for the provided ParentId and ChildId
            _mockUserRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new ApplicationUser());
            // Mock the resource service to return the error message
            _mockResourceService
                .Setup(r => r.GetString(It.IsAny<string>(), It.IsAny<string>()))
                .Returns("Child not found.");

            // Act
            var result = await _controller.CreateAsync(tutorRequestCreateDTO);
            var notFoundResult = result.Result as ObjectResult;

            // Assert
            notFoundResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            var response = notFoundResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ErrorMessages.Should().Contain("Child not found.");
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_ShouldReturnSuccessWithSendingEmail()
        {
            // Arrange
            var tutorRequestCreateDTO = new TutorRequestCreateDTO
            {
                ChildId = 1,
                TutorId = "tutor-id",
            };

            // Simulate a valid user with the "Parent" role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.PARENT_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock the repository to return a valid child
            var child = new ChildInformation { Id = 1, ParentId = "test-user-id" };
            _mockChildInformationRepository
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(child);

            // Mock the repository to return a valid tutor
            var tutor = new ApplicationUser { Id = "tutor-id" };
            _mockUserRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(tutor);

            // Mock the repository to indicate no duplicate requests exist
            _mockTutorRequestRepository
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((0, new List<TutorRequest>()));

            // Mock the creation of the tutor request
            var createdTutorRequest = new TutorRequest
            {
                ParentId = "test-user-id",
                ChildId = 1,
                TutorId = "tutor-id",
                RequestStatus = SD.Status.PENDING,
            };
            _mockTutorRequestRepository
                .Setup(r => r.CreateAsync(It.IsAny<TutorRequest>()))
                .ReturnsAsync(createdTutorRequest);

            // Act
            var result = await _controller.CreateAsync(tutorRequestCreateDTO);
            var okResult = result.Result as ObjectResult;

            // Assert
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_ShouldReturnSuccessWithoutSendingEmail()
        {
            // Arrange
            var tutorRequestCreateDTO = new TutorRequestCreateDTO
            {
                ChildId = 1,
                TutorId = "tutor-id",
            };

            // Simulate a valid user with the "Parent" role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.PARENT_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock the repository to return a valid child
            var child = new ChildInformation { Id = 1, ParentId = "test-user-id" };
            _mockChildInformationRepository
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(child);

            // Mock the repository to return a valid tutor
            var tutor = new ApplicationUser { Id = "tutor-id" };
            _mockUserRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(tutor);

            // Mock the repository to indicate no duplicate requests exist
            _mockTutorRequestRepository
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<TutorRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((0, new List<TutorRequest>()));

            // Mock the creation of the tutor request
            var createdTutorRequest = new TutorRequest
            {
                ParentId = "test-user-id",
                ChildId = 1,
                TutorId = "tutor-id",
                RequestStatus = SD.Status.PENDING,
            };
            _mockTutorRequestRepository
                .Setup(r => r.CreateAsync(It.IsAny<TutorRequest>()))
                .ReturnsAsync(createdTutorRequest);

            // Mock the email service to ensure no email is sent

            // Act
            var result = await _controller.CreateAsync(tutorRequestCreateDTO);
            var okResult = result.Result as ObjectResult;

            // Assert
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task UpdateStatusRequest_AuthenticationFailure_ShouldReturnUnauthorizedResponse()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusTutorRequestDTO
            {
                Id = 1,
                StatusChange = (int)Status.APPROVE,
            };
            _mockResourceService
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext() // No user context is added
                ,
            };

            // Act
            var result = await _controller.UpdateStatusRequest(1, changeStatusDTO);
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
        public async Task UpdateStatusRequest_AuthorizationFailure_ShouldReturnForbiddenResponse()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusTutorRequestDTO
            {
                Id = 1,
                StatusChange = (int)Status.APPROVE,
            };
            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbiden access.");
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "valid-user-id"),
                new Claim(
                    ClaimTypes.Role,
                    SD.PARENT_ROLE
                ) // Invalid role for this action
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Act
            var result = await _controller.UpdateStatusRequest(1, changeStatusDTO);
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
        public async Task UpdateStatusRequest_InternalServerError_ShouldReturnInternalServerErrorResponse()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusTutorRequestDTO
            {
                Id = 1,
                StatusChange = (int)Status.APPROVE,
            };
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "valid-user-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock the repository to throw an exception
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ThrowsAsync(new Exception("Database connection error"));

            // Act
            var result = await _controller.UpdateStatusRequest(1, changeStatusDTO);

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
        public async Task UpdateStatusRequest_BadRequest_ShouldReturnBadRequestResponse()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusTutorRequestDTO
            {
                Id = 1,
                StatusChange = (int)
                    SD.Status.PENDING // Invalid status change
                ,
            };

            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.TUTOR_REQUEST))
                .Returns("Bad request for tutor request.");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "valid-user-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Simulate an invalid model state
            _controller.ModelState.AddModelError("StatusChange", "Invalid status change value.");

            // Act
            var result = await _controller.UpdateStatusRequest(1, changeStatusDTO);

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;

            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.ErrorMessages.Should().Contain("Bad request for tutor request.");
        }

        [Fact]
        public async Task UpdateStatusRequest_ModelNotFoundOrInvalidStatus_ShouldReturnBadRequestResponse()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusTutorRequestDTO
            {
                Id = 1,
                StatusChange = (int)
                    SD.Status.APPROVE // Valid status change
                ,
            };

            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.TUTOR_REQUEST))
                .Returns("Bad request for tutor request.");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "valid-user-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock the repository to return null (model not found)
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((TutorRequest)null);

            // Act
            var result = await _controller.UpdateStatusRequest(1, changeStatusDTO);

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;

            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.ErrorMessages.Should().Contain("Bad request for tutor request.");
        }

        [Fact]
        public async Task UpdateStatusRequest_ValidRequestStatusApprovedButNoMailSent_ShouldReturnOkResponse()
        {
            // Arrange
            var tutor = new ApplicationUser { Id = "valid-tutor-id", FullName = "Tutor Name" };
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "valid-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            var parent = new ApplicationUser
            {
                Id = "valid-parent-id",
                Email = "parent@example.com",
                FullName = "Parent Name",
            };

            var tutorRequest = new TutorRequest
            {
                Id = 1,
                TutorId = "valid-tutor-id",
                ParentId = "valid-parent-id",
                RequestStatus = SD.Status.PENDING,
                Parent = parent,
            };

            var changeStatusDTO = new ChangeStatusTutorRequestDTO
            {
                Id = 1,
                StatusChange = (int)SD.Status.APPROVE,
            };

            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<bool>(),
                        "Parent,Tutor",
                        null
                    )
                )
                .ReturnsAsync(tutorRequest);

            _mockUserRepository
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), false, null)
                )
                .ReturnsAsync(tutor);

            // Simulate missing email template file
            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Templates",
                "ChangeStatusTemplate.cshtml"
            );

            _mockResourceService
                .Setup(r =>
                    r.GetString(
                        SD.CHANGE_STATUS_TUTOR_REQUEST_PARENT_NOTIFICATION,
                        SD.STATUS_APPROVE_VIE,
                        tutor.FullName
                    )
                )
                .Returns($"Yêu cầu dạy học của bạn đến gia sư {tutor.FullName} đã được chấp nhận");

            // Act
            var result = await _controller.UpdateStatusRequest(1, changeStatusDTO);

            // Assert
            var okResult = result.Result as OkObjectResult;

            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify the repository was updated
            _mockTutorRequestRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<TutorRequest>()),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateStatusRequest_ValidRequestStatusApprovedAndMailSent_ShouldReturnOkResponse()
        {
            // Arrange
            var tutor = new ApplicationUser { Id = "valid-tutor-id", FullName = "Tutor Name" };
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "valid-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            var parent = new ApplicationUser
            {
                Id = "valid-parent-id",
                Email = "parent@example.com",
                FullName = "Parent Name",
            };

            var tutorRequest = new TutorRequest
            {
                Id = 1,
                TutorId = "valid-tutor-id",
                ParentId = "valid-parent-id",
                RequestStatus = SD.Status.PENDING,
                Parent = parent,
            };

            var changeStatusDTO = new ChangeStatusTutorRequestDTO
            {
                Id = 1,
                StatusChange = (int)SD.Status.APPROVE,
            };

            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<bool>(),
                        "Parent,Tutor",
                        null
                    )
                )
                .ReturnsAsync(tutorRequest);

            _mockUserRepository
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), false, null)
                )
                .ReturnsAsync(tutor);

            // Simulate email template file exists
            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Templates",
                "ChangeStatusTemplate.cshtml"
            );

            _mockResourceService
                .Setup(r =>
                    r.GetString(
                        SD.CHANGE_STATUS_TUTOR_REQUEST_PARENT_NOTIFICATION,
                        SD.STATUS_APPROVE_VIE,
                        tutor.FullName
                    )
                )
                .Returns($"Yêu cầu dạy học của bạn đến gia sư {tutor.FullName} đã được chấp nhận");

            // Act
            var result = await _controller.UpdateStatusRequest(1, changeStatusDTO);

            // Assert
            var okResult = result.Result as OkObjectResult;

            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task UpdateStatusRequest_ValidRequestStatusRejectedWithSchedulingConflicts_ShouldReturnOkResponseWithoutSendingMail()
        {
            // Arrange
            var tutor = new ApplicationUser { Id = "valid-tutor-id", FullName = "Tutor Name" };
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "valid-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            var parent = new ApplicationUser
            {
                Id = "valid-parent-id",
                Email = "parent@example.com",
                FullName = "Parent Name",
            };

            var tutorRequest = new TutorRequest
            {
                Id = 1,
                TutorId = "valid-tutor-id",
                ParentId = "valid-parent-id",
                RequestStatus = SD.Status.PENDING,
                Parent = parent,
            };

            var changeStatusDTO = new ChangeStatusTutorRequestDTO
            {
                Id = 1,
                StatusChange = (int)SD.Status.REJECT,
                RejectType = SD.RejectType.SchedulingConflicts,
                RejectionReason = "reason",
            };

            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<bool>(),
                        "Parent,Tutor",
                        null
                    )
                )
                .ReturnsAsync(tutorRequest);

            _mockUserRepository
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), false, null)
                )
                .ReturnsAsync(tutor);

            // Act
            var result = await _controller.UpdateStatusRequest(1, changeStatusDTO);

            // Assert
            var okResult = result.Result as OkObjectResult;

            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify the repository was updated
            _mockTutorRequestRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<TutorRequest>()),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateStatusRequest_ValidRequestStatusRejectedWithSchedulingConflicts_ShouldReturnOkResponseWithSendingMail()
        {
            // Arrange
            var tutor = new ApplicationUser { Id = "valid-tutor-id", FullName = "Tutor Name" };
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "valid-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            var parent = new ApplicationUser
            {
                Id = "valid-parent-id",
                Email = "parent@example.com",
                FullName = "Parent Name",
            };

            var tutorRequest = new TutorRequest
            {
                Id = 1,
                TutorId = "valid-tutor-id",
                ParentId = "valid-parent-id",
                RequestStatus = SD.Status.PENDING,
                Parent = parent,
            };

            var changeStatusDTO = new ChangeStatusTutorRequestDTO
            {
                Id = 1,
                StatusChange = (int)SD.Status.REJECT,
                RejectType = SD.RejectType.SchedulingConflicts,
                RejectionReason = "reason",
            };

            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<bool>(),
                        "Parent,Tutor",
                        null
                    )
                )
                .ReturnsAsync(tutorRequest);

            _mockUserRepository
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), false, null)
                )
                .ReturnsAsync(tutor);

            // Simulate email template file exists
            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Templates",
                "ChangeStatusTemplate.cshtml"
            );

            _mockResourceService
                .Setup(r =>
                    r.GetString(
                        SD.CHANGE_STATUS_TUTOR_REQUEST_PARENT_NOTIFICATION,
                        SD.STATUS_REJECT_VIE,
                        tutor.FullName
                    )
                )
                .Returns(
                    $"Yêu cầu dạy học của bạn đến gia sư {tutor.FullName} đã bị từ chối với lý do: {changeStatusDTO.RejectionReason}"
                );

            // Act
            var result = await _controller.UpdateStatusRequest(1, changeStatusDTO);

            // Assert
            var okResult = result.Result as OkObjectResult;

            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify the repository was updated
            _mockTutorRequestRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<TutorRequest>()),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateStatusRequest_ValidRequestStatusRejectedWithIncompatibilityWithCurriculum_ShouldReturnOkResponseAndSendMail()
        {
            // Arrange
            var tutor = new ApplicationUser { Id = "valid-tutor-id", FullName = "Tutor Name" };
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "valid-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            var parent = new ApplicationUser
            {
                Id = "valid-parent-id",
                Email = "parent@example.com",
                FullName = "Parent Name",
            };

            var tutorRequest = new TutorRequest
            {
                Id = 1,
                TutorId = "valid-tutor-id",
                ParentId = "valid-parent-id",
                RequestStatus = SD.Status.PENDING,
                Parent = parent,
            };

            var changeStatusDTO = new ChangeStatusTutorRequestDTO
            {
                Id = 1,
                StatusChange = (int)SD.Status.REJECT,
                RejectType = SD.RejectType.IncompatibilityWithCurriculum, // Reject due to incompatibility with curriculum
                RejectionReason = "Curriculum incompatibility reason",
            };

            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<bool>(),
                        "Parent,Tutor",
                        null
                    )
                )
                .ReturnsAsync(tutorRequest);

            _mockUserRepository
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), false, null)
                )
                .ReturnsAsync(tutor);

            // Simulate email template file exists
            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Templates",
                "ChangeStatusTemplate.cshtml"
            );

            _mockResourceService
                .Setup(r =>
                    r.GetString(
                        SD.CHANGE_STATUS_TUTOR_REQUEST_PARENT_NOTIFICATION,
                        SD.STATUS_REJECT_VIE,
                        tutor.FullName
                    )
                )
                .Returns(
                    $"Yêu cầu dạy học của bạn đến gia sư {tutor.FullName} đã bị từ chối với lý do: {changeStatusDTO.RejectionReason}"
                );

            // Act
            var result = await _controller.UpdateStatusRequest(1, changeStatusDTO);

            // Assert
            var okResult = result.Result as OkObjectResult;

            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify the repository was updated
            _mockTutorRequestRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<TutorRequest>()),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateStatusRequest_ValidRequestStatusRejectedWithIncompatibilityWithCurriculum_ShouldReturnOkResponseAndNotSendMail()
        {
            // Arrange
            var tutor = new ApplicationUser { Id = "valid-tutor-id", FullName = "Tutor Name" };
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "valid-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            var parent = new ApplicationUser
            {
                Id = "valid-parent-id",
                Email = "parent@example.com",
                FullName = "Parent Name",
            };

            var tutorRequest = new TutorRequest
            {
                Id = 1,
                TutorId = "valid-tutor-id",
                ParentId = "valid-parent-id",
                RequestStatus = SD.Status.PENDING,
                Parent = parent,
            };

            var changeStatusDTO = new ChangeStatusTutorRequestDTO
            {
                Id = 1,
                StatusChange = (int)SD.Status.REJECT,
                RejectType = SD.RejectType.IncompatibilityWithCurriculum, // Reject due to curriculum incompatibility
                RejectionReason = "Curriculum incompatibility reason",
            };

            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<bool>(),
                        "Parent,Tutor",
                        null
                    )
                )
                .ReturnsAsync(tutorRequest);

            _mockUserRepository
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), false, null)
                )
                .ReturnsAsync(tutor);

            // Simulate email template file exists (but we won't send an email in this case)
            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Templates",
                "ChangeStatusTemplate.cshtml"
            );

            _mockResourceService
                .Setup(r =>
                    r.GetString(
                        SD.CHANGE_STATUS_TUTOR_REQUEST_PARENT_NOTIFICATION,
                        SD.STATUS_REJECT_VIE,
                        tutor.FullName
                    )
                )
                .Returns(
                    $"Yêu cầu dạy học của bạn đến gia sư {tutor.FullName} đã bị từ chối với lý do: {changeStatusDTO.RejectionReason}"
                );

            // Act
            var result = await _controller.UpdateStatusRequest(1, changeStatusDTO);

            // Assert
            var okResult = result.Result as OkObjectResult;

            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify the repository was updated
            _mockTutorRequestRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<TutorRequest>()),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateStatusRequest_ValidRequestStatusRejectedWithOtherReason_ShouldReturnOkResponseAndNotSendMail()
        {
            // Arrange
            var tutor = new ApplicationUser { Id = "valid-tutor-id", FullName = "Tutor Name" };
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "valid-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            var parent = new ApplicationUser
            {
                Id = "valid-parent-id",
                Email = "parent@example.com",
                FullName = "Parent Name",
            };

            var tutorRequest = new TutorRequest
            {
                Id = 1,
                TutorId = "valid-tutor-id",
                ParentId = "valid-parent-id",
                RequestStatus = SD.Status.PENDING,
                Parent = parent,
            };

            var changeStatusDTO = new ChangeStatusTutorRequestDTO
            {
                Id = 1,
                StatusChange = (int)SD.Status.REJECT,
                RejectType = SD.RejectType.Other, // Reject due to some other reason
                RejectionReason = "Other reason",
            };

            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<bool>(),
                        "Parent,Tutor",
                        null
                    )
                )
                .ReturnsAsync(tutorRequest);

            _mockUserRepository
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), false, null)
                )
                .ReturnsAsync(tutor);

            // Simulate email template file exists (but no email will be sent in this case)
            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Templates",
                "ChangeStatusTemplate.cshtml"
            );

            _mockResourceService
                .Setup(r =>
                    r.GetString(
                        SD.CHANGE_STATUS_TUTOR_REQUEST_PARENT_NOTIFICATION,
                        SD.STATUS_REJECT_VIE,
                        tutor.FullName
                    )
                )
                .Returns(
                    $"Yêu cầu dạy học của bạn đến gia sư {tutor.FullName} đã bị từ chối với lý do: {changeStatusDTO.RejectionReason}"
                );

            // Act
            var result = await _controller.UpdateStatusRequest(1, changeStatusDTO);

            // Assert
            var okResult = result.Result as OkObjectResult;

            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify the repository was updated
            _mockTutorRequestRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<TutorRequest>()),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateStatusRequest_ValidRequestStatusRejectedWithOtherReason_ShouldReturnOkResponseWithSendingMail()
        {
            // Arrange
            var tutor = new ApplicationUser { Id = "valid-tutor-id", FullName = "Tutor Name" };
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "valid-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            var parent = new ApplicationUser
            {
                Id = "valid-parent-id",
                Email = "parent@example.com",
                FullName = "Parent Name",
            };

            var tutorRequest = new TutorRequest
            {
                Id = 1,
                TutorId = "valid-tutor-id",
                ParentId = "valid-parent-id",
                RequestStatus = SD.Status.PENDING,
                Parent = parent,
            };

            var changeStatusDTO = new ChangeStatusTutorRequestDTO
            {
                Id = 1,
                StatusChange = (int)SD.Status.REJECT,
                RejectType = SD.RejectType.Other, // Reject due to some other reason
                RejectionReason = "Other reason",
            };

            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<bool>(),
                        "Parent,Tutor",
                        null
                    )
                )
                .ReturnsAsync(tutorRequest);

            _mockUserRepository
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), false, null)
                )
                .ReturnsAsync(tutor);

            // Simulate email template file exists
            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Templates",
                "ChangeStatusTemplate.cshtml"
            );

            _mockResourceService
                .Setup(r =>
                    r.GetString(
                        SD.CHANGE_STATUS_TUTOR_REQUEST_PARENT_NOTIFICATION,
                        SD.STATUS_REJECT_VIE,
                        tutor.FullName
                    )
                )
                .Returns(
                    $"Yêu cầu dạy học của bạn đến gia sư {tutor.FullName} đã bị từ chối với lý do: {changeStatusDTO.RejectionReason}"
                );

            // Act
            var result = await _controller.UpdateStatusRequest(1, changeStatusDTO);

            // Assert
            var okResult = result.Result as OkObjectResult;

            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify the repository was updated
            _mockTutorRequestRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<TutorRequest>()),
                Times.Once
            );
        }



    }
}
