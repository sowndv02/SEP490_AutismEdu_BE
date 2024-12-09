using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using AutismEduConnectSystem.Controllers.v1;
using AutismEduConnectSystem.Mapper;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.RabbitMQSender;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.SignalR;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Org.BouncyCastle.Asn1.Ocsp;
using Xunit;
using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.Controllers.v1.Tests
{
    public class StudentProfileControllerTests
    {
        private readonly Mock<IStudentProfileRepository> _mockStudentProfileRepository;
        private readonly Mock<IScheduleTimeSlotRepository> _mockScheduleTimeSlotRepository;
        private readonly Mock<IInitialAssessmentResultRepository> _mockInitialAssessmentResultRepository;
        private readonly Mock<IAssessmentQuestionRepository> _mockAssessmentQuestionRepository;
        private readonly Mock<IChildInformationRepository> _mockChildInfoRepository;
        private readonly Mock<ITutorRequestRepository> _mockTutorRequestRepository;
        private readonly Mock<ITutorRepository> _mockTutorRepository;
        private readonly Mock<IRabbitMQMessageSender> _mockMessageBus;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IRoleRepository> _mockRoleRepository;
        private readonly Mock<IBlobStorageRepository> _mockBlobStorageRepository;
        private readonly Mock<IScheduleRepository> _mockScheduleRepository;
        private readonly Mock<IResourceService> _mockResourceService;
        private readonly Mock<ILogger<StudentProfileController>> _mockLogger;
        private readonly Mock<INotificationRepository> _mockNotificationRepository;
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly IMapper _mockMapper;
        private readonly Mock<IConfiguration> _mockConfiguration;

        private readonly StudentProfileController _controller;

        public StudentProfileControllerTests()
        {
            _mockStudentProfileRepository = new Mock<IStudentProfileRepository>();
            _mockScheduleTimeSlotRepository = new Mock<IScheduleTimeSlotRepository>();
            _mockInitialAssessmentResultRepository = new Mock<IInitialAssessmentResultRepository>();
            _mockAssessmentQuestionRepository = new Mock<IAssessmentQuestionRepository>();
            _mockChildInfoRepository = new Mock<IChildInformationRepository>();
            _mockTutorRequestRepository = new Mock<ITutorRequestRepository>();
            _mockTutorRepository = new Mock<ITutorRepository>();
            _mockMessageBus = new Mock<IRabbitMQMessageSender>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockRoleRepository = new Mock<IRoleRepository>();
            _mockBlobStorageRepository = new Mock<IBlobStorageRepository>();
            _mockScheduleRepository = new Mock<IScheduleRepository>();
            _mockResourceService = new Mock<IResourceService>();
            _mockLogger = new Mock<ILogger<StudentProfileController>>();
            _mockNotificationRepository = new Mock<INotificationRepository>();
            _mockHubContext = new Mock<IHubContext<NotificationHub>>();
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingConfig());
            });
            _mockMapper = config.CreateMapper();
            _mockConfiguration = new Mock<IConfiguration>();

            _mockConfiguration.SetupGet(c => c["APIConfig:PageSize"]).Returns("10");
            _mockConfiguration.SetupGet(c => c["RabbitMQSettings:QueueName"]).Returns("some-queue");

            _controller = new StudentProfileController(
                _mockStudentProfileRepository.Object,
                _mockAssessmentQuestionRepository.Object,
                _mockScheduleTimeSlotRepository.Object,
                _mockInitialAssessmentResultRepository.Object,
                _mockChildInfoRepository.Object,
                _mockTutorRequestRepository.Object,
                _mockMapper,
                _mockConfiguration.Object,
                _mockTutorRepository.Object,
                _mockMessageBus.Object,
                _mockUserRepository.Object,
                _mockRoleRepository.Object,
                _mockBlobStorageRepository.Object,
                _mockScheduleRepository.Object,
                _mockResourceService.Object,
                _mockLogger.Object,
                _mockNotificationRepository.Object,
                _mockHubContext.Object
            );
        }

        [Fact]
        public async Task CreateAsync_ParentAndChildCreationWithTutorRequestIdGreaterThanZero_ShouldReturnCreated()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Create a valid DTO with a tutor request and child ID greater than zero
            var studentProfileDto = new StudentProfileCreateDTO
            {
                Email = null,
                ParentFullName = null,
                Address = null,
                PhoneNumber = null,
                ChildName = null,
                isMale = null,
                BirthDate = null,
                Media = null,
                TutorRequestId = 1, // Tutor Request Id > 0
                ChildId = 1, // Child Id > 0
                InitialCondition = "Diagnosed with mild autism",
                InitialAssessmentResults = new List<InitialAssessmentResultCreateDTO>(),
                ScheduleTimeSlots = new List<ScheduleTimeSlotCreateDTO>
                {
                    new ScheduleTimeSlotCreateDTO
                    {
                        Weekday = 1,
                        From = "8:0", // 08:00 AM
                        To = "9:0", // 09:00 AM
                    },
                },
            };

            // Mock the parent account creation to return a valid user
            var createdParentUser = new ApplicationUser
            {
                Id = "test-parent-id",
                UserName = "parent@example.com",
                Email = "parent@example.com",
            };
            _mockUserRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(createdParentUser); // Simulate successful parent creation

            // Mock the child profile creation to return a valid child profile
            var createdChild = new ChildInformation
            {
                Id = 1,
                Name = "Jane Doe",
                ParentId = "test-parent-id",
            };
            _mockChildInfoRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<ChildInformation>()))
                .ReturnsAsync(createdChild); // Simulate successful child creation

            _mockRoleRepository
                .Setup(repo => repo.GetByNameAsync(SD.PARENT_ROLE))
                .ReturnsAsync(new IdentityRole { Id = "Parent-role-Id" });

            _mockChildInfoRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(createdChild);
            _mockTutorRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorRequest, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(new TutorRequest() { Id = 1, HasStudentProfile = false });
            _mockTutorRequestRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<TutorRequest>()))
                .ReturnsAsync(new TutorRequest() { Id = 1, HasStudentProfile = true });
            // Mock the tutor to be assigned
            var tutor = new Tutor
            {
                User = new ApplicationUser
                {
                    Id = "test-tutor-id",
                    UserName = "test-tutor",
                    Email = "tutor@example.com",
                },
            };

            var studentProfile = new StudentProfile
            {
                ChildId = 1,
                TutorId = "test-tutor-id",
                Status = SD.StudentProfileStatus.Pending,
                Tutor = tutor,
                ScheduleTimeSlots = new List<ScheduleTimeSlot>(),
            };

            // Mock the CreateAsync method to return the created student profile
            _mockStudentProfileRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<StudentProfile>()))
                .ReturnsAsync(studentProfile);

            // Act
            var result = await _controller.CreateAsync(studentProfileDto);

            // Assert
            var createdResult = result.Result as ObjectResult;
            createdResult.Should().NotBeNull();
            createdResult.StatusCode.Should().Be((int)HttpStatusCode.OK); // Ensure it returns Created (201)

            var apiResponse = createdResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            apiResponse.ErrorMessages.Should().BeEmpty();

            _mockStudentProfileRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<StudentProfile>()),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateAsync_ParentAndChildCreationSuccessful_ShouldReturnCreated()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Create a valid DTO with a new parent profile
            var studentProfileDto = new StudentProfileCreateDTO
            {
                Email = "parent@example.com",
                ParentFullName = "John Doe",
                Address = "123 Elm Street, Springfield",
                PhoneNumber = "123-456-7890",
                ChildName = "Jane Doe",
                isMale = false,
                BirthDate = new DateTime(2015, 5, 20),
                Media = Mock.Of<IFormFile>(),
                TutorRequestId = 0,
                ChildId = 0,
                InitialCondition = "Diagnosed with mild autism",
                InitialAssessmentResults = new List<InitialAssessmentResultCreateDTO>(),
                ScheduleTimeSlots = new List<ScheduleTimeSlotCreateDTO>
                {
                    new ScheduleTimeSlotCreateDTO
                    {
                        Weekday = 1,
                        From = "8:0", // 08:00 AM
                        To = "9:0", // 09:00 AM
                    },
                },
            };

            // Mock the parent account creation to return a valid user
            var createdParentUser = new ApplicationUser
            {
                Id = "test-parent-id",
                UserName = "parent@example.com",
                Email = "parent@example.com",
            };
            _mockUserRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(createdParentUser); // Simulate successful parent creation

            // Mock the child profile creation to return a valid child profile
            var createdChild = new ChildInformation
            {
                Id = 1,
                Name = "Jane Doe",
                ParentId = "test-parent-id",
            };
            _mockChildInfoRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<ChildInformation>()))
                .ReturnsAsync(createdChild); // Simulate successful child creation

            _mockRoleRepository
                .Setup(repo => repo.GetByNameAsync(SD.PARENT_ROLE))
                .ReturnsAsync(new IdentityRole { Id = "Parent-role-Id" });
            _mockChildInfoRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(createdChild);
            var tutor = new Tutor
            {
                User = new ApplicationUser
                {
                    Id = "test-tutor-id",
                    UserName = "test-tutor",
                    Email = "tutor@example.com",
                },
            };
            var studentProfile = new StudentProfile
            {
                ChildId = 1,
                TutorId = "test-tutor-id",
                Status = SD.StudentProfileStatus.Pending,
                Tutor = tutor,
                ScheduleTimeSlots = new List<ScheduleTimeSlot>(),
            };
            _mockStudentProfileRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<StudentProfile>()))
                .ReturnsAsync(studentProfile);
            // Act
            var result = await _controller.CreateAsync(studentProfileDto);

            // Assert
            var createdResult = result.Result as ObjectResult;
            createdResult.Should().NotBeNull();
            createdResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = createdResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            apiResponse.ErrorMessages.Should().BeEmpty();

            // Verify that both the parent and child repositories were called to create the records
            _mockUserRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
                Times.Once
            );
            _mockChildInfoRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<ChildInformation>()),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateAsync_ChildNotFound_ShouldReturnNotFound()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Create a valid DTO with a new parent profile
            var studentProfileDto = new StudentProfileCreateDTO
            {
                Email = "parent@example.com",
                ParentFullName = "John Doe",
                Address = "123 Elm Street, Springfield",
                PhoneNumber = "123-456-7890",
                ChildName = "Jane Doe",
                isMale = false,
                BirthDate = new DateTime(2015, 5, 20),
                Media = null,
                TutorRequestId = 1,
                ChildId = 1, // Child ID that does not exist
                InitialCondition = "Diagnosed with mild autism",
                InitialAssessmentResults = new List<InitialAssessmentResultCreateDTO>(),
                ScheduleTimeSlots = new List<ScheduleTimeSlotCreateDTO>
                {
                    new ScheduleTimeSlotCreateDTO
                    {
                        Weekday = 1,
                        From = "8:0", // 08:00 AM
                        To = "9:0", // 09:00 AM
                    },
                },
            };

            // Mock the repository to simulate that the child with the given ID does not exist
            _mockChildInfoRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((ChildInformation)null); // Simulate child not found

            // Mock the Resource Service to return the correct error message for missing child
            _mockResourceService
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.CHILD_INFO))
                .Returns("Child information not found.");

            // Act
            var result = await _controller.CreateAsync(studentProfileDto);

            // Assert
            var notFoundResult = result.Result as ObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

            var apiResponse = notFoundResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
            apiResponse.ErrorMessages.Should().Contain("Child information not found.");

            // Verify that the repository was called to check for child information
            _mockChildInfoRepository.Verify(
                repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    ),
                Times.Once
            );

            // Verify that no further actions are performed due to missing child information
            _mockStudentProfileRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<StudentProfile>()),
                Times.Never
            );
        }

        [Fact]
        public async Task CreateAsync_DuplicateStudentProfile_ShouldReturnBadRequest()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Create a valid DTO with a new parent profile
            var studentProfileDto = new StudentProfileCreateDTO
            {
                Email = null,
                ParentFullName = null,
                Address = null,
                PhoneNumber = null,
                ChildName = null,
                isMale = null,
                BirthDate = null,
                Media = null,
                TutorRequestId = 1,
                ChildId = 1, // Child ID that already exists
                InitialCondition = "Diagnosed with mild autism",
                InitialAssessmentResults = new List<InitialAssessmentResultCreateDTO>(),
                ScheduleTimeSlots = new List<ScheduleTimeSlotCreateDTO>
                {
                    new ScheduleTimeSlotCreateDTO
                    {
                        Weekday = 1,
                        From = "8:0", // 08:00 AM
                        To = "9:0", // 09:00 AM
                    },
                },
            };

            // Simulate an existing student profile for the same child and tutor
            var existingStudentProfile = new StudentProfile
            {
                ChildId = 1,
                TutorId = "test-tutor-id",
                Status =
                    SD.StudentProfileStatus.Teaching // Status is either Teaching or Pending
                ,
            };

            // Mock the repository to return an existing student profile for the same child and tutor
            _mockStudentProfileRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(existingStudentProfile); // Simulate duplicate

            // Mock the Resource Service to return the correct error message for duplicate data
            _mockResourceService
                .Setup(r => r.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.STUDENT_PROFILE))
                .Returns("A student profile already exists for the child.");

            // Act
            var result = await _controller.CreateAsync(studentProfileDto);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            apiResponse
                .ErrorMessages.Should()
                .Contain("A student profile already exists for the child.");

            _mockStudentProfileRepository.Verify(
                repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    ),
                Times.Once
            );

            _mockStudentProfileRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<StudentProfile>()),
                Times.Never
            );
        }

        [Fact]
        public async Task CreateAsync_ParentNotCreatedAndChildIdLessThanOrEqualToZero_ShouldReturnBadRequest()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Create a valid DTO with a new parent profile and invalid ChildId
            var studentProfileDto = new StudentProfileCreateDTO
            {
                Email = "parent@example.com",
                ParentFullName = "John Doe",
                Address = "123 Elm Street, Springfield",
                PhoneNumber = "123-456-7890",
                ChildName = "Jane Doe",
                isMale = false,
                BirthDate = new DateTime(2015, 5, 20),
                Media = null,
                TutorRequestId = 0,
                ChildId = 0, // Invalid ChildId (should be > 0 for valid child creation)
                InitialCondition = "Diagnosed with mild autism",
                InitialAssessmentResults = new List<InitialAssessmentResultCreateDTO>(),
                ScheduleTimeSlots = new List<ScheduleTimeSlotCreateDTO>
                {
                    new ScheduleTimeSlotCreateDTO
                    {
                        Weekday = 1,
                        From = "8:0", // 08:00 AM
                        To = "9:0", // 09:00 AM
                    },
                },
            };

            // Mock the user repository to simulate parent creation failure
            _mockUserRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser)null);

            // Mock the Resource Service to return the correct error message for internal server error
            _mockResourceService
                .Setup(r => r.GetString(SD.MISSING_2_INFORMATIONS, SD.PARENT, SD.CHILD))
                .Returns("Missing information of parent and child.");

            // Act
            var result = await _controller.CreateAsync(studentProfileDto);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            apiResponse.ErrorMessages.Should().Contain("Missing information of parent and child.");
        }

        [Fact]
        public async Task CreateAsync_UserAndChildCreationFails_ShouldReturnInternalServerError()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Create a valid DTO with a new parent profile
            var studentProfileDto = new StudentProfileCreateDTO
            {
                Email = "parent@example.com",
                ParentFullName = "John Doe",
                Address = "123 Elm Street, Springfield",
                PhoneNumber = "123-456-7890",
                ChildName = "Jane Doe",
                isMale = false,
                BirthDate = new DateTime(2015, 5, 20),
                Media = Mock.Of<IFormFile>(),
                TutorRequestId = 0,
                ChildId = 0,
                InitialCondition = "Diagnosed with mild autism",
                InitialAssessmentResults = new List<InitialAssessmentResultCreateDTO>(),
                ScheduleTimeSlots = new List<ScheduleTimeSlotCreateDTO>
                {
                    new ScheduleTimeSlotCreateDTO
                    {
                        Weekday = 1,
                        From = "8:0", // 08:00 AM
                        To = "9:0", // 09:00 AM
                    },
                },
            };

            // Mock the user repository to simulate parent creation failure
            _mockUserRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser)null); // Simulate failure (null returned)

            // Mock the Resource Service to return the correct error message for internal server error
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("An internal server error occurred while creating the user.");

            // Act
            var result = await _controller.CreateAsync(studentProfileDto);

            // Assert
            var internalServerErrorResult = result.Result as ObjectResult;
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult
                .StatusCode.Should()
                .Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = internalServerErrorResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            apiResponse
                .ErrorMessages.Should()
                .Contain("An internal server error occurred while creating the user.");
        }

        [Fact]
        public async Task CreateAsync_UserCreationFails_ShouldReturnInternalServerError()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Create a valid DTO with a new parent profile
            var studentProfileDto = new StudentProfileCreateDTO
            {
                Email = "parent@example.com",
                ParentFullName = "John Doe",
                Address = "123 Elm Street, Springfield",
                PhoneNumber = "123-456-7890",
                ChildName = "Jane Doe",
                isMale = false,
                BirthDate = new DateTime(2015, 5, 20),
                Media = Mock.Of<IFormFile>(),
                TutorRequestId = 0,
                ChildId = 0,
                InitialCondition = "Diagnosed with mild autism",
                InitialAssessmentResults = new List<InitialAssessmentResultCreateDTO>(),
                ScheduleTimeSlots = new List<ScheduleTimeSlotCreateDTO>
                {
                    new ScheduleTimeSlotCreateDTO
                    {
                        Weekday = 1,
                        From = "8:0", // 08:00 AM
                        To = "9:0", // 09:00 AM
                    },
                },
            };

            // Mock the user repository to simulate user creation failure
            _mockUserRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser)null); // Simulate failure (null returned)

            // Mock the Resource Service to return the correct error message for internal server error
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("An internal server error occurred while creating the user.");

            // Act
            var result = await _controller.CreateAsync(studentProfileDto);

            // Assert
            var internalServerErrorResult = result.Result as ObjectResult;
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult
                .StatusCode.Should()
                .Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = internalServerErrorResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            apiResponse
                .ErrorMessages.Should()
                .Contain("An internal server error occurred while creating the user.");
        }

        [Fact]
        public async Task CreateAsync_DuplicateEmail_ShouldReturnBadRequest()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Create a valid DTO with the email that already exists in the system
            var studentProfileDto = new StudentProfileCreateDTO
            {
                Email = "parent@example.com", // This email is considered a duplicate
                ParentFullName = "John Doe",
                Address = "123 Elm Street, Springfield",
                PhoneNumber = "123-456-7890",
                ChildName = "Jane Doe",
                isMale = false,
                BirthDate = new DateTime(2015, 5, 20),
                Media = Mock.Of<IFormFile>(),
                TutorRequestId = 0,
                ChildId = 0,
                InitialCondition = "Diagnosed with mild autism",
                InitialAssessmentResults = new List<InitialAssessmentResultCreateDTO>(),
                ScheduleTimeSlots = new List<ScheduleTimeSlotCreateDTO>
                {
                    new ScheduleTimeSlotCreateDTO
                    {
                        Weekday = 1,
                        From = "8:0", // 08:00 AM
                        To = "9:0", // 09:00 AM
                    },
                },
            };

            // Mock the user repository to return a user with the same email
            var existingUser = new ApplicationUser
            {
                Email = "parent@example.com",
                UserName = "parent@example.com",
            };

            _mockUserRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(existingUser); // Simulate existing user with the same email

            // Mock the Resource Service to return the correct error message
            _mockResourceService
                .Setup(r => r.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.EMAIL))
                .Returns("The email already exists in the system.");

            // Act
            var result = await _controller.CreateAsync(studentProfileDto);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            apiResponse.ErrorMessages.Should().Contain("The email already exists in the system.");
        }

        [Fact]
        public async Task CreateAsync_TutorRequestIdGreaterThanZero_ShouldReturnBadRequest()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Create a valid DTO with TutorRequestId greater than 0
            var studentProfileDto = new StudentProfileCreateDTO
            {
                Email = "parent@example.com",
                ParentFullName = "John Doe",
                Address = "123 Elm Street, Springfield",
                PhoneNumber = "123-456-7890",
                ChildName = "Jane Doe",
                isMale = false,
                BirthDate = new DateTime(2015, 5, 20),
                Media = Mock.Of<IFormFile>(),
                TutorRequestId = 1, // This is greater than 0 and should trigger a BadRequest
                ChildId = 0,
                InitialCondition = "Diagnosed with mild autism",
                InitialAssessmentResults = new List<InitialAssessmentResultCreateDTO>(),
                ScheduleTimeSlots = new List<ScheduleTimeSlotCreateDTO>
                {
                    new ScheduleTimeSlotCreateDTO
                    {
                        Weekday = 1,
                        From = "8:0", // 08:00 AM
                        To = "9:0", // 09:00 AM
                    },
                },
            };

            // Mock the Resource Service to return the correct error message
            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.STUDENT_PROFILE))
                .Returns("Invalid request: TutorRequestId should not be greater than 0.");

            // Act
            var result = await _controller.CreateAsync(studentProfileDto);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            apiResponse
                .ErrorMessages.Should()
                .Contain("Invalid request: TutorRequestId should not be greater than 0.");
        }

        [Fact]
        public async Task CreateAsync_DuplicateTimeSlotInDatabase_ShouldReturnBadRequest()
        {
            // Simulate a valid user with TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Arrange
            var studentProfileDto = new StudentProfileCreateDTO
            {
                Email = "parent@example.com",
                ParentFullName = "John Doe",
                Address = "123 Elm Street, Springfield",
                PhoneNumber = "123-456-7890",
                ChildName = "Jane Doe",
                isMale = false,
                BirthDate = new DateTime(2015, 5, 20),
                Media = null,
                TutorRequestId = 1,
                ChildId = 0,
                InitialCondition = "Diagnosed with mild autism",
                InitialAssessmentResults = new List<InitialAssessmentResultCreateDTO>(),
                ScheduleTimeSlots = new List<ScheduleTimeSlotCreateDTO>
                {
                    new ScheduleTimeSlotCreateDTO
                    {
                        Weekday = 1,
                        From = "8:0", // 08:00 AM
                        To = "9:0", // 09:00 AM
                    },
                },
            };

            // Mock a duplicate time slot in the database (same weekday, time range, and tutor)
            var duplicateTimeSlot = new ScheduleTimeSlot
            {
                From = TimeSpan.FromHours(8.0), // 08:00 AM
                To = TimeSpan.FromHours(9.0), // 09:00 AM
                Weekday = 1,
                StudentProfile = new StudentProfile
                {
                    TutorId = "test-tutor-id",
                    Status = SD.StudentProfileStatus.Teaching, // Assume this is the status we care about for the check
                },
                IsDeleted = false,
            };

            // Mock the repository to return a duplicate time slot
            _mockScheduleTimeSlotRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ScheduleTimeSlot, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(duplicateTimeSlot); // Return a duplicate time slot

            _mockResourceService
                .Setup(r =>
                    r.GetString(SD.TIMESLOT_DUPLICATED_MESSAGE, SD.TIME_SLOT, "08:00", "09:00")
                )
                .Returns("The time slot from 08:00 to 09:00 already exists.");

            // Act
            var result = await _controller.CreateAsync(studentProfileDto);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            apiResponse
                .ErrorMessages.Should()
                .Contain("The time slot from 08:00 to 09:00 already exists.");
        }

        [Fact]
        public async Task CreateAsync_InvalidTimeSlotIn_ShouldReturnBadRequest()
        {
            // Simulate a valid user with TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Arrange
            var studentProfileDto = new StudentProfileCreateDTO
            {
                Email = "parent@example.com",
                ParentFullName = "John Doe",
                Address = "123 Elm Street, Springfield",
                PhoneNumber = "123-456-7890",
                ChildName = "Jane Doe",
                isMale = false,
                BirthDate = new DateTime(2015, 5, 20),
                Media = null,
                TutorRequestId = 1,
                ChildId = 0,
                InitialCondition = "Diagnosed with mild autism",
                InitialAssessmentResults = new List<InitialAssessmentResultCreateDTO>(),
                ScheduleTimeSlots = new List<ScheduleTimeSlotCreateDTO>
                {
                    // Duplicate and invalid time slots
                    new ScheduleTimeSlotCreateDTO
                    {
                        Weekday = 1,
                        From = "10:00",
                        To = "09:00",
                    },
                },
            };

            _mockResourceService
                .Setup(r => r.GetString(SD.DUPPLICATED_ASSIGN_EXERCISE, SD.TIME_SLOT, "Duplicated"))
                .Returns("Invalid or duplicate time slot detected");

            // Act
            var result = await _controller.CreateAsync(studentProfileDto);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            apiResponse.ErrorMessages.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task CreateAsync_DuplicateTimeSlot_ShouldReturnBadRequest()
        {
            // Simulate a valid user with TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Arrange
            var studentProfileDto = new StudentProfileCreateDTO
            {
                Email = "parent@example.com",
                ParentFullName = "John Doe",
                Address = "123 Elm Street, Springfield",
                PhoneNumber = "123-456-7890",
                ChildName = "Jane Doe",
                isMale = false,
                BirthDate = new DateTime(2015, 5, 20),
                Media = null,
                TutorRequestId = 1,
                ChildId = 0,
                InitialCondition = "Diagnosed with mild autism",
                InitialAssessmentResults = new List<InitialAssessmentResultCreateDTO>(),
                ScheduleTimeSlots = new List<ScheduleTimeSlotCreateDTO>
                {
                    // Duplicate and invalid time slots
                    new ScheduleTimeSlotCreateDTO
                    {
                        Weekday = 1,
                        From = "10:00",
                        To = "11:00",
                    },
                    new ScheduleTimeSlotCreateDTO
                    {
                        Weekday = 1,
                        From = "10:30", // Overlaps with the first slot
                        To = "11:30",
                    },
                    new ScheduleTimeSlotCreateDTO
                    {
                        Weekday = 2,
                        From = "02:00",
                        To = "01:00", // Invalid slot (From > To)
                    },
                },
            };

            _mockResourceService
                .Setup(r => r.GetString(SD.DUPPLICATED_ASSIGN_EXERCISE, SD.TIME_SLOT, "Duplicated"))
                .Returns("Invalid or duplicate time slot detected");

            // Act
            var result = await _controller.CreateAsync(studentProfileDto);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            apiResponse.ErrorMessages.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task CreateAsync_ModelStateInvalid_ShouldReturnBadRequest()
        {
            // Simulate a valid user with TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Arrange
            var studentProfileDto = new StudentProfileCreateDTO
            {
                Email = "", // Invalid data (empty email)
                ParentFullName = "John Doe",
                Address = "123 Elm Street, Springfield",
                PhoneNumber = "123-456-7890",
                ChildName = "Jane Doe",
                isMale = false,
                BirthDate = new DateTime(2015, 5, 20),
                Media = null,
                TutorRequestId = 1,
                ChildId = 0,
                InitialCondition = "", // Invalid data (empty initial condition)
                InitialAssessmentResults = new List<InitialAssessmentResultCreateDTO>(),
                ScheduleTimeSlots = new List<ScheduleTimeSlotCreateDTO>(),
            };

            _controller.ModelState.AddModelError("Email", "The Email field is required.");
            _controller.ModelState.AddModelError(
                "InitialCondition",
                "The InitialCondition field is required."
            );

            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.STUDENT_PROFILE))
                .Returns("Model state is invalid");

            // Act
            var result = await _controller.CreateAsync(studentProfileDto);

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            apiResponse.ErrorMessages.Should().Contain("Model state is invalid");
        }

        [Fact]
        public async Task CreateAsync_InternalServerError_ShouldReturnInternalServerError()
        {
            // Simulate a valid user with TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            // Arrange
            var studentProfileDto = new StudentProfileCreateDTO
            {
                Email = "parent@example.com",
                ParentFullName = "John Doe",
                Address = "123 Elm Street, Springfield",
                PhoneNumber = "123-456-7890",
                ChildName = "Jane Doe",
                isMale = false,
                BirthDate = new DateTime(2015, 5, 20),
                Media = null, // For test cases without media; replace with a mock IFormFile if needed
                TutorRequestId = 1, // Assuming a valid TutorRequestId for test
                ChildId = 0, // Assume 0 when creating a new account
                InitialCondition = "Diagnosed with mild autism",
                InitialAssessmentResults = new List<InitialAssessmentResultCreateDTO>
                {
                    new InitialAssessmentResultCreateDTO { QuestionId = 1, OptionId = 1 },
                    new InitialAssessmentResultCreateDTO { QuestionId = 2, OptionId = 2 },
                },
                ScheduleTimeSlots = new List<ScheduleTimeSlotCreateDTO>
                {
                    new ScheduleTimeSlotCreateDTO
                    {
                        Weekday = 1,
                        From = "10:00 AM",
                        To = "11:00 AM",
                    },
                    new ScheduleTimeSlotCreateDTO
                    {
                        Weekday = 2,
                        From = "1:00 PM",
                        To = "2:00 PM",
                    },
                },
            };

            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");

            // Simulate an exception thrown during the process
            _mockStudentProfileRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        true,
                        "InitialAndFinalAssessmentResults,ScheduleTimeSlots",
                        It.IsAny<string>()
                    )
                )
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateAsync(studentProfileDto);
            // Assert
            var internalServerErrorResult = result.Result as ObjectResult;
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult
                .StatusCode.Should()
                .Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = internalServerErrorResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            apiResponse.ErrorMessages.Should().Contain("Internal server error");
        }

        [Fact]
        public async Task CreateAsync_UserInValidRole_ReturnsForbidden()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbidden access.");

            // Simulate a user with valid ID but missing required role
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "testUserId") };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Arrange
            var studentProfileDto = new StudentProfileCreateDTO
            {
                Email = "parent@example.com",
                ParentFullName = "John Doe",
                Address = "123 Elm Street, Springfield",
                PhoneNumber = "123-456-7890",
                ChildName = "Jane Doe",
                isMale = false,
                BirthDate = new DateTime(2015, 5, 20),
                Media = null, // For test cases without media; replace with a mock IFormFile if needed
                TutorRequestId = 1, // Assuming a valid TutorRequestId for test
                ChildId = 0, // Assume 0 when creating a new account
                InitialCondition = "Diagnosed with mild autism",
                InitialAssessmentResults = new List<InitialAssessmentResultCreateDTO>
                {
                    new InitialAssessmentResultCreateDTO { QuestionId = 1, OptionId = 1 },
                    new InitialAssessmentResultCreateDTO { QuestionId = 2, OptionId = 2 },
                },
                ScheduleTimeSlots = new List<ScheduleTimeSlotCreateDTO>
                {
                    new ScheduleTimeSlotCreateDTO
                    {
                        Weekday = 1,
                        From = "10:00 AM",
                        To = "11:00 AM",
                    },
                    new ScheduleTimeSlotCreateDTO
                    {
                        Weekday = 2,
                        From = "1:00 PM",
                        To = "2:00 PM",
                    },
                },
            };

            _mockStudentProfileRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<StudentProfile>()))
                .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _controller.CreateAsync(studentProfileDto);

            // Assert
            var forbiddenResult = result.Result as ObjectResult;
            forbiddenResult.Should().NotBeNull();
            forbiddenResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

            var apiResponse = forbiddenResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            apiResponse.ErrorMessages.First().Should().Be("Forbidden access.");
        }

        [Fact]
        public async Task CreateAsync_UnauthorizedUser_ReturnsUnauthorized()
        {
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

            // Arrange
            var studentProfileDto = new StudentProfileCreateDTO
            {
                Email = "parent@example.com",
                ParentFullName = "John Doe",
                Address = "123 Elm Street, Springfield",
                PhoneNumber = "123-456-7890",
                ChildName = "Jane Doe",
                isMale = false,
                BirthDate = new DateTime(2015, 5, 20),
                Media = null, // For test cases without media; replace with a mock IFormFile if needed
                TutorRequestId = 1, // Assuming a valid TutorRequestId for test
                ChildId = 0, // Assume 0 when creating a new account
                InitialCondition = "Diagnosed with mild autism",
                InitialAssessmentResults = new List<InitialAssessmentResultCreateDTO>
                {
                    new InitialAssessmentResultCreateDTO { QuestionId = 1, OptionId = 1 },
                    new InitialAssessmentResultCreateDTO { QuestionId = 2, OptionId = 2 },
                },
                ScheduleTimeSlots = new List<ScheduleTimeSlotCreateDTO>
                {
                    new ScheduleTimeSlotCreateDTO
                    {
                        Weekday = 1,
                        From = "10:00 AM",
                        To = "11:00 AM",
                    },
                    new ScheduleTimeSlotCreateDTO
                    {
                        Weekday = 2,
                        From = "1:00 PM",
                        To = "2:00 PM",
                    },
                },
            };

            _mockStudentProfileRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<StudentProfile>()))
                .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _controller.CreateAsync(studentProfileDto);

            // Assert
            var unauthorizedResult = result.Result as ObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            var apiResponse = unauthorizedResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            apiResponse.ErrorMessages.First().Should().Be("Unauthorized access.");
        }

        [Fact]
        public async Task CloseTutoring_ReturnsUnauthorized_WhenUserIsUnauthorized()
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

            var requestPayload = new CloseTutoringCreatDTO { StudentProfileId = 1 };

            // Act
            var result = await _controller.CloseTutoring(requestPayload);

            // Assert
            var unauthorizedResult = result.Result as ObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            var apiResponse = unauthorizedResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            apiResponse.ErrorMessages.First().Should().Be("Unauthorized access.");
        }

        [Fact]
        public async Task CloseTutoring_ReturnsForbidden_WhenUserDoesNotHaveRequiredRole()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbidden access.");

            // Simulate a user with valid ID but missing required role
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "testUserId") };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var requestPayload = new CloseTutoringCreatDTO { StudentProfileId = 1 };

            // Act
            var result = await _controller.CloseTutoring(requestPayload);

            // Assert
            var forbiddenResult = result.Result as ObjectResult;
            forbiddenResult.Should().NotBeNull();
            forbiddenResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

            var apiResponse = forbiddenResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            apiResponse.ErrorMessages.First().Should().Be("Forbidden access.");
        }

        [Fact]
        public async Task CloseTutoring_InternalServerError_ShouldReturnInternalServerError()
        {
            // Arrange
            var requestPayload = new CloseTutoringCreatDTO { StudentProfileId = 1 };

            // Simulate a valid user with TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-tutor-id"),
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
            _mockStudentProfileRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        true,
                        "InitialAndFinalAssessmentResults,ScheduleTimeSlots",
                        It.IsAny<string>()
                    )
                )
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CloseTutoring(requestPayload);

            // Assert
            var internalServerErrorResult = result.Result as ObjectResult;
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult
                .StatusCode.Should()
                .Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = internalServerErrorResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            apiResponse.ErrorMessages.Should().Contain("Internal server error");
        }

        [Fact]
        public async Task CloseTutoring_BadRequest_ShouldReturnBadRequest_WhenStudentProfileIdIsInvalid()
        {
            // Arrange
            var requestPayload = new CloseTutoringCreatDTO
            {
                StudentProfileId =
                    0 // Invalid StudentProfileId (<= 0)
                ,
            };

            // Simulate a valid user with TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("The provided ID is invalid.");

            // Act
            var result = await _controller.CloseTutoring(requestPayload);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            apiResponse.ErrorMessages.Should().Contain("The provided ID is invalid.");
        }

        [Fact]
        public async Task CloseTutoring_BadRequest_ShouldReturnBadRequest_WhenCloseTutoringCreateDTOIsNullOrInvalid()
        {
            // Arrange
            CloseTutoringCreatDTO requestPayload = null; // Null DTO

            // Simulate a valid user with TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.END_TUTORING))
                .Returns("The end tutoring request is invalid.");

            // Act
            var result = await _controller.CloseTutoring(requestPayload);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            apiResponse.ErrorMessages.Should().Contain("The end tutoring request is invalid.");
        }

        [Fact]
        public async Task CloseTutoring_NotFound_ShouldReturnNotFound_WhenStudentProfileDoesNotExist()
        {
            // Arrange
            var requestPayload = new CloseTutoringCreatDTO
            {
                StudentProfileId =
                    1 // Valid ID but the student profile doesn't exist
                ,
            };

            // Simulate a valid user with TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            _mockResourceService
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.STUDENT_PROFILE))
                .Returns("The student profile was not found.");

            // Simulate repository returning null (student profile does not exist)
            _mockStudentProfileRepository
                .Setup(repo =>
                    repo.GetAsync(
                        x => x.Id == requestPayload.StudentProfileId,
                        true,
                        "InitialAndFinalAssessmentResults,ScheduleTimeSlots",
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((StudentProfile)null);

            // Act
            var result = await _controller.CloseTutoring(requestPayload);

            // Assert
            var notFoundResult = result.Result as ObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

            var apiResponse = notFoundResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
            apiResponse.ErrorMessages.Should().Contain("The student profile was not found.");
        }

        [Fact]
        public async Task CloseTutoring_ValidRequest_ShouldReturnNoContent()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-tutor-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var requestPayload = new CloseTutoringCreatDTO
            {
                StudentProfileId = 1, // Valid ID
                FinalCondition = "Tutoring completed successfully",
                FinalAssessmentResults = new List<InitialAssessmentResultCreateDTO>
                {
                    new InitialAssessmentResultCreateDTO { QuestionId = 1, OptionId = 1 },
                    new InitialAssessmentResultCreateDTO { QuestionId = 2, OptionId = 2 },
                },
            };

            var studentProfile = new StudentProfile
            {
                Id = 1,
                ChildId = 1,
                Status = StudentProfileStatus.Stop,
                UpdatedDate = DateTime.Now,
                InitialAndFinalAssessmentResults = new List<InitialAssessmentResult>(),
            };

            var schedules = new List<Schedule>
            {
                new Schedule
                {
                    Id = 1,
                    StudentProfileId = 1,
                    ScheduleDate = DateTime.Now.AddDays(1),
                    Start = TimeSpan.FromHours(9),
                },
                new Schedule
                {
                    Id = 2,
                    StudentProfileId = 1,
                    ScheduleDate = DateTime.Now.AddDays(-1),
                    Start = TimeSpan.FromHours(9),
                },
            };

            var childInfo = new ChildInformation
            {
                Id = 1,
                Name = "Test Child",
                Parent = new ApplicationUser { FullName = "Test Parent" },
            };

            var updatedStudentProfile = new StudentProfile
            {
                Id = 1,
                ChildId = 1,
                FinalCondition = "Condition After Tutoring",
                Status = StudentProfileStatus.Stop,
                UpdatedDate = DateTime.Now,
                InitialAndFinalAssessmentResults = new List<InitialAssessmentResult>
                {
                    new InitialAssessmentResult
                    {
                        Id = 1,
                        Question = new AssessmentQuestion(),
                        Option = new AssessmentOption(),
                    },
                },
                Child = childInfo,
            };

            // Simulate repository calls and other dependencies
            _mockStudentProfileRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        true,
                        "InitialAndFinalAssessmentResults,ScheduleTimeSlots",
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(studentProfile);

            _mockStudentProfileRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<StudentProfile>()))
                .ReturnsAsync(updatedStudentProfile);

            _mockInitialAssessmentResultRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<InitialAssessmentResult, bool>>>(),
                        true,
                        "Question,Option",
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(
                    new InitialAssessmentResult
                    {
                        Id = 1,
                        Question = new AssessmentQuestion(),
                        Option = new AssessmentOption(),
                    }
                );

            _mockChildInfoRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        true,
                        "Parent",
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(childInfo);

            _mockScheduleRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Schedule, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<Schedule, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((schedules.Count, schedules));

            _mockScheduleRepository
                .Setup(repo => repo.RemoveAsync(It.IsAny<Schedule>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CloseTutoring(requestPayload);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var mappedResult = apiResponse.Result as StudentProfileDetailTutorDTO;
            mappedResult.Should().NotBeNull();
            mappedResult.Id.Should().Be(updatedStudentProfile.Id);
        }

        [Fact]
        public async Task GetAllAsync_InternalServerError_ShouldReturnInternalServerError_WhenExceptionOccurs()
        {
            // Arrange
            var requestQueryParams = new Dictionary<string, string>
            {
                { "status", "all" },
                { "sort", SD.ORDER_DESC },
                { "pageNumber", "1" },
            };

            // Simulate a valid user with TUTOR_ROLE
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
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");
            // Simulate an internal error by throwing an exception in the repository
            _mockStudentProfileRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<StudentProfile, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAllAsync(
                requestQueryParams["status"],
                requestQueryParams["sort"],
                int.Parse(requestQueryParams["pageNumber"])
            );

            // Assert
            var internalServerErrorResult = result.Result as ObjectResult;
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult
                .StatusCode.Should()
                .Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = internalServerErrorResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            apiResponse.ErrorMessages.Should().Contain("Internal server error");
        }

        [Fact]
        public async Task GetAllAsync_Unauthorized_ShouldReturnUnauthorized_WhenTutorIdIsMissing()
        {
            // Arrange
            var requestQueryParams = new Dictionary<string, string>
            {
                { "status", "all" },
                { "sort", SD.ORDER_DESC },
                { "pageNumber", "1" },
            };

            // Simulate a user with no tutorId (unauthorized)
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    string.Empty
                ) // No Tutor ID
                ,
            };
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
            var result = await _controller.GetAllAsync(
                requestQueryParams["status"],
                requestQueryParams["sort"],
                int.Parse(requestQueryParams["pageNumber"])
            );

            // Assert
            var unauthorizedResult = result.Result as ObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            var apiResponse = unauthorizedResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            apiResponse.ErrorMessages.Should().Contain("Unauthorized access.");
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ValidRequest_ShouldReturnSortedResults_WhenStatusIsAll_AndOrderByCreatedDate(
            string sort
        )
        {
            // Arrange
            var requestQueryParams = new Dictionary<string, string>
            {
                { "status", SD.STATUS_ALL },
                { "sort", sort },
                { "pageNumber", "1" },
            };

            // Simulate a valid user with TUTOR_ROLE
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

            // Mock the repository to return a list of student profiles sorted by CreatedDate
            var studentProfiles = new List<StudentProfile>
            {
                new StudentProfile { Id = 1, CreatedDate = new DateTime(2023, 01, 01) },
                new StudentProfile { Id = 2, CreatedDate = new DateTime(2023, 02, 01) },
                new StudentProfile { Id = 3, CreatedDate = new DateTime(2023, 03, 01) },
            };
            if (sort == SD.ORDER_ASC)
                studentProfiles = studentProfiles.OrderBy(x => x.CreatedDate).ToList();
            else
                studentProfiles = studentProfiles.OrderByDescending(x => x.CreatedDate).ToList();

            // Set up the repository mock to return the list of profiles
            _mockStudentProfileRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<StudentProfile, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((studentProfiles.Count, studentProfiles));

            // Act
            var result = await _controller.GetAllAsync(
                requestQueryParams["status"],
                requestQueryParams["sort"],
                int.Parse(requestQueryParams["pageNumber"])
            );

            // Assert
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var studentProfilesDto = apiResponse.Result as List<StudentProfileDTO>;
            studentProfilesDto.Should().NotBeNull().And.HaveCount(3);

            // Verify that the results are sorted by CreatedDate in the correct order
            if (sort == SD.ORDER_ASC)
            {
                studentProfilesDto
                    .First()
                    .CreatedDate.Should()
                    .BeBefore(studentProfilesDto.Last().CreatedDate);
            }
            else if (sort == SD.ORDER_DESC)
            {
                studentProfilesDto
                    .First()
                    .CreatedDate.Should()
                    .BeAfter(studentProfilesDto.Last().CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ValidRequest_ShouldReturnFilteredResults_WhenStatusIsTeaching(
            string sort
        )
        {
            // Arrange
            var requestQueryParams = new Dictionary<string, string>
            {
                { "status", "teaching" },
                { "sort", sort },
                { "pageNumber", "1" },
            };

            // Simulate a valid user with TUTOR_ROLE
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

            // Mock the repository to return a list of approved student profiles
            var studentProfiles = new List<StudentProfile>
            {
                new StudentProfile
                {
                    Id = 1,
                    CreatedDate = new DateTime(2023, 01, 01),
                    Status = SD.StudentProfileStatus.Teaching,
                },
                new StudentProfile
                {
                    Id = 2,
                    CreatedDate = new DateTime(2023, 02, 01),
                    Status = SD.StudentProfileStatus.Teaching,
                },
                new StudentProfile
                {
                    Id = 3,
                    CreatedDate = new DateTime(2023, 03, 01),
                    Status = SD.StudentProfileStatus.Teaching,
                },
                new StudentProfile
                {
                    Id = 4,
                    CreatedDate = new DateTime(2023, 04, 01),
                    Status = SD.StudentProfileStatus.Reject,
                },
            };

            if (sort == SD.ORDER_ASC)
                studentProfiles = studentProfiles
                    .Where(x => x.Status == SD.StudentProfileStatus.Teaching)
                    .OrderBy(x => x.CreatedDate)
                    .ToList();
            else
                studentProfiles = studentProfiles
                    .Where(x => x.Status == SD.StudentProfileStatus.Teaching)
                    .OrderByDescending(x => x.CreatedDate)
                    .ToList();

            // Set up the repository mock to return the filtered and sorted profiles
            _mockStudentProfileRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<StudentProfile, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((studentProfiles.Count, studentProfiles));

            // Act
            var result = await _controller.GetAllAsync(
                requestQueryParams["status"],
                requestQueryParams["sort"],
                int.Parse(requestQueryParams["pageNumber"])
            );

            // Assert
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var studentProfilesDto = apiResponse.Result as List<StudentProfileDTO>;
            studentProfilesDto.Should().NotBeNull();

            // Verify that only approved profiles are returned
            studentProfilesDto
                .All(x => x.Status == SD.StudentProfileStatus.Teaching)
                .Should()
                .BeTrue();
            studentProfilesDto.Count.Should().Be(3);

            // Verify that the results are sorted by CreatedDate in the correct order
            if (sort == SD.ORDER_ASC)
            {
                studentProfilesDto
                    .First()
                    .CreatedDate.Should()
                    .BeBefore(studentProfilesDto.Last().CreatedDate);
            }
            else if (sort == SD.ORDER_DESC)
            {
                studentProfilesDto
                    .First()
                    .CreatedDate.Should()
                    .BeAfter(studentProfilesDto.Last().CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ValidRequest_ShouldReturnFilteredResults_WhenStatusIsPending(
            string sort
        )
        {
            // Arrange
            var requestQueryParams = new Dictionary<string, string>
            {
                { "status", "pending" },
                { "sort", sort },
                { "pageNumber", "1" },
            };

            // Simulate a valid user with TUTOR_ROLE
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

            // Mock the repository to return a list of pending student profiles
            var studentProfiles = new List<StudentProfile>
            {
                new StudentProfile
                {
                    Id = 1,
                    CreatedDate = new DateTime(2023, 01, 01),
                    Status = SD.StudentProfileStatus.Pending,
                },
                new StudentProfile
                {
                    Id = 2,
                    CreatedDate = new DateTime(2023, 02, 01),
                    Status = SD.StudentProfileStatus.Pending,
                },
                new StudentProfile
                {
                    Id = 3,
                    CreatedDate = new DateTime(2023, 03, 01),
                    Status = SD.StudentProfileStatus.Pending,
                },
                new StudentProfile
                {
                    Id = 4,
                    CreatedDate = new DateTime(2023, 04, 01),
                    Status = SD.StudentProfileStatus.Reject,
                },
            };

            if (sort == SD.ORDER_ASC)
                studentProfiles = studentProfiles
                    .Where(x => x.Status == SD.StudentProfileStatus.Pending)
                    .OrderBy(x => x.CreatedDate)
                    .ToList();
            else
                studentProfiles = studentProfiles
                    .Where(x => x.Status == SD.StudentProfileStatus.Pending)
                    .OrderByDescending(x => x.CreatedDate)
                    .ToList();

            // Set up the repository mock to return the filtered and sorted profiles
            _mockStudentProfileRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<StudentProfile, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((studentProfiles.Count, studentProfiles));

            // Act
            var result = await _controller.GetAllAsync(
                requestQueryParams["status"],
                requestQueryParams["sort"],
                int.Parse(requestQueryParams["pageNumber"])
            );

            // Assert
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var studentProfilesDto = apiResponse.Result as List<StudentProfileDTO>;
            studentProfilesDto.Should().NotBeNull();

            // Verify that only pending profiles are returned
            studentProfilesDto
                .All(x => x.Status == SD.StudentProfileStatus.Pending)
                .Should()
                .BeTrue();
            studentProfilesDto.Count.Should().Be(3);

            // Verify that the results are sorted by CreatedDate in the correct order
            if (sort == SD.ORDER_ASC)
            {
                studentProfilesDto
                    .First()
                    .CreatedDate.Should()
                    .BeBefore(studentProfilesDto.Last().CreatedDate);
            }
            else if (sort == SD.ORDER_DESC)
            {
                studentProfilesDto
                    .First()
                    .CreatedDate.Should()
                    .BeAfter(studentProfilesDto.Last().CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ValidRequest_ShouldReturnFilteredResults_WhenStatusIsReject(
            string sort
        )
        {
            // Arrange
            var requestQueryParams = new Dictionary<string, string>
            {
                { "status", "reject" },
                { "sort", sort },
                { "pageNumber", "1" },
            };

            // Simulate a valid user with TUTOR_ROLE
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

            // Mock the repository to return a list of rejected student profiles
            var studentProfiles = new List<StudentProfile>
            {
                new StudentProfile
                {
                    Id = 1,
                    CreatedDate = new DateTime(2023, 01, 01),
                    Status = SD.StudentProfileStatus.Reject,
                },
                new StudentProfile
                {
                    Id = 2,
                    CreatedDate = new DateTime(2023, 02, 01),
                    Status = SD.StudentProfileStatus.Reject,
                },
                new StudentProfile
                {
                    Id = 3,
                    CreatedDate = new DateTime(2023, 03, 01),
                    Status = SD.StudentProfileStatus.Reject,
                },
                new StudentProfile
                {
                    Id = 4,
                    CreatedDate = new DateTime(2023, 04, 01),
                    Status = SD.StudentProfileStatus.Teaching,
                },
            };

            if (sort == SD.ORDER_ASC)
                studentProfiles = studentProfiles
                    .Where(x => x.Status == SD.StudentProfileStatus.Reject)
                    .OrderBy(x => x.CreatedDate)
                    .ToList();
            else
                studentProfiles = studentProfiles
                    .Where(x => x.Status == SD.StudentProfileStatus.Reject)
                    .OrderByDescending(x => x.CreatedDate)
                    .ToList();

            // Set up the repository mock to return the filtered and sorted profiles
            _mockStudentProfileRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<StudentProfile, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((studentProfiles.Count, studentProfiles));

            // Act
            var result = await _controller.GetAllAsync(
                requestQueryParams["status"],
                requestQueryParams["sort"],
                int.Parse(requestQueryParams["pageNumber"])
            );

            // Assert
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var studentProfilesDto = apiResponse.Result as List<StudentProfileDTO>;
            studentProfilesDto.Should().NotBeNull();

            // Verify that only rejected profiles are returned
            studentProfilesDto
                .All(x => x.Status == SD.StudentProfileStatus.Reject)
                .Should()
                .BeTrue();
            studentProfilesDto.Count.Should().Be(3);

            // Verify that the results are sorted by CreatedDate in the correct order
            if (sort == SD.ORDER_ASC)
            {
                studentProfilesDto
                    .First()
                    .CreatedDate.Should()
                    .BeBefore(studentProfilesDto.Last().CreatedDate);
            }
            else if (sort == SD.ORDER_DESC)
            {
                studentProfilesDto
                    .First()
                    .CreatedDate.Should()
                    .BeAfter(studentProfilesDto.Last().CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_ValidRequest_ShouldReturnFilteredResults_WhenStatusIsStop(
            string sort
        )
        {
            // Arrange
            var requestQueryParams = new Dictionary<string, string>
            {
                { "status", "stop" },
                { "sort", sort },
                { "pageNumber", "1" },
            };

            // Simulate a valid user with TUTOR_ROLE
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

            // Mock the repository to return a list of student profiles with Stop status
            var studentProfiles = new List<StudentProfile>
            {
                new StudentProfile
                {
                    Id = 1,
                    CreatedDate = new DateTime(2023, 01, 01),
                    Status = SD.StudentProfileStatus.Stop,
                },
                new StudentProfile
                {
                    Id = 2,
                    CreatedDate = new DateTime(2023, 02, 01),
                    Status = SD.StudentProfileStatus.Stop,
                },
                new StudentProfile
                {
                    Id = 3,
                    CreatedDate = new DateTime(2023, 03, 01),
                    Status = SD.StudentProfileStatus.Stop,
                },
                new StudentProfile
                {
                    Id = 4,
                    CreatedDate = new DateTime(2023, 04, 01),
                    Status = SD.StudentProfileStatus.Teaching,
                },
            };

            if (sort == SD.ORDER_ASC)
                studentProfiles = studentProfiles
                    .Where(x => x.Status == SD.StudentProfileStatus.Stop)
                    .OrderBy(x => x.CreatedDate)
                    .ToList();
            else
                studentProfiles = studentProfiles
                    .Where(x => x.Status == SD.StudentProfileStatus.Stop)
                    .OrderByDescending(x => x.CreatedDate)
                    .ToList();

            // Set up the repository mock to return the filtered and sorted profiles
            _mockStudentProfileRepository
                .Setup(repo =>
                    repo.GetAllWithIncludeAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<StudentProfile, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((studentProfiles.Count, studentProfiles));

            // Act
            var result = await _controller.GetAllAsync(
                requestQueryParams["status"],
                requestQueryParams["sort"],
                int.Parse(requestQueryParams["pageNumber"])
            );

            // Assert
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var studentProfilesDto = apiResponse.Result as List<StudentProfileDTO>;
            studentProfilesDto.Should().NotBeNull();

            // Verify that only stopped profiles are returned
            studentProfilesDto
                .All(x => x.Status == SD.StudentProfileStatus.Stop)
                .Should()
                .BeTrue();
            studentProfilesDto.Count.Should().Be(3);

            // Verify that the results are sorted by CreatedDate in the correct order
            if (sort == SD.ORDER_ASC)
            {
                studentProfilesDto
                    .First()
                    .CreatedDate.Should()
                    .BeBefore(studentProfilesDto.Last().CreatedDate);
            }
            else if (sort == SD.ORDER_DESC)
            {
                studentProfilesDto
                    .First()
                    .CreatedDate.Should()
                    .BeAfter(studentProfilesDto.Last().CreatedDate);
            }
        }

        [Fact]
        public async Task GetAllChildStudentProfile_StatusAll_ShouldReturnAllProfiles()
        {
            // Arrange
            var parentId = "valid-parent-id";
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, parentId) };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var children = new List<ChildInformation>
            {
                new ChildInformation { Id = 1, ParentId = parentId },
                new ChildInformation { Id = 2, ParentId = parentId },
            };

            var studentProfiles = new List<StudentProfile>
            {
                new StudentProfile
                {
                    Id = 1,
                    ChildId = 1,
                    Status = SD.StudentProfileStatus.Pending,
                },
                new StudentProfile
                {
                    Id = 2,
                    ChildId = 1,
                    Status = SD.StudentProfileStatus.Teaching,
                },
            };
            _mockChildInfoRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<ChildInformation, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((children.Count, children));

            _mockStudentProfileRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(studentProfiles.First());

            // Act
            var result = await _controller.GetAllChildStudentProfile(SD.STATUS_ALL);

            // Assert
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();

            var profiles = apiResponse.Result as List<ChildStudentProfileDTO>;
            profiles.Should().NotBeNull();
            profiles.Count.Should().Be(studentProfiles.Count); // Ensure all profiles are returned
        }

        [Fact]
        public async Task GetAllChildStudentProfile_StatusPending_ShouldReturnPendingProfiles()
        {
            // Arrange
            var parentId = "valid-parent-id";
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, parentId) };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var children = new List<ChildInformation>
            {
                new ChildInformation { Id = 1, ParentId = parentId },
                new ChildInformation { Id = 2, ParentId = parentId },
            };

            var studentProfiles = new List<StudentProfile>
            {
                new StudentProfile
                {
                    Id = 1,
                    ChildId = 1,
                    Status = SD.StudentProfileStatus.Pending,
                },
                new StudentProfile
                {
                    Id = 2,
                    ChildId = 1,
                    Status = SD.StudentProfileStatus.Pending,
                },
            };

            _mockChildInfoRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<ChildInformation, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((children.Count, children));

            _mockStudentProfileRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(studentProfiles.First());

            // Act
            var result = await _controller.GetAllChildStudentProfile("pending");

            // Assert
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();

            var profiles = apiResponse.Result as List<ChildStudentProfileDTO>;
            profiles.Should().NotBeNull();
            profiles.Should().OnlyContain(p => p.Status == SD.StudentProfileStatus.Pending); // Ensure only pending profiles are returned
            profiles.Count.Should().Be(2); // Two profiles with 'Pending' status
        }

        [Fact]
        public async Task GetAllChildStudentProfile_StatusReject_ShouldReturnRejectedProfiles()
        {
            // Arrange
            var parentId = "valid-parent-id";
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, parentId) };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var children = new List<ChildInformation>
            {
                new ChildInformation { Id = 1, ParentId = parentId },
                new ChildInformation { Id = 2, ParentId = parentId },
            };

            var studentProfiles = new List<StudentProfile>
            {
                new StudentProfile
                {
                    Id = 1,
                    ChildId = 1,
                    Status = SD.StudentProfileStatus.Reject,
                },
                new StudentProfile
                {
                    Id = 2,
                    ChildId = 1,
                    Status = SD.StudentProfileStatus.Teaching,
                },
                new StudentProfile
                {
                    Id = 3,
                    ChildId = 2,
                    Status = SD.StudentProfileStatus.Reject,
                },
            };

            _mockChildInfoRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<ChildInformation, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((children.Count, children));

            _mockStudentProfileRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(studentProfiles.First());

            // Act
            var result = await _controller.GetAllChildStudentProfile("reject");

            // Assert
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();

            var profiles = apiResponse.Result as List<ChildStudentProfileDTO>;
            profiles.Should().NotBeNull();
            profiles.Should().OnlyContain(p => p.Status == SD.StudentProfileStatus.Reject); // Ensure only rejected profiles are returned
            profiles.Count.Should().Be(2); // Two profiles with 'Reject' status
        }

        [Fact]
        public async Task GetAllChildStudentProfile_StatusTeaching_ShouldReturnTeachingProfiles()
        {
            // Arrange
            var parentId = "valid-parent-id";
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, parentId) };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var children = new List<ChildInformation>
            {
                new ChildInformation { Id = 1, ParentId = parentId },
                new ChildInformation { Id = 2, ParentId = parentId },
            };

            var studentProfiles = new List<StudentProfile>
            {
                new StudentProfile
                {
                    Id = 1,
                    ChildId = 1,
                    Status = SD.StudentProfileStatus.Teaching,
                },
                new StudentProfile
                {
                    Id = 2,
                    ChildId = 1,
                    Status = SD.StudentProfileStatus.Pending,
                },
                new StudentProfile
                {
                    Id = 3,
                    ChildId = 2,
                    Status = SD.StudentProfileStatus.Teaching,
                },
            };

            _mockChildInfoRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<ChildInformation, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((children.Count, children));

            _mockStudentProfileRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(studentProfiles.First());

            // Act
            var result = await _controller.GetAllChildStudentProfile("teaching");

            // Assert
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();

            var profiles = apiResponse.Result as List<ChildStudentProfileDTO>;
            profiles.Should().NotBeNull();
            profiles.Should().OnlyContain(p => p.Status == SD.StudentProfileStatus.Teaching); // Ensure only teaching profiles are returned
            profiles.Count.Should().Be(2); // Two profiles with 'Teaching' status
        }

        [Fact]
        public async Task GetAllChildStudentProfile_StatusStop_ShouldReturnStopProfiles()
        {
            // Arrange
            var parentId = "valid-parent-id";
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, parentId) };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var children = new List<ChildInformation>
            {
                new ChildInformation { Id = 1, ParentId = parentId },
                new ChildInformation { Id = 2, ParentId = parentId },
            };

            var studentProfiles = new List<StudentProfile>
            {
                new StudentProfile
                {
                    Id = 1,
                    ChildId = 1,
                    Status = SD.StudentProfileStatus.Stop,
                },
                new StudentProfile
                {
                    Id = 2,
                    ChildId = 1,
                    Status = SD.StudentProfileStatus.Pending,
                },
                new StudentProfile
                {
                    Id = 3,
                    ChildId = 2,
                    Status = SD.StudentProfileStatus.Stop,
                },
            };

            _mockChildInfoRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<ChildInformation, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((children.Count, children));

            _mockStudentProfileRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(studentProfiles.First(x => x.Status == SD.StudentProfileStatus.Stop));

            // Act
            var result = await _controller.GetAllChildStudentProfile("stop");

            // Assert
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();

            var profiles = apiResponse.Result as List<ChildStudentProfileDTO>;
            profiles.Should().NotBeNull();
            profiles.Should().OnlyContain(p => p.Status == SD.StudentProfileStatus.Stop); // Ensure only stop profiles are returned
            profiles.Count.Should().Be(2); // Two profiles with 'Stop' status
        }

        [Fact]
        public async Task GetAllChildStudentProfile_AuthenticationInvalid_ShouldReturnUnauthorized()
        {
            // Arrange
            var invalidClaims = new List<Claim>(); // No valid claims for authentication
            var identity = new ClaimsIdentity(invalidClaims);
            var principal = new ClaimsPrincipal(identity);
            _mockResourceService
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }, // Setting an unauthenticated principal
            };

            // Act
            var result = await _controller.GetAllChildStudentProfile("pending");

            // Assert
            var unauthorizedResult = result.Result as ObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized); // Ensure unauthorized response
            var apiResponse = unauthorizedResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            apiResponse.ErrorMessages.First().Should().Be("Unauthorized access.");
        }

        [Fact]
        public async Task GetAllChildStudentProfile_InternalServerError_ShouldReturnInternalServerError()
        {
            // Arrange
            var parentId = "valid-parent-id";
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, parentId) };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Simulate an exception or error when accessing the repository
            _mockChildInfoRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<ChildInformation, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ThrowsAsync(new Exception("An unexpected error occurred"));

            // Act
            var result = await _controller.GetAllChildStudentProfile("pending");

            // Assert
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
        public async Task GetAllScheduleTimeSlot_Unauthorized_ShouldReturnUnauthorized()
        {
            // Arrange
            var claims = new List<Claim>(); // Empty claims to simulate an unauthenticated user
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
            var result = await _controller.GetAllScheduleTimeSlot();

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
        public async Task GetAllScheduleTimeSlot_InternalServerError_ShouldReturnInternalServerError()
        {
            // Arrange
            var tutorId = "valid-tutor-id"; // Use a valid tutor ID for the simulation
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, tutorId) };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            // Simulate an exception being thrown in the repository
            _mockStudentProfileRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<StudentProfile, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ThrowsAsync(new Exception("An unexpected error occurred"));
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");
            // Act
            var result = await _controller.GetAllScheduleTimeSlot();

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
        public async Task GetAllScheduleTimeSlot_Valid_ShouldReturnScheduleTimeSlots()
        {
            // Arrange
            var tutorId = "valid-tutor-id"; // Use a valid tutor ID for the simulation
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, tutorId) };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            var scheduleTimeSlots = new List<StudentProfile>
            {
                new StudentProfile
                {
                    Id = 1,
                    TutorId = tutorId,
                    Status = SD.StudentProfileStatus.Teaching,
                    ScheduleTimeSlots = new List<ScheduleTimeSlot>
                    {
                        new ScheduleTimeSlot
                        {
                            Id = 1,
                            From = TimeSpan.Parse("9:00"),
                            To = TimeSpan.Parse("10:00"),
                        },
                    },
                },
                new StudentProfile
                {
                    Id = 2,
                    TutorId = tutorId,
                    Status = SD.StudentProfileStatus.Pending,
                    ScheduleTimeSlots = new List<ScheduleTimeSlot>
                    {
                        new ScheduleTimeSlot
                        {
                            Id = 2,
                            From = TimeSpan.Parse("9:00"),
                            To = TimeSpan.Parse("10:00"),
                        },
                    },
                },
            };

            // Mock the repository to return the schedule time slots
            _mockStudentProfileRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<StudentProfile, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<StudentProfile, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((scheduleTimeSlots.Count, scheduleTimeSlots));

            // Act
            var result = await _controller.GetAllScheduleTimeSlot();

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK); // Ensure OK (200) status code is returned

            var apiResponse = objectResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue(); // Ensure success response
            apiResponse.Result.Should().NotBeNull(); // Ensure result is not null

            var timeSlots = apiResponse.Result as List<GetAllStudentProfileTimeSlotDTO>;
            timeSlots.Should().NotBeNull();
            timeSlots.Count.Should().Be(2); // Ensure two schedule time slots are returned
        }
    }
}
