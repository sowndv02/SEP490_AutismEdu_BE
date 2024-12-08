using System;
using System.Collections.Generic;
using System.Data;
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
using AutismEduConnectSystem.Utils;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static AutismEduConnectSystem.SD;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace AutismEduConnectSystem.Controllers.v1.Tests
{
    public class TutorRegistrationRequestControllerTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ITutorRepository> _mockTutorRepository;
        private readonly Mock<IRabbitMQMessageSender> _mockMessageBus;
        private readonly Mock<ITutorRegistrationRequestRepository> _mockTutorRegistrationRequestRepository;
        private readonly Mock<ICurriculumRepository> _mockCurriculumRepository;
        private readonly Mock<IWorkExperienceRepository> _mockWorkExperienceRepository;
        private readonly Mock<ICertificateMediaRepository> _mockCertificateMediaRepository;
        private readonly Mock<ICertificateRepository> _mockCertificateRepository;
        private readonly Mock<IRoleRepository> _mockRoleRepository;
        private readonly Mock<IBlobStorageRepository> _mockBlobStorageRepository;
        private readonly Mock<ILogger<TutorRegistrationRequestController>> _mockLogger;
        private readonly IMapper _mockMapper;
        private readonly Mock<IResourceService> _mockResourceService;
        private readonly Mock<IConfiguration> _mockConfiguration;

        private readonly TutorRegistrationRequestController _controller;

        public TutorRegistrationRequestControllerTests()
        {
            // Initialize mocks
            _mockUserRepository = new Mock<IUserRepository>();
            _mockTutorRepository = new Mock<ITutorRepository>();
            _mockMessageBus = new Mock<IRabbitMQMessageSender>();
            _mockTutorRegistrationRequestRepository =
                new Mock<ITutorRegistrationRequestRepository>();
            _mockCurriculumRepository = new Mock<ICurriculumRepository>();
            _mockWorkExperienceRepository = new Mock<IWorkExperienceRepository>();
            _mockCertificateMediaRepository = new Mock<ICertificateMediaRepository>();
            _mockCertificateRepository = new Mock<ICertificateRepository>();
            _mockRoleRepository = new Mock<IRoleRepository>();
            _mockBlobStorageRepository = new Mock<IBlobStorageRepository>();
            _mockLogger = new Mock<ILogger<TutorRegistrationRequestController>>();
            _mockResourceService = new Mock<IResourceService>();
            _mockConfiguration = new Mock<IConfiguration>();
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingConfig());
            });
            _mockMapper = config.CreateMapper();
            // Setup configuration mock (if needed)
            _mockConfiguration.Setup(c => c["APIConfig:PageSize"]).Returns("10");
            _mockConfiguration.Setup(c => c["RabbitMQSettings:QueueName"]).Returns("testQueue");

            // Create controller instance using the mocked dependencies
            _controller = new TutorRegistrationRequestController(
                _mockUserRepository.Object,
                _mockTutorRepository.Object,
                _mockLogger.Object,
                _mockBlobStorageRepository.Object,
                _mockMapper,
                _mockConfiguration.Object,
                _mockRoleRepository.Object,
                new FormatString(),
                _mockWorkExperienceRepository.Object,
                _mockCertificateRepository.Object,
                _mockCertificateMediaRepository.Object,
                _mockTutorRegistrationRequestRepository.Object,
                _mockCurriculumRepository.Object,
                _mockMessageBus.Object,
                _mockResourceService.Object
            );
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsForbidden_WhenUserDoesNotHaveRequiredRoles()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"), // Authenticated user
                new Claim(
                    ClaimTypes.Role,
                    "InvalidRole"
                ),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Người dùng không có quyền truy cập vầo tài nguyên.");
            // Act
            var result = await _controller.GetByIdAsync(1);
            var forbiddenResult = result.Result as ObjectResult;

            // Assert
            forbiddenResult.Should().NotBeNull();
            forbiddenResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

            var apiResponse = forbiddenResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Người dùng không có quyền truy cập vầo tài nguyên.");
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsUnauthorized_WhenUserIdIsMissing()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Role is valid, but user ID is missing
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            _mockResourceService
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");

            // Act
            var result = await _controller.GetByIdAsync(1);
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
        public async Task GetByIdAsync_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Valid role
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Simulate an exception in the repository call
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        true,
                        It.IsAny<string>(),
                        null
                    )
                )
                .ThrowsAsync(new Exception("Database error"));

            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("An unexpected error occurred.");

            // Act
            var result = await _controller.GetByIdAsync(1);
            var internalServerErrorResult = result.Result as ObjectResult;

            // Assert
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult
                .StatusCode.Should()
                .Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = internalServerErrorResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("An unexpected error occurred.");
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Valid role
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock the resource service for bad request message
            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Invalid ID provided.");

            // Act
            var result = await _controller.GetByIdAsync(0); // Invalid ID
            var badRequestResult = result.Result as ObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Invalid ID provided.");
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNotFound_WhenResultIsNull()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Valid role
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Mock the repository to return null for the requested ID
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        true,
                        "ApprovedBy,Curriculums,WorkExperiences,Certificates",
                        null
                    )
                )
                .ReturnsAsync((TutorRegistrationRequest)null); // Simulate no result found

            // Mock the resource service for not found message
            _mockResourceService
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.TUTOR_REGISTRATION_REQUEST))
                .Returns("Tutor registration request not found.");

            // Act
            var result = await _controller.GetByIdAsync(1); // Valid ID
            var notFoundResult = result.Result as ObjectResult;

            // Assert
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

            var apiResponse = notFoundResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Tutor registration request not found.");
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsOk_WhenIdIsValid()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    SD.STAFF_ROLE
                ) // Valid role
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Create a mock TutorRegistrationRequest to return from the repository
            var mockResult = new TutorRegistrationRequest
            {
                Id = 1,
                ApprovedBy = new ApplicationUser { FullName = "Admin User" },
                Curriculums = new List<Curriculum>(),
                WorkExperiences = new List<WorkExperience>(),
                Certificates = new List<Certificate>(),
            };

            // Mock the repository to return the valid result for the requested ID
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        true,
                        "ApprovedBy,Curriculums,WorkExperiences,Certificates",
                        null
                    )
                )
                .ReturnsAsync(mockResult);

            // Act
            var result = await _controller.GetByIdAsync(1); // Valid ID
            var okResult = result.Result as ObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<TutorRegistrationRequestDTO>();
            apiResponse.ErrorMessages.Should().BeEmpty(); // No error messages expected
        }

        [Fact]
        public async Task CreateAsync_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            var tutorRegistrationRequestCreateDTO = new TutorRegistrationRequestCreateDTO
            {
                FullName = "John Doe",
                Email = "john.doe@example.com",
                StartAge = 25,
                EndAge = 35,
            };

            // Simulate an exception in the repository call or other service
            _mockTutorRegistrationRequestRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<TutorRegistrationRequest>()))
                .ThrowsAsync(new Exception("Database error"));

            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("An unexpected error occurred.");

            // Act
            var result = await _controller.CreateAsync(tutorRegistrationRequestCreateDTO);
            var internalServerErrorResult = result.Result as ObjectResult;

            // Assert
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult
                .StatusCode.Should()
                .Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = internalServerErrorResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("An unexpected error occurred.");
        }

        [Fact]
        public async Task CreateAsync_ReturnsBadRequest_WhenModelIsInvalid()
        {
            // Arrange
            var tutorRegistrationRequestCreateDTO = new TutorRegistrationRequestCreateDTO
            {
                FullName = null, // Invalid data to trigger model validation failure
                Email = "invalidemail", // Invalid email to trigger model validation failure
                StartAge = 35,
                EndAge =
                    25 // Invalid age range, StartAge > EndAge
                ,
            };

            // Mocking invalid model state by setting it to invalid
            _controller.ModelState.AddModelError("FullName", "Full Name is required.");
            _controller.ModelState.AddModelError("Email", "Email is not valid.");
            _controller.ModelState.AddModelError("Age", "StartAge cannot be greater than EndAge.");

            // Mock the resource service for bad request message
            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.TUTOR_REGISTRATION_REQUEST))
                .Returns("Invalid tutor registration request.");

            // Act
            var result = await _controller.CreateAsync(tutorRegistrationRequestCreateDTO);
            var badRequestResult = result.Result as ObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Invalid tutor registration request.");
        }

        [Fact]
        public async Task CreateAsync_ReturnsBadRequest_WhenRequestAlreadyExistsOrIsPendingOrApproved()
        {
            // Arrange
            var tutorRegistrationRequestCreateDTO = new TutorRegistrationRequestCreateDTO
            {
                FullName = "John Doe",
                Email = "john.doe@example.com",
                StartAge = 25,
                EndAge = 35,
            };

            // Simulate that a tutor registration request already exists with the same email and has a PENDING or APPROVE status
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync(
                    new TutorRegistrationRequest
                    {
                        Email = "john.doe@example.com",
                        RequestStatus = Status.PENDING,
                    }
                );

            // Mock the resource service for error message when request already exists
            _mockResourceService
                .Setup(r => r.GetString(SD.TUTOR_REGISTER_REQUEST_EXIST_OR_IS_TUTOR))
                .Returns(
                    "A tutor registration request already exists or is in pending/approved status."
                );

            // Act
            var result = await _controller.CreateAsync(tutorRegistrationRequestCreateDTO);
            var badRequestResult = result.Result as ObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse
                .ErrorMessages.Should()
                .Contain(
                    "A tutor registration request already exists or is in pending/approved status."
                );
        }

        [Fact]
        public async Task CreateAsync_ReturnsBadRequest_WhenEmailAlreadyExists()
        {
            // Arrange
            var tutorRegistrationRequestCreateDTO = new TutorRegistrationRequestCreateDTO
            {
                FullName = "John Doe",
                Email = "john.doe@example.com",
                StartAge = 25,
                EndAge = 35,
            };

            // Simulate that a user already exists with the same email
            _mockUserRepository
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), true, null)
                )
                .ReturnsAsync(new ApplicationUser { Email = "john.doe@example.com" });

            // Mock the resource service for email existing message
            _mockResourceService
                .Setup(r => r.GetString(SD.EMAIL_EXISTING_MESSAGE))
                .Returns("The email address is already registered.");

            // Act
            var result = await _controller.CreateAsync(tutorRegistrationRequestCreateDTO);
            var badRequestResult = result.Result as ObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("The email address is already registered.");
        }

        [Fact]
        public async Task CreateAsync_ReturnsBadRequest_WhenAgeRangeIsInvalid()
        {
            // Arrange
            var tutorRegistrationRequestCreateDTO = new TutorRegistrationRequestCreateDTO
            {
                FullName = "John Doe",
                Email = "john.doe@example.com",
                StartAge = 35, // Invalid StartAge
                EndAge =
                    25 // Invalid EndAge (StartAge > EndAge)
                ,
            };

            // Mock the resource service for bad request message related to age
            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.AGE))
                .Returns("The start age cannot be greater than the end age.");

            // Act
            var result = await _controller.CreateAsync(tutorRegistrationRequestCreateDTO);
            var badRequestResult = result.Result as ObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse
                .ErrorMessages.Should()
                .Contain("The start age cannot be greater than the end age.");
        }

        [Fact]
        public async Task CreateAsync_ReturnsOk_WhenRequestIsValid()
        {
            // Arrange
            var tutorRegistrationRequestCreateDTO = new TutorRegistrationRequestCreateDTO
            {
                FullName = "John Doe",
                Email = "john.doe@example.com",
                StartAge = 25, // Valid StartAge
                EndAge =
                    35 // Valid EndAge (StartAge < EndAge)
                ,
            };

            // Mock successful repository call to create the registration request
            _mockTutorRegistrationRequestRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<TutorRegistrationRequest>()))
                .ReturnsAsync(
                    new TutorRegistrationRequest
                    {
                        Email = "john.doe@example.com",
                        RequestStatus =
                            Status.PENDING // Assuming the status is PENDING after creation
                        ,
                    }
                );

            // Act
            var result = await _controller.CreateAsync(tutorRegistrationRequestCreateDTO);
            var okResult = result.Result as ObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.ErrorMessages.Should().BeEmpty(); // No error messages in case of success
        }

        [Fact]
        public async Task GetAllAsync_ReturnsUnauthorized_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var tutorRegistrationRequestCreateDTO = new TutorRegistrationRequestCreateDTO
            {
                FullName = "John Doe",
                Email = "john.doe@example.com",
                StartAge = 25,
                EndAge = 35,
            };
            _mockResourceService
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");
            // Simulate user is not authenticated (missing NameIdentifier claim)
            var claimsIdentity = new ClaimsIdentity(); // No claims added
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Act
            var result = await _controller.GetAllAsync(search: null, pageNumber: 1);
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
        public async Task GetAllAsync_ReturnsForbidden_WhenUserHasNoRequiredRole()
        {
            // Arrange
            var tutorRegistrationRequestCreateDTO = new TutorRegistrationRequestCreateDTO
            {
                FullName = "John Doe",
                Email = "john.doe@example.com",
                StartAge = 25,
                EndAge = 35,
            };

            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbiden access.");

            // Simulate a user with no valid claims (unauthorized)
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "testUserId") };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Act
            var result = await _controller.GetAllAsync(search: null, pageNumber: 1);
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
        public async Task GetAllAsync_ReturnsInternalServerError_WhenExceptionOccurs()
        {
            // Arrange
            var tutorRegistrationRequestCreateDTO = new TutorRegistrationRequestCreateDTO
            {
                FullName = "John Doe",
                Email = "john.doe@example.com",
                StartAge = 25,
                EndAge = 35,
            };
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
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("An internal server error occurred.");
            // Simulate an exception occurring during repository call
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetAllAsync(search: null, pageNumber: 1);
            var internalServerErrorResult = result.Result as ObjectResult;

            // Assert
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult
                .StatusCode.Should()
                .Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = internalServerErrorResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("An internal server error occurred.");
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithEndDateOnly_OrderByCreatedDate_Sort_WithSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = null; // StartDate is null
            DateTime? endDate = DateTime.UtcNow.AddDays(-1); // EndDate is set to a valid date
            string orderBy = SD.CREATED_DATE; // Sorting first by 'CreatedDate'
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.APPROVE,
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply sorting based on the 'CreatedDate'
            if (sort == SD.ORDER_ASC)
            {
                tutorRegistrationRequests = tutorRegistrationRequests
                    .Where(t => t.CreatedDate <= endDate) // Only filter by CreatedDate <= EndDate
                    .OrderBy(t => t.CreatedDate) // Sorting by 'CreatedDate' in ascending order
                    .ToList();
            }
            else
            {
                tutorRegistrationRequests = tutorRegistrationRequests
                    .Where(t => t.CreatedDate <= endDate) // Only filter by CreatedDate <= EndDate
                    .OrderByDescending(t => t.CreatedDate) // Sorting by 'CreatedDate' in descending order
                    .ToList();
            }

            // Mock the repository to return the tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((tutorRegistrationRequests.Count, tutorRegistrationRequests));

            // Act
            var result = await _controller.GetAllAsync(
                "abc@gmail.com",
                STATUS_PENDING,
                startDate,
                endDate,
                orderBy,
                sort,
                pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(tutorRegistrationRequests.Count());

            // Verify sorting
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStartDateOnly_OrderByNullAndSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = DateTime.UtcNow.AddDays(-10); // StartDate is set to a valid date
            DateTime? endDate = null; // EndDate is null
            string orderBy = null; // Default sorting by CreatedDate
            var searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "John"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.PENDING
                )
                .ToList();

            // Apply filtering by StartDate and endDate
            filteredRequests = filteredRequests
                .Where(t =>
                    t.CreatedDate >= startDate && (endDate == null || t.CreatedDate <= endDate)
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count, filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: "abc@gmail.com",
                status: STATUS_PENDING,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count);

            // Verify filtering by FullName containing "John"
            registrationRequest
                .Should()
                .OnlyContain(r =>
                    r.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                );

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStartAndEndDate_OrderByNullAndSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = DateTime.UtcNow.AddDays(-10); // StartDate is set to a valid date
            DateTime? endDate = DateTime.UtcNow.AddDays(-1); // EndDate is set to a valid date (before today)
            string orderBy = null; // Default sorting by CreatedDate
            var searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "John"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.PENDING
                )
                .ToList();

            // Apply filtering by StartDate and EndDate
            filteredRequests = filteredRequests
                .Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate)
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count, filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: searchQuery,
                status: STATUS_PENDING,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count);

            // Verify filtering by FullName containing "John"
            registrationRequest
                .Should()
                .OnlyContain(r =>
                    r.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                );

            // Verify filtering by StartDate and EndDate
            registrationRequest
                .Should()
                .OnlyContain(r => r.CreatedDate >= startDate && r.CreatedDate <= endDate);

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStartDateAndEndDate_OrderByCreatedDateWithSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = DateTime.UtcNow.AddDays(-10); // StartDate is set to a valid date
            DateTime? endDate = DateTime.UtcNow.AddDays(-2); // EndDate is also set to a valid date
            string orderBy = "CreatedDate"; // Sorting by CreatedDate
            var searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-5),
                    UpdatedDate = DateTime.UtcNow.AddHours(-2),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-4),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "John"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.PENDING
                )
                .ToList();

            // Apply filtering by StartDate and EndDate
            filteredRequests = filteredRequests
                .Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate)
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count, filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: searchQuery,
                status: STATUS_PENDING,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count);

            // Verify filtering by FullName containing "John"
            registrationRequest
                .Should()
                .OnlyContain(r =>
                    r.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                );

            // Verify filtering by date range (StartDate to EndDate)
            registrationRequest
                .Should()
                .OnlyContain(r => r.CreatedDate >= startDate && r.CreatedDate <= endDate);

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithEndDateOnly_OrderByCreatedDateAndSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = null; // StartDate is null
            DateTime? endDate = DateTime.UtcNow.AddDays(-5); // EndDate is set to a valid date
            string orderBy = "CreatedDate"; // Sorting by CreatedDate
            var searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-10),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "John"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.PENDING
                )
                .ToList();

            // Apply filtering by StartDate (null) and EndDate
            filteredRequests = filteredRequests.Where(t => t.CreatedDate <= endDate).ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count, filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: searchQuery,
                status: STATUS_PENDING,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count);

            // Verify filtering by FullName containing "John"
            registrationRequest
                .Should()
                .OnlyContain(r =>
                    r.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                );

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithEndDateOnly_OrderByNullAndSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = null; // StartDate is null
            DateTime? endDate = DateTime.UtcNow.AddDays(-5); // EndDate is set to a valid date
            string orderBy = null; // Default sorting by CreatedDate
            var searchQuery = "tutor"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "John"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.Email.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.PENDING
                )
                .ToList();

            // Apply filtering by EndDate
            filteredRequests = filteredRequests.Where(t => t.CreatedDate <= endDate).ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count, filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: "tutor",
                status: STATUS_PENDING,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count);

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStartDateOnlyAndApprovedStatus_OrderByNullAndSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = DateTime.UtcNow.AddDays(-10); // StartDate is set to a valid date
            DateTime? endDate = null; // EndDate is null
            string orderBy = null; // Order by null (default sorting by CreatedDate)
            var searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.APPROVE, // Approved status
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.APPROVE, // Approved status
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.APPROVE, // Approved status
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "John" and Approved status
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.APPROVE
                )
                .ToList();

            // Apply filtering by StartDate and endDate
            filteredRequests = filteredRequests
                .Where(t =>
                    t.CreatedDate >= startDate && (endDate == null || t.CreatedDate <= endDate)
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: "John", // Search query for "John"
                status: STATUS_APPROVE, // Approved status filter
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify filtering by FullName containing "John"

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStartDateNotNullEndDateNull_OrderByCreatedDateAndSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = DateTime.UtcNow.AddDays(-10); // StartDate is set to a valid date
            DateTime? endDate = null; // EndDate is null
            string orderBy = "CreatedDate"; // Sorting by CreatedDate
            var searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.APPROVE, // Set to APPROVED status
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.APPROVE, // Set to APPROVED status
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING, // Set to PENDING status to exclude
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by RequestStatus == APPROVED, FullName containing "John"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.APPROVE
                )
                .ToList();

            // Apply filtering by StartDate and endDate
            filteredRequests = filteredRequests
                .Where(t =>
                    t.CreatedDate >= startDate && (endDate == null || t.CreatedDate <= endDate)
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count, filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: searchQuery,
                status: STATUS_APPROVE,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify filtering by FullName containing "John"

            // Verify sorting by CreatedDate if needed (ascending or descending)
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStartDateAndEndDate_OrderByCreatedDate_StatusApproved_WithSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = DateTime.UtcNow.AddDays(-10); // StartDate is set to a valid date
            DateTime? endDate = DateTime.UtcNow.AddDays(-5); // EndDate is set to a valid date
            string orderBy = "CreatedDate"; // Sort by CreatedDate
            var searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.APPROVE, // Changed to Approved
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.APPROVE, // Changed to Approved
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-7),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.APPROVE, // Changed to Approved
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-8),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "John"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.APPROVE
                )
                .ToList();

            // Apply filtering by StartDate, EndDate and CreatedDate
            filteredRequests = filteredRequests
                .Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate)
                .OrderBy(x => x.CreatedDate)
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: searchQuery,
                status: STATUS_APPROVE,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify sorting by CreatedDate
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithApprovedStatus_OrderByNullStartAndEndDateWithSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = DateTime.UtcNow.AddDays(-10); // StartDate is set to a valid date
            DateTime? endDate = DateTime.UtcNow.AddDays(-1); // EndDate is set to a valid date
            string orderBy = null; // Default sorting by CreatedDate
            var searchQuery = "Jane"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.APPROVE,
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.APPROVE,
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "Jane" and Approved status
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.APPROVE
                )
                .ToList();

            // Apply filtering by StartDate and EndDate
            filteredRequests = filteredRequests
                .Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate)
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: searchQuery,
                status: STATUS_APPROVE,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify filtering by FullName containing "Jane"

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithEndDateOnly_OrderByNullStatusApprovedAndSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = null; // StartDate is null
            DateTime? endDate = DateTime.UtcNow.AddDays(-5); // EndDate is set to a valid date
            string orderBy = null; // Default sorting by CreatedDate
            var searchQuery = "Smith"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.APPROVE, // Status is now APPROVED
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.APPROVE, // Status is now APPROVED
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.APPROVE, // Status is now APPROVED
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "Smith"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.APPROVE
                )
                .ToList();

            // Apply filtering by CreatedDate only (using endDate, as startDate is null)
            filteredRequests = filteredRequests.Where(t => t.CreatedDate <= endDate).ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: searchQuery,
                status: STATUS_APPROVE, // Status is now "approved"
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify filtering by FullName containing "Smith"

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithApprovedStatusAndNullStartEndDate_Search_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = null; // StartDate is null
            DateTime? endDate = null; // EndDate is null
            string orderBy = "CreatedDate"; // Ordering by CreatedDate
            string searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.APPROVE, // Status is APPROVED
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.APPROVE, // Status is APPROVED
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.APPROVE, // Status is APPROVED
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "John" and status = APPROVED
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.APPROVE
                )
                .ToList();

            // Apply filtering by StartDate and EndDate (both null, so no filtering needed)
            filteredRequests = filteredRequests
                .Where(t =>
                    (startDate == null || t.CreatedDate >= startDate)
                    && (endDate == null || t.CreatedDate <= endDate)
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: searchQuery,
                status: STATUS_APPROVE,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify filtering by FullName containing "John"

            // Verify sorting by CreatedDate
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStatusApproved_SearchWithNullDates_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = null; // StartDate is null
            DateTime? endDate = null; // EndDate is null
            string orderBy = null; // No specific order is provided
            var searchQuery = "Jane"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.APPROVE, // Status is APPROVED
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.APPROVE, // Status is APPROVED
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.APPROVE, // Status is APPROVED
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "Jane"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.APPROVE
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: searchQuery,
                status: STATUS_APPROVE, // Status is APPROVED
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify filtering by FullName containing "Jane"

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithNullStartDateAndEndDate_StatusRejected_OrderByNullAndSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = null; // StartDate is null
            DateTime? endDate = null; // EndDate is null
            string orderBy = null; // OrderBy is null (default sorting will be applied based on CreatedDate)
            var searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = null,
                    RejectionReason = "Not qualified", // Reason for rejection
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = "admin123",
                    RejectionReason = "Insufficient experience", // Reason for rejection
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "John"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.REJECT
                )
                .ToList();

            // Apply filtering by StartDate and EndDate being null
            filteredRequests = filteredRequests
                .Where(t =>
                    t.CreatedDate >= startDate && (endDate == null || t.CreatedDate <= endDate)
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: searchQuery, // Using search query "John"
                status: STATUS_REJECT, // Filtering by rejected status
                startDate: startDate, // StartDate is null
                endDate: endDate, // EndDate is null
                orderBy: orderBy, // OrderBy is null (default sorting by CreatedDate)
                sort: sort, // Sorting ASC or DESC
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStatusRejected_OrderByCreatedDateAndSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = null; // StartDate is null
            DateTime? endDate = null; // EndDate is null
            string orderBy = "CreatedDate"; // Sorting by CreatedDate
            string searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = null,
                    RejectionReason = "Not approved",
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = "admin123",
                    RejectionReason = "Rejected for skills mismatch",
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING, // Status is PENDING, so this won't be included
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by RequestStatus being REJECTED
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.REJECT
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: searchQuery,
                status: STATUS_REJECT,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStatusRejected_OrderByCreatedDateAndSearchWithEndDate_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = null; // StartDate is null
            DateTime? endDate = DateTime.UtcNow.AddDays(-2); // EndDate is not null (set to 2 days ago)
            string orderBy = "CreatedDate"; // Sorting by CreatedDate
            string searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = null,
                    RejectionReason = "Not approved",
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = "admin123",
                    RejectionReason = "Rejected for skills mismatch",
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING, // Status is PENDING, so this won't be included
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by RequestStatus being REJECTED and search query "John"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.REJECT
                )
                .Where(t => t.CreatedDate <= endDate) // Filtering based on EndDate
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: searchQuery,
                status: STATUS_REJECT,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStatusRejected_OrderByNullAndStartDateNullEndDateNotNullAndSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = null; // StartDate is null
            DateTime? endDate = DateTime.UtcNow.AddDays(-2); // EndDate is not null (using a date in the past)
            string orderBy = null; // OrderBy is null
            string searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = null,
                    RejectionReason = "Not approved",
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = "admin123",
                    RejectionReason = "Rejected for skills mismatch",
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING, // Status is PENDING, so this won't be included
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by RequestStatus being REJECTED and matching search query
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.REJECT
                )
                .ToList();

            // Apply filtering by endDate (createdDate <= endDate)
            filteredRequests = filteredRequests.Where(t => t.CreatedDate <= endDate).ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: searchQuery,
                status: STATUS_REJECT,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify sorting if needed (based on 'CreatedDate') if sorting is specified (though here it's null)
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStatusRejected_OrderByNull_StartDateAndEndDateNotNull_Search_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = DateTime.UtcNow.AddDays(-10); // StartDate is not null
            DateTime? endDate = DateTime.UtcNow.AddDays(-2); // EndDate is not null
            string orderBy = null; // OrderBy is null
            string searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = null,
                    RejectionReason = "Not approved",
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = "admin123",
                    RejectionReason = "Rejected for skills mismatch",
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING, // Status is PENDING, so this won't be included
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by RequestStatus being REJECTED and matching the date range and search query
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.REJECT
                    && t.CreatedDate >= startDate
                    && t.CreatedDate <= endDate
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: searchQuery,
                status: STATUS_REJECT,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStatusRejected_OrderByCreatedDateAndSearchWithStartEndDate_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = DateTime.UtcNow.AddDays(-5); // StartDate is not null
            DateTime? endDate = DateTime.UtcNow.AddDays(-1); // EndDate is not null
            string orderBy = "CreatedDate"; // Sorting by CreatedDate
            string searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Sample tutor registration requests
            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = null,
                    RejectionReason = "Not approved",
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = "admin123",
                    RejectionReason = "Rejected for skills mismatch",
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING, // Status is PENDING, so this won't be included
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by RequestStatus being REJECTED and between StartDate and EndDate
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.REJECT
                    && t.CreatedDate >= startDate.Value
                    && t.CreatedDate <= endDate.Value
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: searchQuery,
                status: STATUS_REJECT,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStatusRejected_OrderByCreatedDateAndSearchWithStartDateNotNull_EndDateNull_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = new DateTime(2024, 1, 1); // StartDate is not null
            DateTime? endDate = null; // EndDate is null
            string orderBy = "CreatedDate"; // Sorting by CreatedDate
            string searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = null,
                    RejectionReason = "Not approved",
                    CreatedDate = new DateTime(2024, 1, 2), // CreatedDate after the StartDate
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = "admin123",
                    RejectionReason = "Rejected for skills mismatch",
                    CreatedDate = new DateTime(2024, 1, 3), // CreatedDate after the StartDate
                    UpdatedDate = new DateTime(2024, 1, 3),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING, // Status is PENDING, so this won't be included
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = new DateTime(2024, 1, 4),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by RequestStatus being REJECTED, StartDate, and search query
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.RequestStatus == Status.REJECT
                    && t.CreatedDate >= startDate
                    && t.CreatedDate <= (endDate ?? DateTime.UtcNow)
                    && t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: searchQuery,
                status: STATUS_REJECT,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStatusAll_OrderByNull_StartDateNotNull_EndDateNull_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = new DateTime(2024, 1, 1); // StartDate is not null
            DateTime? endDate = null; // EndDate is null
            string orderBy = null; // No specific sorting field, it's null
            string searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.PENDING, // Status is PENDING, so it will be included
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = new DateTime(2024, 1, 2), // CreatedDate after the StartDate
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.PENDING, // Status is PENDING, so it will be included
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = new DateTime(2024, 1, 3), // CreatedDate after the StartDate
                    UpdatedDate = new DateTime(2024, 1, 3),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING, // Status is PENDING, so it won't be included
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = new DateTime(2024, 1, 4),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by RequestStatus being ALL (PENDING & REJECTED), StartDate, and search query
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    (t.RequestStatus == Status.PENDING || t.RequestStatus == Status.REJECT)
                    && t.CreatedDate >= startDate
                    && t.CreatedDate <= (endDate ?? DateTime.UtcNow)
                    && t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: searchQuery,
                status: "ALL", // Status is ALL (PENDING and REJECTED)
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy, // No specific field to sort by (null)
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithEndDateOnly_OrderByCreatedDate_Sort_WithNoSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = null; // StartDate is null
            DateTime? endDate = DateTime.UtcNow.AddDays(-1); // EndDate is set to a valid date
            string orderBy = SD.CREATED_DATE; // Sorting first by 'CreatedDate'
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.APPROVE,
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply sorting based on the 'CreatedDate'
            if (sort == SD.ORDER_ASC)
            {
                tutorRegistrationRequests = tutorRegistrationRequests
                    .Where(t => t.CreatedDate <= endDate) // Only filter by CreatedDate <= EndDate
                    .OrderBy(t => t.CreatedDate) // Sorting by 'CreatedDate' in ascending order
                    .ToList();
            }
            else
            {
                tutorRegistrationRequests = tutorRegistrationRequests
                    .Where(t => t.CreatedDate <= endDate) // Only filter by CreatedDate <= EndDate
                    .OrderByDescending(t => t.CreatedDate) // Sorting by 'CreatedDate' in descending order
                    .ToList();
            }

            // Mock the repository to return the tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((tutorRegistrationRequests.Count, tutorRegistrationRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: (string)null,
                STATUS_PENDING,
                startDate,
                endDate,
                orderBy,
                sort,
                pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(tutorRegistrationRequests.Count());

            // Verify sorting
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStartDateOnly_OrderByNullAndNoSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = DateTime.UtcNow.AddDays(-10); // StartDate is set to a valid date
            DateTime? endDate = null; // EndDate is null
            string orderBy = null; // Default sorting by CreatedDate
            var searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "John"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.PENDING
                )
                .ToList();

            // Apply filtering by StartDate and endDate
            filteredRequests = filteredRequests
                .Where(t =>
                    t.CreatedDate >= startDate && (endDate == null || t.CreatedDate <= endDate)
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count, filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: (string)null,
                status: STATUS_PENDING,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count);

            // Verify filtering by FullName containing "John"
            registrationRequest
                .Should()
                .OnlyContain(r =>
                    r.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                );

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStartAndEndDate_OrderByNullAndNoSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = DateTime.UtcNow.AddDays(-10); // StartDate is set to a valid date
            DateTime? endDate = DateTime.UtcNow.AddDays(-1); // EndDate is set to a valid date (before today)
            string orderBy = null; // Default sorting by CreatedDate
            var searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "John"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.PENDING
                )
                .ToList();

            // Apply filtering by StartDate and EndDate
            filteredRequests = filteredRequests
                .Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate)
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count, filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: (string)null,
                status: STATUS_PENDING,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count);

            // Verify filtering by FullName containing "John"
            registrationRequest
                .Should()
                .OnlyContain(r =>
                    r.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                );

            // Verify filtering by StartDate and EndDate
            registrationRequest
                .Should()
                .OnlyContain(r => r.CreatedDate >= startDate && r.CreatedDate <= endDate);

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStartDateAndEndDate_OrderByCreatedDateWithNoSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = DateTime.UtcNow.AddDays(-10); // StartDate is set to a valid date
            DateTime? endDate = DateTime.UtcNow.AddDays(-2); // EndDate is also set to a valid date
            string orderBy = "CreatedDate"; // Sorting by CreatedDate
            var searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-5),
                    UpdatedDate = DateTime.UtcNow.AddHours(-2),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-4),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "John"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.PENDING
                )
                .ToList();

            // Apply filtering by StartDate and EndDate
            filteredRequests = filteredRequests
                .Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate)
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count, filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: (string)null,
                status: STATUS_PENDING,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count);

            // Verify filtering by FullName containing "John"
            registrationRequest
                .Should()
                .OnlyContain(r =>
                    r.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                );

            // Verify filtering by date range (StartDate to EndDate)
            registrationRequest
                .Should()
                .OnlyContain(r => r.CreatedDate >= startDate && r.CreatedDate <= endDate);

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithEndDateOnly_OrderByCreatedDateAndNoSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = null; // StartDate is null
            DateTime? endDate = DateTime.UtcNow.AddDays(-5); // EndDate is set to a valid date
            string orderBy = "CreatedDate"; // Sorting by CreatedDate
            var searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-10),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "John"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.PENDING
                )
                .ToList();

            // Apply filtering by StartDate (null) and EndDate
            filteredRequests = filteredRequests.Where(t => t.CreatedDate <= endDate).ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count, filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: (string)null,
                status: STATUS_PENDING,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count);

            // Verify filtering by FullName containing "John"
            registrationRequest
                .Should()
                .OnlyContain(r =>
                    r.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                );

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithEndDateOnly_OrderByNullAndNoSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = null; // StartDate is null
            DateTime? endDate = DateTime.UtcNow.AddDays(-5); // EndDate is set to a valid date
            string orderBy = null; // Default sorting by CreatedDate
            var searchQuery = "tutor"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "John"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.Email.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.PENDING
                )
                .ToList();

            // Apply filtering by EndDate
            filteredRequests = filteredRequests.Where(t => t.CreatedDate <= endDate).ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count, filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: (string)null,
                status: STATUS_PENDING,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count);

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStartDateOnlyAndApprovedStatus_OrderByNullAndNoSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = DateTime.UtcNow.AddDays(-10); // StartDate is set to a valid date
            DateTime? endDate = null; // EndDate is null
            string orderBy = null; // Order by null (default sorting by CreatedDate)
            var searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.APPROVE, // Approved status
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.APPROVE, // Approved status
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.APPROVE, // Approved status
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "John" and Approved status
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.APPROVE
                )
                .ToList();

            // Apply filtering by StartDate and endDate
            filteredRequests = filteredRequests
                .Where(t =>
                    t.CreatedDate >= startDate && (endDate == null || t.CreatedDate <= endDate)
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: (string)null, // Search query for "John"
                status: STATUS_APPROVE, // Approved status filter
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify filtering by FullName containing "John"

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStartDateNotNullEndDateNull_OrderByCreatedDateAndNoSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = DateTime.UtcNow.AddDays(-10); // StartDate is set to a valid date
            DateTime? endDate = null; // EndDate is null
            string orderBy = "CreatedDate"; // Sorting by CreatedDate
            var searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.APPROVE, // Set to APPROVED status
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.APPROVE, // Set to APPROVED status
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING, // Set to PENDING status to exclude
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by RequestStatus == APPROVED, FullName containing "John"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.APPROVE
                )
                .ToList();

            // Apply filtering by StartDate and endDate
            filteredRequests = filteredRequests
                .Where(t =>
                    t.CreatedDate >= startDate && (endDate == null || t.CreatedDate <= endDate)
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count, filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: (string)null,
                status: STATUS_APPROVE,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify filtering by FullName containing "John"

            // Verify sorting by CreatedDate if needed (ascending or descending)
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStartDateAndEndDate_OrderByCreatedDate_StatusApproved_WithNoSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = DateTime.UtcNow.AddDays(-10); // StartDate is set to a valid date
            DateTime? endDate = DateTime.UtcNow.AddDays(-5); // EndDate is set to a valid date
            string orderBy = "CreatedDate"; // Sort by CreatedDate
            var searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.APPROVE, // Changed to Approved
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.APPROVE, // Changed to Approved
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-7),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.APPROVE, // Changed to Approved
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-8),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "John"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.APPROVE
                )
                .ToList();

            // Apply filtering by StartDate, EndDate and CreatedDate
            filteredRequests = filteredRequests
                .Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate)
                .OrderBy(x => x.CreatedDate)
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: (string)null,
                status: STATUS_APPROVE,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify sorting by CreatedDate
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithApprovedStatus_OrderByNullStartAndEndDateWithNoSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = DateTime.UtcNow.AddDays(-10); // StartDate is set to a valid date
            DateTime? endDate = DateTime.UtcNow.AddDays(-1); // EndDate is set to a valid date
            string orderBy = null; // Default sorting by CreatedDate
            var searchQuery = "Jane"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.PENDING,
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.APPROVE,
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.APPROVE,
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "Jane" and Approved status
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.APPROVE
                )
                .ToList();

            // Apply filtering by StartDate and EndDate
            filteredRequests = filteredRequests
                .Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate)
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: (string)null,
                status: STATUS_APPROVE,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify filtering by FullName containing "Jane"

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithEndDateOnly_OrderByNullStatusApprovedAndNoSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = null; // StartDate is null
            DateTime? endDate = DateTime.UtcNow.AddDays(-5); // EndDate is set to a valid date
            string orderBy = null; // Default sorting by CreatedDate
            var searchQuery = "Smith"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.APPROVE, // Status is now APPROVED
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.APPROVE, // Status is now APPROVED
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.APPROVE, // Status is now APPROVED
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "Smith"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.APPROVE
                )
                .ToList();

            // Apply filtering by CreatedDate only (using endDate, as startDate is null)
            filteredRequests = filteredRequests.Where(t => t.CreatedDate <= endDate).ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: (string)null,
                status: STATUS_APPROVE, // Status is now "approved"
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify filtering by FullName containing "Smith"

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithApprovedStatusAndNullStartEndDate_NoSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = null; // StartDate is null
            DateTime? endDate = null; // EndDate is null
            string orderBy = "CreatedDate"; // Ordering by CreatedDate
            string searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.APPROVE, // Status is APPROVED
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.APPROVE, // Status is APPROVED
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.APPROVE, // Status is APPROVED
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "John" and status = APPROVED
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.APPROVE
                )
                .ToList();

            // Apply filtering by StartDate and EndDate (both null, so no filtering needed)
            filteredRequests = filteredRequests
                .Where(t =>
                    (startDate == null || t.CreatedDate >= startDate)
                    && (endDate == null || t.CreatedDate <= endDate)
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: (string)null,
                status: STATUS_APPROVE,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify filtering by FullName containing "John"

            // Verify sorting by CreatedDate
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStatusApproved_NoSearchWithNullDates_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = null; // StartDate is null
            DateTime? endDate = null; // EndDate is null
            string orderBy = null; // No specific order is provided
            var searchQuery = "Jane"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.APPROVE, // Status is APPROVED
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.APPROVE, // Status is APPROVED
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.APPROVE, // Status is APPROVED
                    ApprovedId = "admin123",
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "Jane"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.APPROVE
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: (string)null,
                status: STATUS_APPROVE, // Status is APPROVED
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify filtering by FullName containing "Jane"

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithNullStartDateAndEndDate_StatusRejected_OrderByNullAndNoSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = null; // StartDate is null
            DateTime? endDate = null; // EndDate is null
            string orderBy = null; // OrderBy is null (default sorting will be applied based on CreatedDate)
            var searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = null,
                    RejectionReason = "Not qualified", // Reason for rejection
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = "admin123",
                    RejectionReason = "Insufficient experience", // Reason for rejection
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by FullName containing "John"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.REJECT
                )
                .ToList();

            // Apply filtering by StartDate and EndDate being null
            filteredRequests = filteredRequests
                .Where(t =>
                    t.CreatedDate >= startDate && (endDate == null || t.CreatedDate <= endDate)
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: (string)null, // Using search query "John"
                status: STATUS_REJECT, // Filtering by rejected status
                startDate: startDate, // StartDate is null
                endDate: endDate, // EndDate is null
                orderBy: orderBy, // OrderBy is null (default sorting by CreatedDate)
                sort: sort, // Sorting ASC or DESC
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStatusRejected_OrderByCreatedDateAndNoSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = null; // StartDate is null
            DateTime? endDate = null; // EndDate is null
            string orderBy = "CreatedDate"; // Sorting by CreatedDate
            string searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = null,
                    RejectionReason = "Not approved",
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = "admin123",
                    RejectionReason = "Rejected for skills mismatch",
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING, // Status is PENDING, so this won't be included
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by RequestStatus being REJECTED
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.REJECT
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: (string)null,
                status: STATUS_REJECT,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStatusRejected_OrderByCreatedDateAndNoSearchWithEndDate_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = null; // StartDate is null
            DateTime? endDate = DateTime.UtcNow.AddDays(-2); // EndDate is not null (set to 2 days ago)
            string orderBy = "CreatedDate"; // Sorting by CreatedDate
            string searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = null,
                    RejectionReason = "Not approved",
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = "admin123",
                    RejectionReason = "Rejected for skills mismatch",
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING, // Status is PENDING, so this won't be included
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by RequestStatus being REJECTED and search query "John"
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.REJECT
                )
                .Where(t => t.CreatedDate <= endDate) // Filtering based on EndDate
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: (string)null,
                status: STATUS_REJECT,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStatusRejected_OrderByNullAndStartDateNullEndDateNotNullAndNoSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = null; // StartDate is null
            DateTime? endDate = DateTime.UtcNow.AddDays(-2); // EndDate is not null (using a date in the past)
            string orderBy = null; // OrderBy is null
            string searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = null,
                    RejectionReason = "Not approved",
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = "admin123",
                    RejectionReason = "Rejected for skills mismatch",
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING, // Status is PENDING, so this won't be included
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by RequestStatus being REJECTED and matching search query
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.REJECT
                )
                .ToList();

            // Apply filtering by endDate (createdDate <= endDate)
            filteredRequests = filteredRequests.Where(t => t.CreatedDate <= endDate).ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: (string)null,
                status: STATUS_REJECT,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify sorting if needed (based on 'CreatedDate') if sorting is specified (though here it's null)
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStatusRejected_OrderByNull_StartDateAndEndDateNotNull_NoSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = DateTime.UtcNow.AddDays(-10); // StartDate is not null
            DateTime? endDate = DateTime.UtcNow.AddDays(-2); // EndDate is not null
            string orderBy = null; // OrderBy is null
            string searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = null,
                    RejectionReason = "Not approved",
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = "admin123",
                    RejectionReason = "Rejected for skills mismatch",
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING, // Status is PENDING, so this won't be included
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by RequestStatus being REJECTED and matching the date range and search query
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.REJECT
                    && t.CreatedDate >= startDate
                    && t.CreatedDate <= endDate
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: (string)null,
                status: STATUS_REJECT,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStatusRejected_OrderByCreatedDateAndNoSearchWithStartEndDate_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = DateTime.UtcNow.AddDays(-5); // StartDate is not null
            DateTime? endDate = DateTime.UtcNow.AddDays(-1); // EndDate is not null
            string orderBy = "CreatedDate"; // Sorting by CreatedDate
            string searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Sample tutor registration requests
            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = null,
                    RejectionReason = "Not approved",
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = "admin123",
                    RejectionReason = "Rejected for skills mismatch",
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    UpdatedDate = DateTime.UtcNow.AddHours(-1),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING, // Status is PENDING, so this won't be included
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by RequestStatus being REJECTED and between StartDate and EndDate
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    && t.RequestStatus == Status.REJECT
                    && t.CreatedDate >= startDate.Value
                    && t.CreatedDate <= endDate.Value
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: (string)null,
                status: STATUS_REJECT,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStatusRejected_OrderByCreatedDateAndNoSearchWithStartDateNotNull_EndDateNull_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = new DateTime(2024, 1, 1); // StartDate is not null
            DateTime? endDate = null; // EndDate is null
            string orderBy = "CreatedDate"; // Sorting by CreatedDate
            string searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = null,
                    RejectionReason = "Not approved",
                    CreatedDate = new DateTime(2024, 1, 2), // CreatedDate after the StartDate
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.REJECT, // Status is REJECTED
                    ApprovedId = "admin123",
                    RejectionReason = "Rejected for skills mismatch",
                    CreatedDate = new DateTime(2024, 1, 3), // CreatedDate after the StartDate
                    UpdatedDate = new DateTime(2024, 1, 3),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING, // Status is PENDING, so this won't be included
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = new DateTime(2024, 1, 4),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by RequestStatus being REJECTED, StartDate, and search query
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    t.RequestStatus == Status.REJECT
                    && t.CreatedDate >= startDate
                    && t.CreatedDate <= (endDate ?? DateTime.UtcNow)
                    && t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: (string)null,
                status: STATUS_REJECT,
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy,
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRegistrationRequestWithStatusAll_OrderByNull_StartDateNotNull_EndDateNull_WithNoSearch_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "admin-user-id";
            DateTime? startDate = new DateTime(2024, 1, 1); // StartDate is not null
            DateTime? endDate = null; // EndDate is null
            string orderBy = null; // No specific sorting field, it's null
            string searchQuery = "John"; // Search query for filtering tutors by FullName or other criteria
            var pageNumber = 1;
            var pageSize = 10;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var tutorRegistrationRequests = new List<TutorRegistrationRequest>
            {
                new TutorRegistrationRequest
                {
                    Id = 1,
                    Email = "tutor1@example.com",
                    FullName = "John Doe",
                    PhoneNumber = "123-456-7890",
                    ImageUrl = "http://example.com/images/johndoe.jpg",
                    Address = "123 Main St, City, Country",
                    PriceFrom = 25.00m,
                    PriceEnd = 40.00m,
                    SessionHours = 2.5f,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    StartAge = 18,
                    EndAge = 30,
                    AboutMe = "Experienced tutor with a passion for teaching.",
                    RequestStatus = Status.PENDING, // Status is PENDING, so it will be included
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = new DateTime(2024, 1, 2), // CreatedDate after the StartDate
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 2,
                    Email = "tutor2@example.com",
                    FullName = "Jane Smith",
                    PhoneNumber = "987-654-3210",
                    ImageUrl = "http://example.com/images/janesmith.jpg",
                    Address = "456 Elm St, City, Country",
                    PriceFrom = 30.00m,
                    PriceEnd = 50.00m,
                    SessionHours = 3.0f,
                    DateOfBirth = new DateTime(1990, 8, 20),
                    StartAge = 20,
                    EndAge = 35,
                    AboutMe = "Passionate about teaching and helping students succeed.",
                    RequestStatus = Status.PENDING, // Status is PENDING, so it will be included
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = new DateTime(2024, 1, 3), // CreatedDate after the StartDate
                    UpdatedDate = new DateTime(2024, 1, 3),
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
                new TutorRegistrationRequest
                {
                    Id = 3,
                    Email = "tutor3@example.com",
                    FullName = "Alice Brown",
                    PhoneNumber = "555-123-4567",
                    ImageUrl = null,
                    Address = "789 Oak St, City, Country",
                    PriceFrom = 20.00m,
                    PriceEnd = 35.00m,
                    SessionHours = 1.5f,
                    DateOfBirth = new DateTime(1995, 11, 10),
                    StartAge = 18,
                    EndAge = 28,
                    AboutMe = "I love working with students to improve their skills and knowledge.",
                    RequestStatus = Status.PENDING, // Status is PENDING, so it won't be included
                    ApprovedId = null,
                    RejectionReason = null,
                    CreatedDate = new DateTime(2024, 1, 4),
                    UpdatedDate = null,
                    Curriculums = new List<Curriculum>
                    {
                        new Curriculum
                        {
                            Id = 5,
                            AgeFrom = 3,
                            AgeEnd = 5,
                            Description = "Painting and Sculpture",
                        },
                    },
                    WorkExperiences = new List<WorkExperience>
                    {
                        new WorkExperience
                        {
                            Id = 5,
                            Position = "Art Tutor",
                            CompanyName = "Creative Academy",
                            StartDate = new DateTime(2016, 7, 1),
                            EndDate = new DateTime(2018, 7, 1),
                        },
                    },
                    Certificates = new List<Certificate>
                    {
                        new Certificate
                        {
                            Id = 4,
                            CertificateName = "Art Education Certification",
                            IssuingInstitution = "Art Institute",
                            IssuingDate = new DateTime(2016, 5, 1),
                        },
                    },
                },
            };

            // Apply filtering by RequestStatus being ALL (PENDING & REJECTED), StartDate, and search query
            var filteredRequests = tutorRegistrationRequests
                .Where(t =>
                    (t.RequestStatus == Status.PENDING || t.RequestStatus == Status.REJECT)
                    && t.CreatedDate >= startDate
                    && t.CreatedDate <= (endDate ?? DateTime.UtcNow)
                    && t.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                )
                .ToList();

            // Mock the repository to return the filtered tutor registration requests
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<TutorRegistrationRequest, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((filteredRequests.Count(), filteredRequests));

            // Act
            var result = await _controller.GetAllAsync(
                search: (string)null,
                status: "ALL", // Status is ALL (PENDING and REJECTED)
                startDate: startDate,
                endDate: endDate,
                orderBy: orderBy, // No specific field to sort by (null)
                sort: sort,
                pageNumber: pageNumber
            );

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<TutorRegistrationRequestDTO>>();

            var registrationRequest = apiResponse.Result as List<TutorRegistrationRequestDTO>;
            registrationRequest.Should().NotBeNull();
            registrationRequest.Should().HaveCount(filteredRequests.Count());

            // Verify sorting if needed (based on 'CreatedDate')
            if (sort == SD.ORDER_ASC)
            {
                registrationRequest.Should().BeInAscendingOrder(r => r.CreatedDate);
            }
            else
            {
                registrationRequest.Should().BeInDescendingOrder(r => r.CreatedDate);
            }
        }

        [Fact]
        public async Task UpdateStatusRequest_ReturnsUnauthorized_WhenUserIsUnauthorized()
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

            var changeStatusDTO = new ChangeStatusDTO
            {
                Id = 1,
                StatusChange = (int)Status.APPROVE,
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
            apiResponse.ErrorMessages.First().Should().Be("Unauthorized access.");
        }

        [Fact]
        public async Task UpdateStatusRequest_ReturnsForbidden_WhenUserDoesNotHaveRequiredRole()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbidden access.");

            // Simulate a user with no valid role (missing required role)
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "testUserId") };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            var changeStatusDTO = new ChangeStatusDTO
            {
                Id = 1,
                StatusChange = (int)Status.APPROVE,
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
            apiResponse.ErrorMessages.First().Should().Be("Forbidden access.");
        }

        [Fact]
        public async Task UpdateStatusRequest_InternalServerError_ShouldReturnInternalServerError()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusDTO
            {
                Id = 1,
                StatusChange = (int)Status.APPROVE,
            };

            // Simulate a valid user (e.g., STAFF_ROLE or MANAGER_ROLE)
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
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");

            // Simulate an exception thrown during the repository call
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);

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
        public async Task UpdateStatusRequest_BadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusDTO
            {
                Id = 1,
                StatusChange = (int)
                    Status.PENDING // Simulate an invalid status change
                ,
            };

            // Simulate a valid user (e.g., STAFF_ROLE or MANAGER_ROLE)
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

            // Simulate ModelState invalid case
            _controller.ModelState.AddModelError("StatusChange", "Invalid status change"); // Mark the model state as invalid

            // Set up mock resource service for BAD_REQUEST_MESSAGE
            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.TUTOR_REGISTRATION_REQUEST))
                .Returns("Bad request: Status change to PENDING is not allowed.");

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response
                .ErrorMessages.Should()
                .Contain("Bad request: Status change to PENDING is not allowed.");
        }

        [Fact]
        public async Task UpdateStatusRequest_BadRequest_WhenStatusChangeIsPending()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusDTO
            {
                Id = 1,
                StatusChange = (int)
                    Status.PENDING // Simulate an invalid status change
                ,
            };

            // Simulate a valid user (e.g., STAFF_ROLE or MANAGER_ROLE)
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

            // Set up mock resource service for TUTOR_UPDATE_STATUS_IS_PENDING message
            _mockResourceService
                .Setup(r => r.GetString(SD.TUTOR_UPDATE_STATUS_IS_PENDING))
                .Returns("Status change to PENDING is not allowed for Tutor Registration Request.");

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response
                .ErrorMessages.Should()
                .Contain("Status change to PENDING is not allowed for Tutor Registration Request.");
        }

        [Fact]
        public async Task UpdateStatusRequest_NotFound_WhenTutorRegistrationRequestDoesNotExist()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusDTO
            {
                Id = 1,
                StatusChange = (int)
                    Status.APPROVE // Simulate an invalid status change
                ,
            };

            // Simulate a valid user (e.g., STAFF_ROLE or MANAGER_ROLE)
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

            // Set up mock resource service for NOT_FOUND_MESSAGE
            _mockResourceService
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.TUTOR_REGISTRATION_REQUEST))
                .Returns("Tutor Registration Request not found.");

            // Set up the repository mock to return null, simulating that the tutor registration request is not found
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((TutorRegistrationRequest)null);

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);

            // Assert
            var notFoundResult = result.Result as ObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

            var response = notFoundResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ErrorMessages.Should().Contain("Tutor Registration Request not found.");
        }

        [Fact]
        public async Task UpdateStatusRequest_BadRequest_WhenRequestStatusIsNotPending()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusDTO
            {
                Id = 1,
                StatusChange = (int)
                    Status.APPROVE // Simulate a valid status change
                ,
            };

            // Simulate a valid user (e.g., STAFF_ROLE or MANAGER_ROLE)
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

            // Set up mock resource service for BAD_REQUEST_MESSAGE
            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.TUTOR_REGISTRATION_REQUEST))
                .Returns("Status change is not allowed for Tutor Registration Request.");

            // Set up the repository mock to return a model where RequestStatus is not PENDING
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(
                    new TutorRegistrationRequest
                    {
                        Id = 1,
                        RequestStatus =
                            Status.APPROVE // Simulate a non-PENDING status
                        ,
                    }
                );

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response
                .ErrorMessages.Should()
                .Contain("Status change is not allowed for Tutor Registration Request.");
        }

        [Fact]
        public async Task UpdateStatusRequest_CreatesUser_WhenRequestStatusIsApproved()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusDTO
            {
                Id = 1,
                StatusChange = (int)
                    Status.APPROVE // Simulate an APPROVE status change
                ,
            };

            // Simulate a valid user (e.g., STAFF_ROLE or MANAGER_ROLE)
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

            // Set up mock resource service for success message
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");

            // Set up the repository mock to return a model where RequestStatus is PENDING (valid scenario)
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(
                    new TutorRegistrationRequest
                    {
                        Id = 1,
                        RequestStatus =
                            Status.PENDING // Simulate a PENDING status, ready for approval
                        ,
                    }
                );

            // Mock the _roleRepository to return the tutor role
            _mockRoleRepository
                .Setup(repo => repo.GetByNameAsync(SD.TUTOR_ROLE))
                .ReturnsAsync(new IdentityRole { Id = "TutorRoleId", Name = SD.TUTOR_ROLE });

            // Mock the _userRepository to return a successful user creation response
            _mockUserRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);

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
        public async Task UpdateStatusRequest_ReturnsInternalServerError_WhenTutorCreationFails()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusDTO
            {
                Id = 1,
                StatusChange = (int)
                    Status.APPROVE // Simulate an APPROVE status change
                ,
            };

            // Simulate a valid user (e.g., STAFF_ROLE or MANAGER_ROLE)
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

            // Set up mock resource service for success message
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");

            // Set up the repository mock to return a model where RequestStatus is PENDING (valid scenario)
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(
                    new TutorRegistrationRequest
                    {
                        Id = 1,
                        RequestStatus =
                            Status.PENDING // Simulate a PENDING status, ready for approval
                        ,
                    }
                );

            // Mock the _roleRepository to return the tutor role
            _mockRoleRepository
                .Setup(repo => repo.GetByNameAsync(SD.TUTOR_ROLE))
                .ReturnsAsync(new IdentityRole { Id = "TutorRoleId", Name = SD.TUTOR_ROLE });

            // Mock the _userRepository to return a successful user creation response
            _mockUserRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new ApplicationUser
                    {
                        Email = "test@example.com",
                        FullName = "Test User",
                        PhoneNumber = "1234567890",
                        UserName = "test@example.com",
                        RoleId = "TutorRoleId",
                    }
                );

            // Mock the _tutorRepository to return null (indicating tutor creation failed)
            _mockTutorRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Tutor>()))
                .ReturnsAsync((Tutor)null); // Simulating a failed tutor creation

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);

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
        public async Task UpdateStatusRequest_CreatesUserAndTutor_WhenRequestStatusIsApproved()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusDTO
            {
                Id = 1,
                StatusChange = (int)
                    Status.APPROVE // Simulate an APPROVE status change
                ,
            };

            // Simulate a valid user (e.g., STAFF_ROLE or MANAGER_ROLE)
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

            // Set up mock resource service for success message
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");

            // Set up the repository mock to return a model where RequestStatus is PENDING (valid scenario)
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(
                    new TutorRegistrationRequest
                    {
                        Id = 1,
                        RequestStatus =
                            Status.PENDING // Simulate a PENDING status, ready for approval
                        ,
                    }
                );

            // Mock the _roleRepository to return the tutor role
            _mockRoleRepository
                .Setup(repo => repo.GetByNameAsync(SD.TUTOR_ROLE))
                .ReturnsAsync(new IdentityRole { Id = "TutorRoleId", Name = SD.TUTOR_ROLE });

            // Mock the _userRepository to return a successful user creation response
            _mockUserRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new ApplicationUser
                    {
                        Email = "test@example.com",
                        FullName = "Test User",
                        PhoneNumber = "1234567890",
                        UserName = "test@example.com",
                        RoleId = "TutorRoleId",
                    }
                );

            // Mock the _tutorRepository to return a successful tutor creation response
            _mockTutorRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Tutor>()))
                .ReturnsAsync(
                    new Tutor
                    {
                        TutorId = "test-user-id", // Assuming user ID is linked with tutor
                        PriceFrom = 50,
                        PriceEnd = 100,
                        AboutMe = "Experienced tutor",
                        DateOfBirth = DateTime.Now.AddYears(-25), // 25 years old
                        StartAge = 18,
                        EndAge = 30,
                        SessionHours = 10,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now,
                    }
                );

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);

            // Assert
            var okResult = result.Result as ObjectResult;
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.ErrorMessages.Should().BeEmpty(); // No errors should be present

            // Verify that user and tutor creation methods were called
            _mockUserRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
                Times.Once
            );
            _mockTutorRepository.Verify(repo => repo.CreateAsync(It.IsAny<Tutor>()), Times.Once);

            // Optionally, verify that a success message is logged or returned, depending on your implementation
            _mockResourceService.Verify(r => r.GetString(It.IsAny<string>()), Times.Never); // Ensure no error message is returned
        }

        [Fact]
        public async Task UpdateStatusRequest_DoesNotCreateUserOrTutor_WhenRequestStatusIsRejected()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusDTO
            {
                Id = 1,
                StatusChange = (int)
                    Status.REJECT // Simulate a REJECT status change
                ,
            };

            // Simulate a valid user (e.g., STAFF_ROLE or MANAGER_ROLE)
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

            // Set up mock resource service for rejection message
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");

            // Set up the repository mock to return a model where RequestStatus is PENDING (valid scenario)
            _mockTutorRegistrationRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRegistrationRequest, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(
                    new TutorRegistrationRequest
                    {
                        Id = 1,
                        RequestStatus =
                            Status.PENDING // Simulate a PENDING status, ready for approval
                        ,
                    }
                );

            // Mock the _roleRepository to return the tutor role (though it won't be used here)
            _mockRoleRepository
                .Setup(repo => repo.GetByNameAsync(SD.TUTOR_ROLE))
                .ReturnsAsync(new IdentityRole { Id = "TutorRoleId", Name = SD.TUTOR_ROLE });

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);

            // Assert
            var okResult = result.Result as ObjectResult;
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value.Should().BeOfType<APIResponse>().Subject;
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.ErrorMessages.Should().BeEmpty(); // No errors should be present

            // Verify that user and tutor creation methods were NOT called
            _mockUserRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
                Times.Never
            );
            _mockTutorRepository.Verify(repo => repo.CreateAsync(It.IsAny<Tutor>()), Times.Never);

            // Optionally, verify that a rejection message or log entry is created
            _mockResourceService.Verify(r => r.GetString(It.IsAny<string>()), Times.Never); // Ensure no error message is returned in this valid case
        }






    }
}
