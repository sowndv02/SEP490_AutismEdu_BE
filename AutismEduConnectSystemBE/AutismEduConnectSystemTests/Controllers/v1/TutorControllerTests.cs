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
using AutismEduConnectSystem.RabbitMQSender;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.SignalR;
using AutismEduConnectSystem.Utils;
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
    public class TutorControllerTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ITutorRepository> _mockTutorRepository;
        private readonly Mock<ITutorRequestRepository> _mockTutorRequestRepository;
        private readonly Mock<ITutorProfileUpdateRequestRepository> _mockTutorProfileUpdateRequestRepository;
        private readonly IMapper _mockMapper;
        private readonly Mock<ILogger<TutorController>> _mockLogger;
        private readonly Mock<IResourceService> _mockResourceService;
        private readonly Mock<IRabbitMQMessageSender> _mockMessageBus;
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly Mock<INotificationRepository> _mockNotificationRepository;
        private readonly TutorController _controller;
        private readonly Mock<IConfiguration> _mockConfiguration;

        public TutorControllerTests()
        {
            // Initialize mocks
            _mockUserRepository = new Mock<IUserRepository>();
            _mockTutorRepository = new Mock<ITutorRepository>();
            _mockTutorRequestRepository = new Mock<ITutorRequestRepository>();
            _mockTutorProfileUpdateRequestRepository =
                new Mock<ITutorProfileUpdateRequestRepository>();
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingConfig());
            });
            _mockMapper = config.CreateMapper();
            _mockLogger = new Mock<ILogger<TutorController>>();
            _mockResourceService = new Mock<IResourceService>();
            _mockMessageBus = new Mock<IRabbitMQMessageSender>();
            _mockHubContext = new Mock<IHubContext<NotificationHub>>();
            _mockNotificationRepository = new Mock<INotificationRepository>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(c => c["APIConfig:PageSize"]).Returns("10");
            _mockConfiguration.Setup(c => c["RabbitMQSettings:QueueName"]).Returns("testQueue");
            // Initialize the controller with the mocked dependencies
            _controller = new TutorController(
                _mockUserRepository.Object,
                _mockTutorRepository.Object,
                _mockMapper,
                _mockConfiguration.Object, // mock configuration
                new FormatString(),
                _mockTutorProfileUpdateRequestRepository.Object,
                _mockTutorRequestRepository.Object,
                _mockResourceService.Object,
                _mockLogger.Object,
                _mockHubContext.Object,
                _mockNotificationRepository.Object,
                _mockMessageBus.Object
            );
        }

        [Fact]
        public async Task GetAllAsync_ReturnsUnauthorized_WhenUserIsUnauthorized()
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

            // Act
            var result = await _controller.GetAllUpdateRequestProfileAsync(
                search: null,
                status: SD.STATUS_ALL,
                orderBy: SD.CREATED_DATE,
                sort: SD.ORDER_DESC,
                pageNumber: 1
            );
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
        public async Task GetAllAsync_ReturnsForbidden_WhenUserDoesNotHaveRequiredRole()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbidden access.");

            // Simulate a user with no valid claims (unauthorized)
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "testUserId") };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Act
            var result = await _controller.GetAllUpdateRequestProfileAsync(
                search: null,
                status: SD.STATUS_ALL,
                orderBy: SD.CREATED_DATE,
                sort: SD.ORDER_DESC,
                pageNumber: 1
            );
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
        public async Task GetAllAsync_InternalServerError_ShouldReturnInternalServerError()
        {
            // Arrange
            // Simulate a valid user with required role
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

            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");

            // Simulate an exception thrown during the process
            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllTutorUpdateRequestAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAllUpdateRequestProfileAsync(
                search: null,
                status: SD.STATUS_ALL,
                orderBy: SD.CREATED_DATE,
                sort: SD.ORDER_DESC,
                pageNumber: 1
            );
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
        public async Task GetAllAsync_UnauthorizedOrForbidden_ShouldReturnUnauthorized()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");

            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbidden access.");

            // Simulate a user with no claims (unauthorized)
            var claimsIdentity = new ClaimsIdentity(); // No claims added
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Act: Test Unauthorized case
            var resultUnauthorized = await _controller.GetAllUpdateRequestProfileAsync(
                search: null,
                status: SD.STATUS_ALL,
                orderBy: SD.CREATED_DATE,
                sort: SD.ORDER_DESC,
                pageNumber: 1
            );
            var unauthorizedResult = resultUnauthorized.Result as ObjectResult;

            // Assert Unauthorized
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            var apiResponseUnauthorized = unauthorizedResult.Value as APIResponse;
            apiResponseUnauthorized.Should().NotBeNull();
            apiResponseUnauthorized.IsSuccess.Should().BeFalse();
            apiResponseUnauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            apiResponseUnauthorized.ErrorMessages.First().Should().Be("Unauthorized access.");
        }

        [Fact]
        public async Task GetAllAsync_UnauthorizedOrForbidden_ShouldReturnForbidden()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");

            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbidden access.");

            var claimsForbidden = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(
                    ClaimTypes.Role,
                    SD.PARENT_ROLE
                ) // Invalid role for this action
                ,
            };
            var identityForbidden = new ClaimsIdentity(claimsForbidden);
            var principalForbidden = new ClaimsPrincipal(identityForbidden);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principalForbidden },
            };

            // Act: Test Forbidden case
            var resultForbidden = await _controller.GetAllUpdateRequestProfileAsync(
                search: null,
                status: SD.STATUS_ALL,
                orderBy: SD.CREATED_DATE,
                sort: SD.ORDER_DESC,
                pageNumber: 1
            );
            var forbiddenResult = resultForbidden.Result as ObjectResult;

            // Assert Forbidden
            forbiddenResult.Should().NotBeNull();
            forbiddenResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

            var apiResponseForbidden = forbiddenResult.Value as APIResponse;
            apiResponseForbidden.Should().NotBeNull();
            apiResponseForbidden.IsSuccess.Should().BeFalse();
            apiResponseForbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            apiResponseForbidden.ErrorMessages.First().Should().Be("Forbidden access.");
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ValidRoleTutor_NoSearchStatusApproved_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
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

            var returnResults = new List<TutorProfileUpdateRequest>
            {
                new TutorProfileUpdateRequest
                {
                    Id = 1,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 2,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
            };

            var expectedResults = new List<TutorProfileUpdateRequestDTO>
            {
                new TutorProfileUpdateRequestDTO
                {
                    Id = 1,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequestDTO
                {
                    Id = 2,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
            };

            if (sortOrder == SD.ORDER_DESC)
            {
                returnResults = returnResults.OrderByDescending(x => x.CreatedDate).ToList();
                expectedResults = expectedResults.OrderByDescending(x => x.CreatedDate).ToList();
            }
            else
            {
                returnResults = returnResults.OrderBy(x => x.CreatedDate).ToList();
                expectedResults = expectedResults.OrderBy(x => x.CreatedDate).ToList();
            }

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllTutorUpdateRequestAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((2, returnResults.ToList()));

            // Act
            var result = await _controller.GetAllUpdateRequestProfileAsync(
                search: null,
                status: SD.STATUS_APPROVE,
                orderBy: SD.CREATED_DATE,
                sort: sortOrder,
                pageNumber: 1
            );

            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var actualResults = apiResponse.Result as List<TutorProfileUpdateRequestDTO>;
            actualResults.Should().NotBeNull();
            actualResults
                .Should()
                .BeEquivalentTo(expectedResults, options => options.WithStrictOrdering());
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ValidRoleTutor_NoSearchStatusRejected_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
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

            var returnResults = new List<TutorProfileUpdateRequest>
            {
                new TutorProfileUpdateRequest
                {
                    Id = 1,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 2,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
            };

            var expectedResults = new List<TutorProfileUpdateRequestDTO>
            {
                new TutorProfileUpdateRequestDTO
                {
                    Id = 1,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequestDTO
                {
                    Id = 2,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
            };

            if (sortOrder == SD.ORDER_DESC)
            {
                returnResults = returnResults.OrderByDescending(x => x.CreatedDate).ToList();
                expectedResults = expectedResults.OrderByDescending(x => x.CreatedDate).ToList();
            }
            else
            {
                returnResults = returnResults.OrderBy(x => x.CreatedDate).ToList();
                expectedResults = expectedResults.OrderBy(x => x.CreatedDate).ToList();
            }

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllTutorUpdateRequestAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((2, returnResults.ToList()));

            // Act
            var result = await _controller.GetAllUpdateRequestProfileAsync(
                search: null,
                status: SD.STATUS_REJECT,
                orderBy: SD.CREATED_DATE,
                sort: sortOrder,
                pageNumber: 1
            );

            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var actualResults = apiResponse.Result as List<TutorProfileUpdateRequestDTO>;
            actualResults.Should().NotBeNull();
            actualResults
                .Should()
                .BeEquivalentTo(expectedResults, options => options.WithStrictOrdering());
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ValidRoleTutor_NoSearchStatusPending_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
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

            var returnResults = new List<TutorProfileUpdateRequest>
            {
                new TutorProfileUpdateRequest
                {
                    Id = 1,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 2,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
            };

            var expectedResults = new List<TutorProfileUpdateRequestDTO>
            {
                new TutorProfileUpdateRequestDTO
                {
                    Id = 1,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequestDTO
                {
                    Id = 2,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
            };

            if (sortOrder == SD.ORDER_DESC)
            {
                returnResults = returnResults.OrderByDescending(x => x.CreatedDate).ToList();
                expectedResults = expectedResults.OrderByDescending(x => x.CreatedDate).ToList();
            }
            else
            {
                returnResults = returnResults.OrderBy(x => x.CreatedDate).ToList();
                expectedResults = expectedResults.OrderBy(x => x.CreatedDate).ToList();
            }

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllTutorUpdateRequestAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((2, returnResults.ToList()));

            // Act
            var result = await _controller.GetAllUpdateRequestProfileAsync(
                search: null,
                status: SD.STATUS_PENDING,
                orderBy: SD.CREATED_DATE,
                sort: sortOrder,
                pageNumber: 1
            );

            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var actualResults = apiResponse.Result as List<TutorProfileUpdateRequestDTO>;
            actualResults.Should().NotBeNull();
            actualResults
                .Should()
                .BeEquivalentTo(expectedResults, options => options.WithStrictOrdering());
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ValidRoleTutor_SearchStatusAll_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
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

            var returnResults = new List<TutorProfileUpdateRequest>
            {
                new TutorProfileUpdateRequest
                {
                    Id = 1,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 2,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 3,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 3, 1),
                },
            };

            var expectedResults = new List<TutorProfileUpdateRequestDTO>
            {
                new TutorProfileUpdateRequestDTO
                {
                    Id = 1,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequestDTO
                {
                    Id = 2,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
                new TutorProfileUpdateRequestDTO
                {
                    Id = 3,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 3, 1),
                },
            };

            if (sortOrder == SD.ORDER_DESC)
            {
                returnResults = returnResults.OrderByDescending(x => x.CreatedDate).ToList();
                expectedResults = expectedResults.OrderByDescending(x => x.CreatedDate).ToList();
            }
            else
            {
                returnResults = returnResults.OrderBy(x => x.CreatedDate).ToList();
                expectedResults = expectedResults.OrderBy(x => x.CreatedDate).ToList();
            }

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllTutorUpdateRequestAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((returnResults.Count, returnResults.ToList()));

            // Act
            var result = await _controller.GetAllUpdateRequestProfileAsync(
                search: null,
                status: SD.STATUS_ALL,
                orderBy: SD.CREATED_DATE,
                sort: sortOrder,
                pageNumber: 1
            );

            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var actualResults = apiResponse.Result as List<TutorProfileUpdateRequestDTO>;
            actualResults.Should().NotBeNull();
            actualResults
                .Should()
                .BeEquivalentTo(expectedResults, options => options.WithStrictOrdering());
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ValidRoleTutor_SearchProvidedStatusAll_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
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

            var searchQuery = "2023"; // Example search query.

            var returnResults = new List<TutorProfileUpdateRequest>
            {
                new TutorProfileUpdateRequest
                {
                    Id = 1,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 2,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 3,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 3, 1),
                },
            };

            var expectedResults = new List<TutorProfileUpdateRequestDTO>
            {
                new TutorProfileUpdateRequestDTO
                {
                    Id = 1,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequestDTO
                {
                    Id = 2,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
                new TutorProfileUpdateRequestDTO
                {
                    Id = 3,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 3, 1),
                },
            };

            if (sortOrder == SD.ORDER_DESC)
            {
                returnResults = returnResults.OrderByDescending(x => x.CreatedDate).ToList();
                expectedResults = expectedResults.OrderByDescending(x => x.CreatedDate).ToList();
            }
            else
            {
                returnResults = returnResults.OrderBy(x => x.CreatedDate).ToList();
                expectedResults = expectedResults.OrderBy(x => x.CreatedDate).ToList();
            }

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllTutorUpdateRequestAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((returnResults.Count, returnResults.ToList()));

            // Act
            var result = await _controller.GetAllUpdateRequestProfileAsync(
                search: searchQuery,
                status: SD.STATUS_ALL,
                orderBy: SD.CREATED_DATE,
                sort: sortOrder,
                pageNumber: 1
            );

            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var actualResults = apiResponse.Result as List<TutorProfileUpdateRequestDTO>;
            actualResults.Should().NotBeNull();
            actualResults
                .Should()
                .BeEquivalentTo(expectedResults, options => options.WithStrictOrdering());
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ValidRoleTutor_SearchProvidedStatusReject_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
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

            var searchQuery = "reject"; // Example search query matching the reject status.

            var returnResults = new List<TutorProfileUpdateRequest>
            {
                new TutorProfileUpdateRequest
                {
                    Id = 1,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 1, 1),
                    Tutor = new Tutor
                    {
                        User = new ApplicationUser
                        {
                            FullName = "John Doe",
                            Email = "john.doe@example.com",
                        },
                    },
                },
                new TutorProfileUpdateRequest
                {
                    Id = 2,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 2, 1),
                    Tutor = new Tutor
                    {
                        User = new ApplicationUser
                        {
                            FullName = "Jane Smith",
                            Email = "jane.smith@example.com",
                        },
                    },
                },
                new TutorProfileUpdateRequest
                {
                    Id = 3,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 3, 1),
                    Tutor = new Tutor
                    {
                        User = new ApplicationUser
                        {
                            FullName = "Jack Reject",
                            Email = "jack.reject@example.com",
                        },
                    },
                },
            };

            // Filter for status REJECT and matching the search query.
            var filteredResults = returnResults
                .Where(x =>
                    x.RequestStatus == SD.Status.REJECT
                    && x.Tutor?.User != null
                    && (
                        x.Tutor.User.FullName.ToLower().Contains(searchQuery.ToLower())
                        || x.Tutor.User.Email.ToLower().Contains(searchQuery.ToLower())
                    )
                )
                .ToList();

            // Sort based on the provided order.
            if (sortOrder == SD.ORDER_DESC)
            {
                filteredResults = filteredResults.OrderByDescending(x => x.CreatedDate).ToList();
            }
            else
            {
                filteredResults = filteredResults.OrderBy(x => x.CreatedDate).ToList();
            }

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllTutorUpdateRequestAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredResults.Count, filteredResults.ToList()));

            // Act
            var result = await _controller.GetAllUpdateRequestProfileAsync(
                search: searchQuery,
                status: SD.STATUS_REJECT,
                orderBy: SD.CREATED_DATE,
                sort: sortOrder,
                pageNumber: 1
            );

            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var actualResults = apiResponse.Result as List<TutorProfileUpdateRequestDTO>;
            actualResults.Should().NotBeNull();
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ValidRoleTutor_SearchProvidedStatusApprove_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
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

            var searchQuery = "2023"; // Example search query.

            var returnResults = new List<TutorProfileUpdateRequest>
            {
                new TutorProfileUpdateRequest
                {
                    Id = 1,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 2,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 3,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 3, 1),
                },
            };

            // Filter results by status APPROVE and matching the search query.
            var filteredResults = returnResults
                .Where(x =>
                    x.RequestStatus == SD.Status.APPROVE
                    && x.CreatedDate.Year.ToString().Contains(searchQuery)
                ) // Filter based on search query.
                .ToList();

            // Sort based on the provided order.
            if (sortOrder == SD.ORDER_DESC)
            {
                filteredResults = filteredResults.OrderByDescending(x => x.CreatedDate).ToList();
            }
            else
            {
                filteredResults = filteredResults.OrderBy(x => x.CreatedDate).ToList();
            }

            var expectedResults = filteredResults
                .Select(x => new TutorProfileUpdateRequestDTO
                {
                    Id = x.Id,
                    RequestStatus = x.RequestStatus,
                    CreatedDate = x.CreatedDate,
                })
                .ToList();

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllTutorUpdateRequestAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredResults.Count, filteredResults.ToList()));

            // Act
            var result = await _controller.GetAllUpdateRequestProfileAsync(
                search: searchQuery,
                status: SD.STATUS_APPROVE,
                orderBy: SD.CREATED_DATE,
                sort: sortOrder,
                pageNumber: 1
            );

            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var actualResults = apiResponse.Result as List<TutorProfileUpdateRequestDTO>;
            actualResults.Should().NotBeNull();
            actualResults
                .Should()
                .BeEquivalentTo(expectedResults, options => options.WithStrictOrdering());
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ValidRoleTutor_SearchProvidedStatusPending_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
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

            var searchQuery = "2023"; // Example search query.

            var returnResults = new List<TutorProfileUpdateRequest>
            {
                new TutorProfileUpdateRequest
                {
                    Id = 1,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 2,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 3,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 3, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 4,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 4, 1),
                },
            };

            // Filter results for status PENDING and matching the search query.
            var filteredResults = returnResults
                .Where(x =>
                    x.RequestStatus == SD.Status.PENDING
                    && x.CreatedDate.Year.ToString().Contains(searchQuery)
                )
                .ToList();

            // Sort the filtered results based on the sortOrder.
            if (sortOrder == SD.ORDER_DESC)
            {
                filteredResults = filteredResults.OrderByDescending(x => x.CreatedDate).ToList();
            }
            else
            {
                filteredResults = filteredResults.OrderBy(x => x.CreatedDate).ToList();
            }

            var expectedResults = filteredResults
                .Select(x => new TutorProfileUpdateRequestDTO
                {
                    Id = x.Id,
                    RequestStatus = x.RequestStatus,
                    CreatedDate = x.CreatedDate,
                })
                .ToList();

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllTutorUpdateRequestAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredResults.Count, filteredResults.ToList()));

            // Act
            var result = await _controller.GetAllUpdateRequestProfileAsync(
                search: searchQuery,
                status: SD.STATUS_PENDING,
                orderBy: SD.CREATED_DATE,
                sort: sortOrder,
                pageNumber: 1
            );

            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var actualResults = apiResponse.Result as List<TutorProfileUpdateRequestDTO>;
            actualResults.Should().NotBeNull();
            actualResults
                .Should()
                .BeEquivalentTo(expectedResults, options => options.WithStrictOrdering());
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ValidRoleStaff_NoSearchStatusApproved_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var returnResults = new List<TutorProfileUpdateRequest>
            {
                new TutorProfileUpdateRequest
                {
                    Id = 1,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 2,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
            };

            var expectedResults = new List<TutorProfileUpdateRequestDTO>
            {
                new TutorProfileUpdateRequestDTO
                {
                    Id = 1,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequestDTO
                {
                    Id = 2,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
            };

            if (sortOrder == SD.ORDER_DESC)
            {
                returnResults = returnResults.OrderByDescending(x => x.CreatedDate).ToList();
                expectedResults = expectedResults.OrderByDescending(x => x.CreatedDate).ToList();
            }
            else
            {
                returnResults = returnResults.OrderBy(x => x.CreatedDate).ToList();
                expectedResults = expectedResults.OrderBy(x => x.CreatedDate).ToList();
            }

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllTutorUpdateRequestAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((2, returnResults.ToList()));

            // Act
            var result = await _controller.GetAllUpdateRequestProfileAsync(
                search: null,
                status: SD.STATUS_APPROVE,
                orderBy: SD.CREATED_DATE,
                sort: sortOrder,
                pageNumber: 1
            );

            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var actualResults = apiResponse.Result as List<TutorProfileUpdateRequestDTO>;
            actualResults.Should().NotBeNull();
            actualResults
                .Should()
                .BeEquivalentTo(expectedResults, options => options.WithStrictOrdering());
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ValidRoleStaff_NoSearchStatusRejected_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var returnResults = new List<TutorProfileUpdateRequest>
            {
                new TutorProfileUpdateRequest
                {
                    Id = 1,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 2,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
            };

            var expectedResults = new List<TutorProfileUpdateRequestDTO>
            {
                new TutorProfileUpdateRequestDTO
                {
                    Id = 1,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequestDTO
                {
                    Id = 2,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
            };

            if (sortOrder == SD.ORDER_DESC)
            {
                returnResults = returnResults.OrderByDescending(x => x.CreatedDate).ToList();
                expectedResults = expectedResults.OrderByDescending(x => x.CreatedDate).ToList();
            }
            else
            {
                returnResults = returnResults.OrderBy(x => x.CreatedDate).ToList();
                expectedResults = expectedResults.OrderBy(x => x.CreatedDate).ToList();
            }

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllTutorUpdateRequestAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((2, returnResults.ToList()));

            // Act
            var result = await _controller.GetAllUpdateRequestProfileAsync(
                search: null,
                status: SD.STATUS_REJECT,
                orderBy: SD.CREATED_DATE,
                sort: sortOrder,
                pageNumber: 1
            );

            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var actualResults = apiResponse.Result as List<TutorProfileUpdateRequestDTO>;
            actualResults.Should().NotBeNull();
            actualResults
                .Should()
                .BeEquivalentTo(expectedResults, options => options.WithStrictOrdering());
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ValidRoleStaff_NoSearchStatusPending_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var returnResults = new List<TutorProfileUpdateRequest>
            {
                new TutorProfileUpdateRequest
                {
                    Id = 1,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 2,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
            };

            var expectedResults = new List<TutorProfileUpdateRequestDTO>
            {
                new TutorProfileUpdateRequestDTO
                {
                    Id = 1,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequestDTO
                {
                    Id = 2,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
            };

            if (sortOrder == SD.ORDER_DESC)
            {
                returnResults = returnResults.OrderByDescending(x => x.CreatedDate).ToList();
                expectedResults = expectedResults.OrderByDescending(x => x.CreatedDate).ToList();
            }
            else
            {
                returnResults = returnResults.OrderBy(x => x.CreatedDate).ToList();
                expectedResults = expectedResults.OrderBy(x => x.CreatedDate).ToList();
            }

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllTutorUpdateRequestAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((2, returnResults.ToList()));

            // Act
            var result = await _controller.GetAllUpdateRequestProfileAsync(
                search: null,
                status: SD.STATUS_PENDING,
                orderBy: SD.CREATED_DATE,
                sort: sortOrder,
                pageNumber: 1
            );

            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var actualResults = apiResponse.Result as List<TutorProfileUpdateRequestDTO>;
            actualResults.Should().NotBeNull();
            actualResults
                .Should()
                .BeEquivalentTo(expectedResults, options => options.WithStrictOrdering());
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ValidRoleStaff_SearchStatusAll_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var returnResults = new List<TutorProfileUpdateRequest>
            {
                new TutorProfileUpdateRequest
                {
                    Id = 1,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 2,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 3,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 3, 1),
                },
            };

            var expectedResults = new List<TutorProfileUpdateRequestDTO>
            {
                new TutorProfileUpdateRequestDTO
                {
                    Id = 1,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequestDTO
                {
                    Id = 2,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
                new TutorProfileUpdateRequestDTO
                {
                    Id = 3,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 3, 1),
                },
            };

            if (sortOrder == SD.ORDER_DESC)
            {
                returnResults = returnResults.OrderByDescending(x => x.CreatedDate).ToList();
                expectedResults = expectedResults.OrderByDescending(x => x.CreatedDate).ToList();
            }
            else
            {
                returnResults = returnResults.OrderBy(x => x.CreatedDate).ToList();
                expectedResults = expectedResults.OrderBy(x => x.CreatedDate).ToList();
            }

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllTutorUpdateRequestAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((returnResults.Count, returnResults.ToList()));

            // Act
            var result = await _controller.GetAllUpdateRequestProfileAsync(
                search: null,
                status: SD.STATUS_ALL,
                orderBy: SD.CREATED_DATE,
                sort: sortOrder,
                pageNumber: 1
            );

            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var actualResults = apiResponse.Result as List<TutorProfileUpdateRequestDTO>;
            actualResults.Should().NotBeNull();
            actualResults
                .Should()
                .BeEquivalentTo(expectedResults, options => options.WithStrictOrdering());
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ValidRoleStaff_SearchProvidedStatusAll_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var searchQuery = "2023"; // Example search query.

            var returnResults = new List<TutorProfileUpdateRequest>
            {
                new TutorProfileUpdateRequest
                {
                    Id = 1,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 2,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 3,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 3, 1),
                },
            };

            var expectedResults = new List<TutorProfileUpdateRequestDTO>
            {
                new TutorProfileUpdateRequestDTO
                {
                    Id = 1,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequestDTO
                {
                    Id = 2,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
                new TutorProfileUpdateRequestDTO
                {
                    Id = 3,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 3, 1),
                },
            };

            if (sortOrder == SD.ORDER_DESC)
            {
                returnResults = returnResults.OrderByDescending(x => x.CreatedDate).ToList();
                expectedResults = expectedResults.OrderByDescending(x => x.CreatedDate).ToList();
            }
            else
            {
                returnResults = returnResults.OrderBy(x => x.CreatedDate).ToList();
                expectedResults = expectedResults.OrderBy(x => x.CreatedDate).ToList();
            }

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllTutorUpdateRequestAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((returnResults.Count, returnResults.ToList()));

            // Act
            var result = await _controller.GetAllUpdateRequestProfileAsync(
                search: searchQuery,
                status: SD.STATUS_ALL,
                orderBy: SD.CREATED_DATE,
                sort: sortOrder,
                pageNumber: 1
            );

            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var actualResults = apiResponse.Result as List<TutorProfileUpdateRequestDTO>;
            actualResults.Should().NotBeNull();
            actualResults
                .Should()
                .BeEquivalentTo(expectedResults, options => options.WithStrictOrdering());
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ValidRoleStaff_SearchProvidedStatusReject_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var searchQuery = "reject"; // Example search query matching the reject status.

            var returnResults = new List<TutorProfileUpdateRequest>
            {
                new TutorProfileUpdateRequest
                {
                    Id = 1,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 1, 1),
                    Tutor = new Tutor
                    {
                        User = new ApplicationUser
                        {
                            FullName = "John Doe",
                            Email = "john.doe@example.com",
                        },
                    },
                },
                new TutorProfileUpdateRequest
                {
                    Id = 2,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 2, 1),
                    Tutor = new Tutor
                    {
                        User = new ApplicationUser
                        {
                            FullName = "Jane Smith",
                            Email = "jane.smith@example.com",
                        },
                    },
                },
                new TutorProfileUpdateRequest
                {
                    Id = 3,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 3, 1),
                    Tutor = new Tutor
                    {
                        User = new ApplicationUser
                        {
                            FullName = "Jack Reject",
                            Email = "jack.reject@example.com",
                        },
                    },
                },
            };

            // Filter for status REJECT and matching the search query.
            var filteredResults = returnResults
                .Where(x =>
                    x.RequestStatus == SD.Status.REJECT
                    && x.Tutor?.User != null
                    && (
                        x.Tutor.User.FullName.ToLower().Contains(searchQuery.ToLower())
                        || x.Tutor.User.Email.ToLower().Contains(searchQuery.ToLower())
                    )
                )
                .ToList();

            // Sort based on the provided order.
            if (sortOrder == SD.ORDER_DESC)
            {
                filteredResults = filteredResults.OrderByDescending(x => x.CreatedDate).ToList();
            }
            else
            {
                filteredResults = filteredResults.OrderBy(x => x.CreatedDate).ToList();
            }

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllTutorUpdateRequestAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredResults.Count, filteredResults.ToList()));

            // Act
            var result = await _controller.GetAllUpdateRequestProfileAsync(
                search: searchQuery,
                status: SD.STATUS_REJECT,
                orderBy: SD.CREATED_DATE,
                sort: sortOrder,
                pageNumber: 1
            );

            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var actualResults = apiResponse.Result as List<TutorProfileUpdateRequestDTO>;
            actualResults.Should().NotBeNull();
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ValidRoleStaff_SearchProvidedStatusApprove_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var searchQuery = "2023"; // Example search query.

            var returnResults = new List<TutorProfileUpdateRequest>
            {
                new TutorProfileUpdateRequest
                {
                    Id = 1,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 2,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 3,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 3, 1),
                },
            };

            // Filter results by status APPROVE and matching the search query.
            var filteredResults = returnResults
                .Where(x =>
                    x.RequestStatus == SD.Status.APPROVE
                    && x.CreatedDate.Year.ToString().Contains(searchQuery)
                ) // Filter based on search query.
                .ToList();

            // Sort based on the provided order.
            if (sortOrder == SD.ORDER_DESC)
            {
                filteredResults = filteredResults.OrderByDescending(x => x.CreatedDate).ToList();
            }
            else
            {
                filteredResults = filteredResults.OrderBy(x => x.CreatedDate).ToList();
            }

            var expectedResults = filteredResults
                .Select(x => new TutorProfileUpdateRequestDTO
                {
                    Id = x.Id,
                    RequestStatus = x.RequestStatus,
                    CreatedDate = x.CreatedDate,
                })
                .ToList();

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllTutorUpdateRequestAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredResults.Count, filteredResults.ToList()));

            // Act
            var result = await _controller.GetAllUpdateRequestProfileAsync(
                search: searchQuery,
                status: SD.STATUS_APPROVE,
                orderBy: SD.CREATED_DATE,
                sort: sortOrder,
                pageNumber: 1
            );

            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var actualResults = apiResponse.Result as List<TutorProfileUpdateRequestDTO>;
            actualResults.Should().NotBeNull();
            actualResults
                .Should()
                .BeEquivalentTo(expectedResults, options => options.WithStrictOrdering());
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ValidRoleStaff_SearchProvidedStatusPending_ShouldReturnSortedResults(
            string sortOrder
        )
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var searchQuery = "2023"; // Example search query.

            var returnResults = new List<TutorProfileUpdateRequest>
            {
                new TutorProfileUpdateRequest
                {
                    Id = 1,
                    RequestStatus = SD.Status.APPROVE,
                    CreatedDate = new DateTime(2023, 1, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 2,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 2, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 3,
                    RequestStatus = SD.Status.PENDING,
                    CreatedDate = new DateTime(2023, 3, 1),
                },
                new TutorProfileUpdateRequest
                {
                    Id = 4,
                    RequestStatus = SD.Status.REJECT,
                    CreatedDate = new DateTime(2023, 4, 1),
                },
            };

            // Filter results for status PENDING and matching the search query.
            var filteredResults = returnResults
                .Where(x =>
                    x.RequestStatus == SD.Status.PENDING
                    && x.CreatedDate.Year.ToString().Contains(searchQuery)
                )
                .ToList();

            // Sort the filtered results based on the sortOrder.
            if (sortOrder == SD.ORDER_DESC)
            {
                filteredResults = filteredResults.OrderByDescending(x => x.CreatedDate).ToList();
            }
            else
            {
                filteredResults = filteredResults.OrderBy(x => x.CreatedDate).ToList();
            }

            var expectedResults = filteredResults
                .Select(x => new TutorProfileUpdateRequestDTO
                {
                    Id = x.Id,
                    RequestStatus = x.RequestStatus,
                    CreatedDate = x.CreatedDate,
                })
                .ToList();

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllTutorUpdateRequestAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredResults.Count, filteredResults.ToList()));

            // Act
            var result = await _controller.GetAllUpdateRequestProfileAsync(
                search: searchQuery,
                status: SD.STATUS_PENDING,
                orderBy: SD.CREATED_DATE,
                sort: sortOrder,
                pageNumber: 1
            );

            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var actualResults = apiResponse.Result as List<TutorProfileUpdateRequestDTO>;
            actualResults.Should().NotBeNull();
            actualResults
                .Should()
                .BeEquivalentTo(expectedResults, options => options.WithStrictOrdering());
        }

        [Fact]
        public async Task GetProfileTutor_InvalidAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange: Set up a user without authentication (no claims)
            var claims = new List<Claim>(); // No user or role claim
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            _mockResourceService
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");
            // Act: Call the GetProfileTutor method
            var result = await _controller.GetProfileTutor();

            // Assert: Verify the response is Unauthorized
            var unauthorizedResult = result.Result as ObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            var apiResponse = unauthorizedResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            apiResponse.ErrorMessages.Should().Contain("Unauthorized access.");
        }

        [Fact]
        public async Task GetProfileTutor_InvalidRole_ShouldReturnForbidden()
        {
            // Arrange: Set up a user with a role other than TUTOR (e.g., ADMIN)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(
                    ClaimTypes.Role,
                    SD.ADMIN_ROLE
                ) // Invalid role, not TUTOR
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

            // Act: Call the GetProfileTutor method
            var result = await _controller.GetProfileTutor();

            // Assert: Verify the response is Forbidden
            var forbiddenResult = result.Result as ObjectResult;
            forbiddenResult.Should().NotBeNull();
            forbiddenResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

            var apiResponse = forbiddenResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            apiResponse.ErrorMessages.Should().Contain("Forbiden access.");
        }

        [Fact]
        public async Task GetProfileTutor_NoPendingRequests_ShouldReturnApprovedRequest()
        {
            // Arrange: Set up a user with the 'TUTOR' role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Valid role
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Setup mock for ResourceService to return forbidden access message if needed
            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbidden access.");

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Arrange mock data for pending requests
            var pendingRequests = new List<TutorProfileUpdateRequest>(); // No pending requests

            // Arrange mock data for approved requests
            var approvedRequests = new List<TutorProfileUpdateRequest>
            {
                new TutorProfileUpdateRequest
                {
                    TutorId = "test-user-id",
                    RequestStatus = Status.APPROVE,
                    CreatedDate = new DateTime(2023, 5, 1),
                },
            };

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((pendingRequests.Count, pendingRequests)); // No pending requests

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((approvedRequests.Count, approvedRequests)); // Returns approved requests

            // Act: Call the GetProfileTutor method
            var result = await _controller.GetProfileTutor();

            // Assert: Verify the response is successful and contains the latest approved request
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var actualResult = apiResponse.Result as TutorProfileUpdateRequestDTO;
            actualResult.Should().NotBeNull();
            actualResult.CreatedDate.Should().Be(new DateTime(2023, 5, 1));
            actualResult.RequestStatus.Should().Be(Status.APPROVE);
        }

        [Fact]
        public async Task GetProfileTutor_LatestRequestExists_ShouldMapToDTO()
        {
            // Arrange: Set up a user with the 'TUTOR' role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Valid role
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Setup mock for ResourceService to return forbidden access message if needed
            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbidden access.");

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Arrange mock data for pending requests
            var pendingRequests = new List<TutorProfileUpdateRequest>
            {
                new TutorProfileUpdateRequest
                {
                    TutorId = "test-user-id",
                    RequestStatus = Status.PENDING,
                    CreatedDate = new DateTime(2023, 6, 1),
                },
            };

            // Arrange mock data for approved requests
            var approvedRequests = new List<TutorProfileUpdateRequest>
            {
                new TutorProfileUpdateRequest
                {
                    TutorId = "test-user-id",
                    RequestStatus = Status.APPROVE,
                    CreatedDate = new DateTime(2023, 5, 1),
                },
            };

            // Mock repository setup for pending requests
            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((pendingRequests.Count, pendingRequests)); // Return the pending request

            // Mock repository setup for approved requests (if no pending requests exist)
            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((approvedRequests.Count, approvedRequests)); // Return the approved request

            // Act: Call the GetProfileTutor method
            var result = await _controller.GetProfileTutor();

            // Assert: Verify the response is successful and contains the latest pending request
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetProfileTutor_NoPendingRequests_ShouldFetchTutorAndReturnDTO()
        {
            // Arrange: Set up a user with the 'TUTOR' role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Valid role
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Setup mock for ResourceService to return forbidden access message if needed
            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbidden access.");

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Arrange mock data for pending requests
            var pendingRequests = new List<TutorProfileUpdateRequest>(); // No pending requests

            // Mock tutor data
            var tutor = new Tutor
            {
                TutorId = "test-user-id",
                User = new ApplicationUser { FullName = "John Doe" },
            };

            // Arrange mock data for approved requests
            var approvedRequests = new List<TutorProfileUpdateRequest>
            {
                new TutorProfileUpdateRequest
                {
                    TutorId = "test-user-id",
                    RequestStatus = Status.APPROVE,
                    CreatedDate = new DateTime(2023, 5, 1),
                },
            };

            // Mock repository setup for pending requests
            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((pendingRequests.Count, pendingRequests)); // No pending requests

            // Mock repository setup for approved requests (if no pending requests exist)
            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((approvedRequests.Count, approvedRequests)); // Return the approved request

            // Mock the tutor repository to return a tutor
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(tutor); // Return the tutor details

            // Act: Call the GetProfileTutor method
            var result = await _controller.GetProfileTutor();

            // Assert: Verify the response is successful and contains the tutor's data
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetProfileTutor_InternalServerError_ShouldReturnInternalServerError()
        {
            // Arrange: Set up a user with the 'TUTOR' role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Valid role
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Setup mock for ResourceService to return forbidden access message if needed
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Database connection failed");

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Arrange mock data for pending requests
            var pendingRequests = new List<TutorProfileUpdateRequest>(); // No pending requests

            // Arrange mock data for approved requests
            var approvedRequests = new List<TutorProfileUpdateRequest>
            {
                new TutorProfileUpdateRequest
                {
                    TutorId = "test-user-id",
                    RequestStatus = Status.APPROVE,
                    CreatedDate = new DateTime(2023, 5, 1),
                },
            };

            // Mock repository setup for pending requests (No pending requests)
            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((pendingRequests.Count, pendingRequests)); // No pending requests

            // Mock repository setup for approved requests (If no pending requests exist)
            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((approvedRequests.Count, approvedRequests)); // Returns the approved request

            // Simulate an internal server error by making the tutor repository throw an exception
            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ThrowsAsync(new Exception("Database connection failed")); // Simulate failure

            // Act: Call the GetProfileTutor method
            var result = await _controller.GetProfileTutor();

            // Assert: Verify the response is an Internal Server Error (500)
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError); // Should return 500

            var apiResponse = objectResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse(); // Indicates failure
            apiResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError); // 500 Status
            apiResponse.ErrorMessages.Should().Contain("Database connection failed"); // Verify the error message
        }

        [Fact]
        public async Task GetByIdAsync_InternalServerError_ShouldReturnInternalServerError()
        {
            // Arrange: Set up the user with valid claims (to simulate valid authentication and authorization)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "valid-user-id"), // Valid user ID
                new Claim(
                    ClaimTypes.Role,
                    SD.PARENT_ROLE
                ) // Valid role (e.g., Parent)
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Setup mock for ResourceService to return internal server error message
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("An unexpected error occurred.");

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock the repository to throw an exception (simulating an internal server error)
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        false,
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ThrowsAsync(new Exception("Database connection failure"));

            // Act: Call the GetByIdAsync method with a tutor ID
            var result = await _controller.GetByIdAsync("some-tutor-id");

            // Assert: Verify the response is Internal Server Error (500)
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError); // Should return 500 Internal Server Error

            var apiResponse = objectResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse(); // Indicates failure
            apiResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError); // 500 Status

            apiResponse.ErrorMessages.Should().Contain("An unexpected error occurred."); // Verify the error message
        }

        [Fact]
        public async Task GetByIdAsync_EmptyId_ShouldReturnBadRequest()
        {
            // Arrange: Set up the user with valid claims (to simulate valid authentication and authorization)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "valid-user-id"), // Valid user ID
                new Claim(
                    ClaimTypes.Role,
                    SD.PARENT_ROLE
                ) // Valid role (e.g., Parent)
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Setup mock for ResourceService to return bad request message for missing ID
            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Missing or invalid TutorId.");

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Act: Call the GetByIdAsync method with an empty ID
            var result = await _controller.GetByIdAsync("");

            // Assert: Verify the response is Bad Request (400)
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest); // Should return 400 Bad Request

            var apiResponse = objectResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse(); // Indicates failure
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest); // 400 Status

            apiResponse.ErrorMessages.Should().Contain("Missing or invalid TutorId."); // Verify the error message
        }

        [Fact]
        public async Task GetByIdAsync_TutorNotFound_ShouldReturnNotFound()
        {
            // Arrange: Set up the user with valid claims (to simulate valid authentication and authorization)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "valid-user-id"), // Valid user ID
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Valid role (e.g., Parent)
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Setup mock for ResourceService to return not found message
            _mockResourceService
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.TUTOR))
                .Returns("Tutor not found.");

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock the repository to return null (simulate tutor not found)
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        false,
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((Tutor)null); // Simulate tutor not found

            // Act: Call the GetByIdAsync method with a tutor ID that does not exist
            var result = await _controller.GetByIdAsync("non-existent-tutor-id");

            // Assert: Verify the response is Not Found (404)
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound); // Should return 404 Not Found

            var apiResponse = objectResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse(); // Indicates failure
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound); // 404 Status
            apiResponse.ErrorMessages.Should().Contain("Tutor not found."); // Verify the error message
        }

        [Fact]
        public async Task GetByIdAsync_ValidIdAndInValidParentRole_ShouldReturnOk()
        {
            // Arrange: Set up the user with valid claims and the correct role (PARENT_ROLE)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "valid-user-id"), // Valid user ID
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Valid role (Parent)
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Setup mock for ResourceService to return the success message
            _mockResourceService
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.TUTOR))
                .Returns("Tutor not found.");
            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Invalid Tutor ID.");

            // Set up the mock repositories and the tutor data
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        false,
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(
                    new Tutor
                    {
                        TutorId = "valid-tutor-id",
                        User = new ApplicationUser { FullName = "John Doe" },
                        Curriculums = new List<Curriculum>
                        {
                            new Curriculum { Description = "Math" },
                        },
                        Certificates = new List<Certificate>
                        {
                            new Certificate { CertificateName = "Math Expert" },
                        },
                        WorkExperiences = new List<WorkExperience>
                        {
                            new WorkExperience { CompanyName = "XYZ Corp" },
                        },
                        Reviews = new List<Review> { new Review { RateScore = 4 } },
                    }
                );

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Act: Call the GetByIdAsync method with a valid tutor ID
            var result = await _controller.GetByIdAsync("valid-tutor-id");

            // Assert: Verify that the response is Ok (200)
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK); // Should return 200 OK

            var apiResponse = objectResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue(); // Indicates success
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK); // 200 Status

            // Verify the result contains the expected tutor data
            var resultData = apiResponse.Result as TutorDTO;
            resultData.Should().NotBeNull();
            resultData.RejectChildIds.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdAsync_ValidIdAndParentRole_ShouldReturnOk()
        {
            // Arrange: Set up the user with valid claims and the correct role (PARENT_ROLE)
            var userId = "valid-user-id";
            var tutorId = "valid-tutor-id";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId), // Valid user ID
                new Claim(
                    ClaimTypes.Role,
                    SD.PARENT_ROLE
                ) // Valid role (Parent)
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _mockUserRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                        false,
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(
                    new ApplicationUser
                    {
                        Id = userId,
                        FullName = "Parent User",
                        TutorRequests = new List<TutorRequest>
                        {
                            new TutorRequest
                            {
                                ParentId = userId,
                                TutorId = tutorId,
                                RejectType = RejectType.IncompatibilityWithCurriculum,
                                ChildId = 123,
                            },
                        },
                    }
                );
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
                    (
                        2,
                        new List<TutorRequest>
                        {
                            new TutorRequest
                            {
                                ParentId = userId,
                                TutorId = tutorId,
                                RejectType = RejectType.IncompatibilityWithCurriculum,
                                ChildId = 123,
                            },
                            new TutorRequest
                            {
                                ParentId = userId,
                                TutorId = tutorId,
                                RejectType = RejectType.IncompatibilityWithCurriculum,
                                ChildId = 456,
                            },
                        }
                    )
                );
            // Setup mock for ResourceService to return the success message
            _mockResourceService
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.TUTOR))
                .Returns("Tutor not found.");
            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Invalid Tutor ID.");

            // Set up the mock repositories and the tutor data
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        false,
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(
                    new Tutor
                    {
                        TutorId = "valid-tutor-id",
                        User = new ApplicationUser { FullName = "John Doe" },
                        Curriculums = new List<Curriculum>
                        {
                            new Curriculum { Description = "Math" },
                        },
                        Certificates = new List<Certificate>
                        {
                            new Certificate { CertificateName = "Math Expert" },
                        },
                        WorkExperiences = new List<WorkExperience>
                        {
                            new WorkExperience { CompanyName = "XYZ Corp" },
                        },
                        Reviews = new List<Review> { new Review { RateScore = 4 } },
                    }
                );

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Act: Call the GetByIdAsync method with a valid tutor ID
            var result = await _controller.GetByIdAsync("valid-tutor-id");

            // Assert: Verify that the response is Ok (200)
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK); // Should return 200 OK

            var apiResponse = objectResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue(); // Indicates success
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK); // 200 Status

            // Verify the result contains the expected tutor data
            var resultData = apiResponse.Result as TutorDTO;
            resultData.Should().NotBeNull();
            resultData.RejectChildIds.Should().Contain(123);
            resultData.RejectChildIds.Should().Contain(456);
        }

        [Fact]
        public async Task UpdateProfileAsync_ReturnsUnauthorized_WhenUserIsUnauthorized()
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

            var updatePayload = new TutorProfileUpdateRequestCreateDTO
            {
                // Example fields for update payload
                Address = "Updated Value",
            };

            // Act
            var result = await _controller.UpdateProfileAsync(updatePayload);
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
        public async Task UpdateProfileAsync_ReturnsForbidden_WhenUserDoesNotHaveRequiredRole()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbidden access.");

            // Simulate a user with no valid claims (unauthorized)
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "testUserId") };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            var updatePayload = new TutorProfileUpdateRequestCreateDTO
            {
                // Example fields for update payload
                Address = "Updated Value",
            };

            // Act
            var result = await _controller.UpdateProfileAsync(updatePayload);
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
        public async Task UpdateProfileAsync_ReturnsBadRequest_WhenRequestIsDuplicated()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.IN_STATUS_PENDING, SD.UPDATE_PROFILE_REQUEST))
                .Returns("Duplicate request for profile update.");

            // Simulate a valid user with proper role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Simulate an existing request that is in pending status
            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        true
                    )
                )
                .ReturnsAsync(
                    (1, new List<TutorProfileUpdateRequest> { new TutorProfileUpdateRequest() })
                );

            var updatePayload = new TutorProfileUpdateRequestCreateDTO
            {
                Address = "Updated Value",
            };

            // Act
            var result = await _controller.UpdateProfileAsync(updatePayload);
            var badRequestResult = result.Result as ObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            apiResponse.ErrorMessages.First().Should().Be("Duplicate request for profile update.");
        }

        [Fact]
        public async Task UpdateProfileAsync_ReturnsBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.UPDATE_PROFILE_REQUEST))
                .Returns("Invalid model state.");

            // Simulate a valid user with proper role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Simulate invalid model state by adding an error to the model state
            _controller.ModelState.AddModelError("Address", "Address is required.");

            var updatePayload = new TutorProfileUpdateRequestCreateDTO
            {
                // Intentionally missing required field (Address) to trigger model state error
                // Other fields are valid
            };

            // Act
            var result = await _controller.UpdateProfileAsync(updatePayload);
            var badRequestResult = result.Result as ObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            apiResponse.ErrorMessages.First().Should().Be("Invalid model state.");
        }

        [Fact]
        public async Task UpdateProfileAsync_ReturnsNoContent_WhenRequestIsValid()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.UPDATE_PROFILE_REQUEST))
                .Returns("Profile updated successfully.");

            // Simulate a valid user with proper role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Simulate that there are no existing pending requests
            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, object>>>(),
                        true
                    )
                )
                .ReturnsAsync((0, new List<TutorProfileUpdateRequest>()));

            // Setup for successful profile update request creation
            var updatePayload = new TutorProfileUpdateRequestCreateDTO { Address = "New Address" };

            var createdRequest = new TutorProfileUpdateRequest
            {
                TutorId = "testUserId",
                Address = "New Address",
                RequestStatus = Status.PENDING,
            };

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<TutorProfileUpdateRequest>()))
                .ReturnsAsync(createdRequest);

            // Act
            var result = await _controller.UpdateProfileAsync(updatePayload);
            var noContentResult = result.Result as ObjectResult;

            // Assert
            noContentResult.Should().NotBeNull();
            noContentResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = noContentResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
            apiResponse.Result.Should().BeOfType<TutorProfileUpdateRequestDTO>();
        }

        [Fact]
        public async Task UpdateStatusRequest_ReturnsUnauthorized_WhenUserIsNotAuthenticated()
        {
            // Arrange: Simulate a user that is not authenticated (missing NameIdentifier claim)
            var claims = new List<Claim>(); // No NameIdentifier claim
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _mockResourceService
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var changeStatusDTO = new ChangeStatusDTO
            {
                Id = 123,
                StatusChange = (int)
                    Status.APPROVE // Valid status change for this case
                ,
            };

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);
            var unauthorizedResult = result.Result as ObjectResult;

            // Assert
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
            var apiResponse = unauthorizedResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            apiResponse.ErrorMessages.Should().Contain("Unauthorized access.");
        }

        [Fact]
        public async Task UpdateStatusRequest_ReturnsInternalServerError_WhenExceptionOccurs()
        {
            // Arrange: Simulate a valid user but throw an exception when processing the request
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
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
            var changeStatusDTO = new ChangeStatusDTO
            {
                Id = 123,
                StatusChange = (int)
                    Status.APPROVE // Valid status change for this case
                ,
            };

            // Simulate a repository method that throws an exception
            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        false,
                        null,
                        null
                    )
                )
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);
            var errorResult = result.Result as ObjectResult;

            // Assert
            errorResult.Should().NotBeNull();
            errorResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
            var apiResponse = errorResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            apiResponse.ErrorMessages.Should().Contain("Internal server error");
        }

        [Fact]
        public async Task UpdateStatusRequest_ReturnsForbidden_WhenUserHasInvalidRole()
        {
            // Arrange: Simulate a user with an invalid role (e.g., "USER_ROLE")
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    "USER_ROLE"
                ) // Invalid role
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

            var changeStatusDTO = new ChangeStatusDTO
            {
                Id = 123,
                StatusChange = (int)
                    Status.APPROVE // Valid status change for this test
                ,
            };

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);
            var forbiddenResult = result.Result as ObjectResult;

            // Assert
            forbiddenResult.Should().NotBeNull();
            forbiddenResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
            var apiResponse = forbiddenResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            apiResponse.ErrorMessages.Should().Contain("Forbiden access.");
        }

        [Fact]
        public async Task UpdateStatusRequest_ReturnsNotFound_WhenTutorProfileUpdateRequestIsMissing()
        {
            // Arrange: Mock repository to return null for the given request ID
            var requestId = 123;
            var changeStatusDTO = new ChangeStatusDTO
            {
                Id = requestId,
                StatusChange = (int)Status.APPROVE
            };

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(), false, null, null))
                .ReturnsAsync((TutorProfileUpdateRequest)null);

            _mockResourceService
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.UPDATE_PROFILE_REQUEST))
                .Returns("The update profile request was not found.");

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, "testUserId"),
        new Claim(ClaimTypes.Role, SD.STAFF_ROLE) // Valid role
    };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);
            var notFoundResult = result.Result as ObjectResult;

            // Assert
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

            var apiResponse = notFoundResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
            apiResponse.ErrorMessages.Should().Contain("The update profile request was not found.");

        }

        [Fact]
        public async Task UpdateStatusRequest_ReturnsOk_WhenApproveRequestIsSuccessful()
        {
            // Arrange
            var requestId = 123;
            var changeStatusDTO = new ChangeStatusDTO
            {
                Id = requestId,
                StatusChange = (int)Status.APPROVE
            };

            var mockRequest = new TutorProfileUpdateRequest
            {
                Id = requestId,
                RequestStatus = Status.PENDING
            };
            var mockRequestReturn = new TutorProfileUpdateRequest
            {
                Id = requestId,
                RequestStatus = Status.REJECT
            };
            _mockTutorProfileUpdateRequestRepository
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(), false, null, null))
                .ReturnsAsync(mockRequest);

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<TutorProfileUpdateRequest>()))
                .ReturnsAsync(mockRequestReturn);


            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, "staffUserId"),
        new Claim(ClaimTypes.Role, SD.STAFF_ROLE)
    };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);
            var okResult = result.Result as ObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task UpdateStatusRequest_ReturnsOk_WhenRejectRequestIsSuccessful()
        {
            // Arrange
            var requestId = 123;
            var changeStatusDTO = new ChangeStatusDTO
            {
                Id = requestId,
                StatusChange = (int)Status.REJECT
            };

            var mockRequest = new TutorProfileUpdateRequest
            {
                Id = requestId,
                RequestStatus = Status.PENDING
            };

            var mockRequestReturn = new TutorProfileUpdateRequest
            {
                Id = requestId,
                RequestStatus = Status.REJECT
            };

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(), false, null, null))
                .ReturnsAsync(mockRequest);

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<TutorProfileUpdateRequest>()))
                .ReturnsAsync(mockRequestReturn);


            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, "staffUserId"),
        new Claim(ClaimTypes.Role, SD.STAFF_ROLE)
    };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);
            var okResult = result.Result as ObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }


    }
}
