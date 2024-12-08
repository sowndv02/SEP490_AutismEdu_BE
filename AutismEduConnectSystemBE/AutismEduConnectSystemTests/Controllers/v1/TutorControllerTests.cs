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
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.SignalR;
using AutismEduConnectSystem.Utils;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
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
        private readonly Mock<IEmailSender> _mockMessageBus;
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
            _mockMessageBus = new Mock<IEmailSender>();
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
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenValidFiltersAreProvided_WithNoSearchAndSearchAddressNewYork_WithReviewScoreZero()
        {
            // Arrange
            int ageFrom = 3;
            int ageTo = 5;
            var search = string.Empty; // No search term
            var searchAddress = "New York"; // Search by address
            int reviewScore = 0; // Review score set to 0
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 3,
                    EndAge = 5,
                    ReviewScore = 5, // Review score matches the filter (0)
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "Sample Tutor",
                        Address =
                            "New York" // Matches the search address
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 3,
                    EndAge = 5,
                    ReviewScore = 5, // Review score matches the filter (0)
                    FullName = "Sample Tutor",
                    Address = "New York", // Matches the search address
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        reviewScore,
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(), // Searching by address
                        9,
                        pageNumber,
                        It.IsAny<Expression<Func<Tutor, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        reviewScore, // Review score filter (0)
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(), // Search by address
                        9,
                        pageNumber,
                        null,
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenValidFiltersAreProvided_WithNoSearchAndSearchAddressNewYork()
        {
            // Arrange
            int ageFrom = 3;
            int ageTo = 5;
            var search = string.Empty; // No search term
            var searchAddress = "New York"; // Search by address
            int reviewScore = 4; // Review score set to 4
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 3,
                    EndAge = 5,
                    ReviewScore = 4,
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "Sample Tutor",
                        Address =
                            "New York" // Matches the search address
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 3,
                    EndAge = 5,
                    ReviewScore = 4,
                    FullName = "Sample Tutor",
                    Address = "New York", // Matches the search address
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        reviewScore,
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(), // Searching by address
                        9,
                        pageNumber,
                        It.IsAny<Expression<Func<Tutor, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        reviewScore,
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(), // Search by address
                        9,
                        pageNumber,
                        null,
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenValidFiltersAreProvided_WithNoSearchAndReviewScore4()
        {
            // Arrange
            int ageFrom = 3;
            int ageTo = 5;
            var search = string.Empty; // No search term
            var searchAddress = string.Empty; // No search address
            int reviewScore = 4; // Review score set to 4
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 3,
                    EndAge = 5,
                    ReviewScore = 4,
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "Sample Tutor",
                        Address = "Some Address",
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 3,
                    EndAge = 5,
                    ReviewScore = 4,
                    FullName = "Sample Tutor",
                    Address = "Some Address",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        reviewScore,
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(), // Empty search term
                        9,
                        pageNumber,
                        It.IsAny<Expression<Func<Tutor, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        reviewScore,
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(), // Empty search term
                        9,
                        pageNumber,
                        null,
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenValidFiltersAreProvided_WithNoSearchAndReviewScoreZero()
        {
            // Arrange
            int ageFrom = 3;
            int ageTo = 5;
            var search = string.Empty; // No search term
            var searchAddress = string.Empty; // No search address
            int reviewScore = 0; // Review score set to 0
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 3,
                    EndAge = 5,
                    ReviewScore = 5,
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "Sample Tutor",
                        Address = "Some Address",
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 3,
                    EndAge = 5,
                    ReviewScore = 5,
                    FullName = "Sample Tutor",
                    Address = "Some Address",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        reviewScore,
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(), // Empty search term
                        9,
                        pageNumber,
                        It.IsAny<Expression<Func<Tutor, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        reviewScore,
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(), // Empty search term
                        9,
                        pageNumber,
                        null,
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenValidFiltersAreProvided_WithAgeAndReviewScoreZero()
        {
            // Arrange
            int ageFrom = 3;
            int ageTo = 5;
            var search = "Search";
            var searchAddress = string.Empty; // No address provided
            int reviewScore = 0; // Review score set to 0
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 3,
                    EndAge = 5,
                    ReviewScore = 5,
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "Search Tutor",
                        Address = "Some Address",
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 3,
                    EndAge = 5,
                    ReviewScore = 5,
                    FullName = "Search Tutor",
                    Address = "Some Address",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        reviewScore,
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(), // Search term "Search"
                        9,
                        pageNumber,
                        It.IsAny<Expression<Func<Tutor, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        reviewScore,
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(), // Search term "Search"
                        9,
                        pageNumber,
                        null,
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenValidFiltersAreProvided_WithAgeRangeReviewScore4_NoAddress()
        {
            // Arrange
            int ageFrom = 3; // Age range from 3
            int ageTo = 5; // Age range to 5
            var search = "John"; // Name search filter
            string searchAddress = null; // No address search filter
            int reviewScore = 4; // Review score filter
            int pageNumber = 1; // First page of results

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 3, // Tutor's start age is within the filter range
                    EndAge = 5, // Tutor's end age is within the filter range
                    ReviewScore = 4, // Review score matches the filter
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe", // Full name matches the search
                        Address =
                            "New York" // Address is irrelevant in this case since it's not being filtered
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 3,
                    EndAge = 5,
                    ReviewScore = 4,
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            // Set up the repository mock to return a tutor matching the filters
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age range check
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Review score filter
                        reviewScore, // Review score filter value
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Search for "John"
                        It.IsAny<string>(), // No address filter applied
                        9, // Page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // No explicit ordering
                        true
                    )
                ) // Whether to include pagination
                .ReturnsAsync((mockTutors.Count, mockTutors)); // Return mock tutors list

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK); // Status code should be 200 OK

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs); // Check that result matches mock DTOs
            response.Pagination.PageNumber.Should().Be(pageNumber); // Page number should be 1
            response.Pagination.PageSize.Should().Be(9); // Page size should be 9
            response.Pagination.Total.Should().Be(mockTutors.Count); // Total should be 1
            response.StatusCode.Should().Be(HttpStatusCode.OK); // Status code should be OK

            // Verify that the repository was called with the expected filters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age range check
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Review score filter
                        reviewScore, // Review score filter value
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Search for "John"
                        It.IsAny<string>(), // No address filter applied
                        9, // Page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    ),
                Times.Once
            ); // Verify repository call was made once with these parameters
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenValidFiltersAreProvided_WithAgeRangeReviewScore4()
        {
            // Arrange
            int ageFrom = 3; // Age range from 3
            int ageTo = 5; // Age range to 5
            var search = "John"; // Name search filter
            var searchAddress = "New York"; // Address search filter
            int reviewScore = 4; // Review score filter
            int pageNumber = 1; // First page of results

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 3, // Tutor's start age is within the filter range
                    EndAge = 5, // Tutor's end age is within the filter range
                    ReviewScore = 4, // Review score matches the filter
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe", // Full name matches the search
                        Address =
                            "New York" // Address matches the search
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 3,
                    EndAge = 5,
                    ReviewScore = 4,
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            // Set up the repository mock to return a tutor matching the filters
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age range filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age range filter
                        reviewScore, // Review score filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Search for name "John"
                        It.IsAny<string>(), // Search for address "New York"
                        9, // Page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // Sorting expression
                        It.IsAny<bool>()
                    )
                ) // Whether to include pagination
                .ReturnsAsync((mockTutors.Count, mockTutors)); // Return mock tutors list

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK); // Status code should be 200 OK

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs); // Check that result matches mock DTOs
            response.Pagination.PageNumber.Should().Be(pageNumber); // Page number should be 1
            response.Pagination.PageSize.Should().Be(9); // Page size should be 9
            response.Pagination.Total.Should().Be(mockTutors.Count); // Total should be 1
            response.StatusCode.Should().Be(HttpStatusCode.OK); // Status code should be OK

            // Verify that the repository was called with the expected filters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age range check
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Review score filter
                        It.Is<int>(score => score == reviewScore), // Review score filter value
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Search for "John"
                        It.IsAny<string>(), // Address filter check
                        9, // Page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    ),
                Times.Once
            ); // Verify repository call was made once with these parameters
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenValidFiltersAreProvided_WithAgeRangeReviewScore0()
        {
            // Arrange
            int ageFrom = 3; // Valid age range start
            int ageTo = 5; // Valid age range end
            var search = "John"; // Name to search for
            var searchAddress = "New York"; // Address to filter by
            int reviewScore = 0; // Review score filter set to 0
            int pageNumber = 1; // First page of results

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 3, // Within the age range
                    EndAge = 5, // Within the age range
                    ReviewScore = 5, // Review score matches filter
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe", // Name matches search term
                        Address =
                            "New York" // Address matches search address
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 3,
                    EndAge = 5,
                    ReviewScore = 5,
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            // Set up the repository to return the mock tutor based on the filters
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter
                        reviewScore, // Review score filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Search for name "John"
                        It.IsAny<string>(), // Search address filter "New York"
                        9, // Page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // No explicit sorting
                        true
                    )
                ) // Apply pagination
                .ReturnsAsync((mockTutors.Count, mockTutors)); // Return mock tutors

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs); // Should match mock DTOs
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count); // Total should match mock tutor count (1)
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify that the repository was called with the correct filters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age range filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Review score filter
                        It.IsAny<int>(), // Check that review score filter was applied
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Search filter for name
                        It.IsAny<string>(), // Address filter check
                        9, // Page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    ),
                Times.Once
            ); // Verify that the method was called once
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyList_WhenAgeFromIsGreaterThanAgeToAndReviewScoreIsZero()
        {
            // Arrange
            int ageFrom = 8; // Lower bound of age
            int ageTo = 5; // Upper bound of age, invalid range (8 > 5)
            var search = "Search"; // Search term for tutor's name
            string searchAddress = null; // No address search filter
            int reviewScore = 0; // Review score filter, looking for tutors with a score of 0
            int pageNumber = 1; // First page of results

            var mockTutors = new List<Tutor>(); // No tutors match the criteria

            var mockTutorDTOs = new List<TutorDTO>(); // Should be an empty list

            // Set up the repository to return no tutors for the invalid age range and other filters
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter
                        reviewScore, // Review score filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Search filter for name
                        It.IsAny<string>(), // Search filter for address
                        9, // Page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    )
                ) // Apply pagination
                .ReturnsAsync((mockTutors.Count, mockTutors)); // Return empty list of tutors

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs); // Should be empty
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count); // Total should be 0
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify repository interaction to check if the method was called with the correct parameters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(), // No address filter (searchAddress)
                        9, // Page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    ),
                Times.Once
            ); // Verify that it was called once
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyList_WhenAgeFromIsGreaterThanAgeToAndReviewScoreIsProvided()
        {
            // Arrange
            int ageFrom = 8; // Lower bound of age
            int ageTo = 5; // Upper bound of age, invalid range (8 > 5)
            var search = "Search"; // Search term for tutor's name
            string searchAddress = null; // No address search filter
            int reviewScore = 4; // Review score filter, looking for tutors with a score of 4
            int pageNumber = 1; // First page of results

            var mockTutors = new List<Tutor>(); // No tutors match the criteria

            var mockTutorDTOs = new List<TutorDTO>(); // Should be an empty list

            // Set up the repository to return no tutors for the invalid age range and other filters
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter
                        reviewScore, // Review score filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Search filter for name
                        It.IsAny<string>(), // Search filter for address
                        9, // Page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    )
                ) // Apply pagination
                .ReturnsAsync((mockTutors.Count, mockTutors)); // Return empty list of tutors

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs); // Should be empty
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count); // Total should be 0
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify repository interaction to check if the method was called with the correct parameters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter
                        It.IsAny<int>(), // Ensure review score is 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No name filter (search)
                        It.IsAny<string>(), // No address filter (searchAddress)
                        9, // Page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    ),
                Times.Once
            ); // Verify that it was called once
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyList_WhenAgeFromIsGreaterThanAgeTo()
        {
            // Arrange
            int ageFrom = 8; // Lower age bound
            int ageTo = 5; // Upper age bound (smaller than ageFrom, which makes it invalid)
            var search = "John"; // Search term for the tutor's name
            var searchAddress = "New York"; // Search term for the tutor's address
            int reviewScore = 4; // Filter by review score of 4
            int pageNumber = 1; // First page

            var mockTutors = new List<Tutor>(); // No tutors matching the criteria

            var mockTutorDTOs = new List<TutorDTO>(); // No DTOs should be returned

            // Set up the repository to return no tutors for the invalid age range
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter
                        reviewScore, // Review score filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Search filter for name
                        It.IsAny<string>(), // Search filter for address
                        9, // Page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    )
                ) // Apply pagination
                .ReturnsAsync((mockTutors.Count, mockTutors)); // Return empty list of tutors

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs); // Should be empty
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count); // Total should be 0
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify repository interaction to check if the method was called with the correct parameters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter
                        It.IsAny<int>(), // Ensure review score is 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No name filter (search)
                        It.IsAny<string>(), // No address filter (searchAddress)
                        9, // Page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenReviewScoreIsProvidedAndNoFiltersAreApplied()
        {
            // Arrange
            int ageFrom = -1; // AgeFrom is -1 (no lower bound)
            int? ageTo = null; // AgeTo is null (no upper bound)
            string search = null; // No search term
            string searchAddress = null; // No address provided
            int reviewScore = 4; // Review score of 4
            int pageNumber = 1; // First page

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Review score of 4
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe",
                        Address = "New York",
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Review score of 4
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter
                        reviewScore, // Review score of 4 (filtering by review score)
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name search filter (null)
                        It.IsAny<string>(), // No address search filter (null)
                        9, // Default page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    )
                ) // Apply pagination
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify repository interaction
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter (-1)
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter (null)
                        It.IsAny<int>(), // Ensure review score is 4 (filtering by review score)
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Ensure no name search filter (null)
                        It.IsAny<string>(), // Ensure no address search filter (null)
                        9, // Default page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenSearchAddressIsProvidedAndReviewScoreIsZero()
        {
            // Arrange
            int ageFrom = -1; // AgeFrom is -1 (no lower bound)
            int? ageTo = null; // AgeTo is null (no upper bound)
            string search = null; // No search term
            string searchAddress = "New York"; // Search by address
            int reviewScore = 0; // Review score of 0 (no filtering based on review score)
            int pageNumber = 1; // First page

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Review score of 4
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe",
                        Address =
                            "New York" // Address matches search address
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Review score of 4
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter
                        reviewScore, // Review score of 0 (no filtering)
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name search filter (null)
                        It.IsAny<string>(), // Search by address ("New York")
                        9, // Default page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    )
                ) // Apply pagination
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify repository interaction
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter (-1)
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter (null)
                        It.IsAny<int>(), // Ensure review score is 0 (no filtering)
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Ensure no name search filter (null)
                        It.IsAny<string>(), // Ensure address search filter is "New York"
                        9, // Default page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenSearchAddressIsProvidedAndNoSearchWithReviewScoreFour()
        {
            // Arrange
            int ageFrom = -1; // AgeFrom is -1 (no lower bound)
            int? ageTo = null; // AgeTo is null (no upper bound)
            string search = null; // No search term
            string searchAddress = "New York"; // Search by address
            int reviewScore = 4; // Review score of 4
            int pageNumber = 1; // First page

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Review score of 4
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe",
                        Address =
                            "New York" // Address matches search address
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Review score of 4
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter
                        reviewScore, // Review score of 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name search filter (null)
                        It.IsAny<string>(), // Search by address ("New York")
                        9, // Default page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    )
                ) // Apply pagination
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify repository interaction
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter (-1)
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter (null)
                        It.IsAny<int>(), // Ensure review score is 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Ensure no name search filter (null)
                        It.IsAny<string>(), // Ensure address search filter is "New York"
                        9, // Default page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenSearchIsProvidedAndNoSearchAddressWithReviewScoreFour()
        {
            // Arrange
            int ageFrom = -1; // AgeFrom is -1 (no lower bound)
            int? ageTo = null; // AgeTo is null (no upper bound)
            string search = "John"; // Search term
            string searchAddress = null; // No search address
            int reviewScore = 4; // Review score of 4
            int pageNumber = 1; // First page

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Review score of 4
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe",
                        Address =
                            "New York" // Address won't be filtered since searchAddress is null
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Review score of 4
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter
                        reviewScore, // Review score of 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name search filter ("John")
                        It.IsAny<string>(), // No address filter
                        9, // Default page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    )
                ) // Apply pagination
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify repository interaction
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter (-1)
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter (null)
                        It.IsAny<int>(), // Ensure review score is 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Check name search filter ("John")
                        It.IsAny<string>(), // Ensure address filter is null
                        9, // Default page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenSearchIsProvidedAndNoSearchAddressWithReviewScoreZero()
        {
            // Arrange
            int ageFrom = -1; // AgeFrom is -1 (no lower bound)
            int? ageTo = null; // AgeTo is null (no upper bound)
            string search = "John"; // Search term
            string searchAddress = null; // No search address
            int reviewScore = 0; // Review score of 0
            int pageNumber = 1; // First page

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5, // Review score of 0
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe",
                        Address =
                            "New York" // Address won't be filtered since searchAddress is null
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5, // Review score of 0
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter
                        reviewScore, // Review score of 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name search filter ("John")
                        It.IsAny<string>(), // No address filter
                        9, // Default page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    )
                ) // Apply pagination
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify repository interaction
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter (-1)
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter (null)
                        It.IsAny<int>(), // Ensure review score is 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Check name search filter ("John")
                        It.IsAny<string>(), // Ensure address filter is null
                        9, // Default page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenSearchAndSearchAddressAreProvidedWithReviewScoreZero()
        {
            // Arrange
            int ageFrom = -1; // AgeFrom is -1 (no lower bound)
            int? ageTo = null; // AgeTo is null (no upper bound)
            string search = "John"; // Search term
            string searchAddress = "New York"; // Search address
            int reviewScore = 0; // Review score of 0
            int pageNumber = 1; // First page

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5, // Review score of 0
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe",
                        Address = "New York",
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5, // Review score of 0
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter
                        reviewScore, // Review score of 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name search filter ("John")
                        It.IsAny<string>(), // Address search filter ("New York")
                        9, // Default page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    )
                ) // Apply pagination
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify repository interaction
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter (-1)
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter (null)
                        It.IsAny<int>(), // Ensure review score is 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Check name search filter ("John")
                        It.IsAny<string>(), // Ensure address filter is "New York"
                        9, // Default page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenSearchAndSearchAddressAreProvidedWithReviewScoreFour()
        {
            // Arrange
            int ageFrom = -1; // AgeFrom is -1 (no lower bound)
            int? ageTo = null; // AgeTo is null (no upper bound)
            string search = "John"; // Search term
            string searchAddress = "New York"; // Search address
            int reviewScore = 4; // Review score of 4
            int pageNumber = 1; // First page

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Review score of 4
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe",
                        Address = "New York",
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Review score of 4
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter
                        reviewScore, // Review score of 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name search filter ("John")
                        It.IsAny<string>(), // Address search filter ("New York")
                        9, // Default page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    )
                ) // Apply pagination
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify repository interaction
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter (-1)
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter (null)
                        It.IsAny<int>(), // Ensure review score is 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Check name search filter ("John")
                        It.IsAny<string>(), // Ensure address filter is "New York"
                        9, // Default page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenNoSearchAndNoSearchAddressAreProvidedWithReviewScoreFour()
        {
            // Arrange
            int? ageFrom = null; // AgeFrom is null
            int ageTo = 0; // AgeTo is 0
            string search = null; // No search term
            string searchAddress = null; // No address provided
            int reviewScore = 4; // Review score is 4
            int pageNumber = 1; // First page

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Review score is 4
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe",
                        Address = "New York",
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Review score is 4
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter
                        reviewScore, // Review score 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name search filter (null in this case)
                        It.IsAny<string>(), // No address filter (null)
                        9, // Default page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // No explicit ordering
                        true
                    )
                ) // Apply pagination
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify repository interaction
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter
                        It.IsAny<int>(), // Ensure review score is 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name search filter (null)
                        It.IsAny<string>(), // No address filter
                        9, // Default page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenNoSearchAndNoSearchAddressAreProvidedWithReviewScoreZero()
        {
            // Arrange
            int? ageFrom = null; // AgeFrom is null
            int ageTo = 0; // AgeTo is 0
            string search = null; // No search term
            string searchAddress = null; // No address provided
            int reviewScore = 0; // Review score is 0
            int pageNumber = 1; // First page

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5, // Review score is 0
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe",
                        Address = "New York",
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5, // Review score is 0
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter
                        reviewScore, // Review score 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name search filter (null in this case)
                        It.IsAny<string>(), // No address filter (null)
                        9, // Default page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    )
                ) // Apply pagination
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify repository interaction
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter
                        It.IsAny<int>(), // Ensure review score is 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name search filter (null)
                        It.IsAny<string>(), // No address filter
                        9, // Default page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenAgeFromIsNullAndSearchAddressIsProvidedWithReviewScoreZero()
        {
            // Arrange
            int? ageFrom = null; // AgeFrom is null
            int ageTo = 0; // AgeTo is 0
            string search = null; // No search term
            var searchAddress = "New York"; // Search address provided
            int reviewScore = 0; // Review score is 0
            int pageNumber = 1; // First page

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5, // Review score is 0
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe",
                        Address = "New York",
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5, // Review score is 0
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        reviewScore, // Review score 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(), // Ensure the address is "New York"
                        9, // Default page size
                        pageNumber,
                        It.IsAny<Expression<Func<Tutor, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify repository interaction
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(),
                        9, // Default page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenAgeFromIsNullAndSearchAddressIsProvidedWithReviewScoreFour()
        {
            // Arrange
            int? ageFrom = null; // AgeFrom is null
            int ageTo = 0; // AgeTo is 0
            string search = null; // No search term
            var searchAddress = "New York"; // Search address provided
            int reviewScore = 4; // Review score is 4
            int pageNumber = 1; // First page

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4,
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe",
                        Address = "New York",
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4,
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        reviewScore,
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(), // Ensure the address is "New York"
                        9, // Default page size
                        pageNumber,
                        It.IsAny<Expression<Func<Tutor, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify repository interaction
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(),
                        9, // Default page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenAgeFromIsNullAndNoSearchAddressWithReviewScoreFour()
        {
            // Arrange
            int? ageFrom = null; // AgeFrom is null
            int ageTo = 0; // AgeTo is 0
            var search = "John"; // Search term provided
            string searchAddress = null; // No search address
            int reviewScore = 4; // ReviewScore is 4
            int pageNumber = 1; // First page

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4,
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe",
                        Address = "New York",
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4,
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        reviewScore,
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(), // Ensure address is null
                        9, // Default page size
                        pageNumber,
                        It.IsAny<Expression<Func<Tutor, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify repository interaction
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeFrom filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // AgeTo filter
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(),
                        9, // Default page size
                        pageNumber, // Page number
                        null, // No explicit ordering
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenAgeFromIsNullAndNoSearchAddressAndReviewScoreIsZero()
        {
            // Arrange
            int? ageFrom = null; // No lower age bound
            int ageTo = 0; // Upper age bound is 0
            string search = "John"; // Search term for tutor's name
            string searchAddress = null; // No search address
            int reviewScore = 0; // Review score is 0
            int pageNumber = 1; // First page

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5,
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe", // Matches the search term
                        Address = "New York",
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5,
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        reviewScore,
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(),
                        9, // Page size
                        pageNumber,
                        It.IsAny<Expression<Func<Tutor, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify repository was called with correct parameters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Address filter (null)
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(),
                        9, // Page size
                        pageNumber, // Page number
                        null, // Order by
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenAgeFromIsNullAndReviewScoreIsZero()
        {
            // Arrange
            int? ageFrom = null; // No lower age bound filter
            int ageTo = 0; // Upper age bound is 0
            string search = "John"; // Search term for tutor's name
            string searchAddress = "New York"; // Search term for address
            int reviewScore = 0; // Review score is zero
            int pageNumber = 1; // Page number for pagination

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5, // Outside the age filter (ageTo = 0)
                    EndAge = 10, // Outside the age filter (ageTo = 0)
                    ReviewScore = 5, // Matches the review score
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe", // Matches the search term
                        Address =
                            "New York" // Matches the search address
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5,
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        reviewScore,
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(),
                        9, // Page size
                        pageNumber,
                        It.IsAny<Expression<Func<Tutor, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify repository was called with the correct parameters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age filter with ageFrom = null
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Address filter
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(),
                        9, // Page size
                        pageNumber, // Page number
                        null, // Order by
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenAgeFromIsNullAndFiltersAreProvided()
        {
            // Arrange
            int? ageFrom = null; // No AgeFrom filter
            int ageTo = 0; // AgeTo filter set to 0
            string search = "John"; // Search term for tutor's name
            string searchAddress = "New York"; // Search term for address
            int reviewScore = 4; // Review score filter set to 4
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5, // Not filtered by ageFrom since it's null
                    EndAge = 10, // Not filtered by ageTo as the logic might not match `ageTo = 0`
                    ReviewScore = 4, // Review score matches the filter
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe", // FullName matches the search term
                        Address =
                            "New York" // Address matches the search address
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4,
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            // Mock repository to return filtered data
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Filters for age
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Filters for address
                        reviewScore, // Review score filter matches 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Filters for name
                        It.IsAny<string>(), // Address filter for "New York"
                        9, // Page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // Order by
                        It.IsAny<bool>()
                    )
                ) // Order descending
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify repository was called with the correct parameters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Filter for ageFrom = null and ageTo = 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Address filter
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(), // Address matches "New York"
                        9, // Page size
                        pageNumber, // Page number
                        null, // Order by (not specified in test)
                        true
                    ),
                Times.Once
            ); // Verify method is called exactly once
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenNoAgeFilterNoSearchWithSearchAddressAndReviewScore4()
        {
            // Arrange
            int? ageFrom = null; // No ageFrom filter
            int? ageTo = null; // No ageTo filter
            string search = null; // No search term for tutor's name
            string searchAddress = "New York"; // Search term for address is provided
            int reviewScore = 4; // Review score filter set to 4 (should only include tutors with review score 4)
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Review score matches the filter of 4
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe", // FullName does not need to match any search term
                        Address =
                            "New York" // Address matches the filter for "New York"
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Review score should match 4
                    FullName = "John Doe", // FullName does not need to match any search term
                    Address = "New York", // Address should match the search term "New York"
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            // Mock repository to return the tutor data
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No age filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Address filter should match "New York"
                        reviewScore, // Review score filter, should match tutors with score 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No search filter for name
                        It.IsAny<string>(), // Address filter should match "New York"
                        9, // Page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // Order by
                        It.IsAny<bool>()
                    )
                ) // Is descending
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify that the repository was called once with the correct parameters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No age filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Address filter for "New York"
                        It.IsAny<int>(), // Review score should match 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No search filter for name
                        It.IsAny<string>(), // Address filter should match "New York"
                        9, // Page size
                        pageNumber, // Page number
                        null, // Order by (unspecified in the test)
                        true
                    ),
                Times.Once
            ); // Verify it was called once
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenNoAgeFilterSearchNoSearchAndSearchAddressWithReviewScoreZero()
        {
            // Arrange
            int? ageFrom = null; // No ageFrom filter
            int? ageTo = null; // No ageTo filter
            string search = null; // No search term for tutor's name
            string searchAddress = "New York"; // Search term for address is provided
            int reviewScore = 0; // Review score filter set to 0 (should only include tutors with review score 0)
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5, // Review score matches the filter of 0
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe", // FullName does not need to match any search term
                        Address =
                            "New York" // Address matches the filter for "New York"
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5, // Review score should match 0
                    FullName = "John Doe", // FullName does not need to match any search term
                    Address = "New York", // Address should match the search term "New York"
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            // Mock repository to return the tutor data
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No age filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Address filter should match "New York"
                        reviewScore, // Review score filter, should match tutors with score 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No search filter for name
                        It.IsAny<string>(), // Address filter should match "New York"
                        9, // Page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // Order by
                        It.IsAny<bool>()
                    )
                ) // Is descending
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify that the repository was called once with the correct parameters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No age filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Address filter for "New York"
                        It.IsAny<int>(), // Review score should match 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No search filter for name
                        It.IsAny<string>(), // Address filter should match "New York"
                        9, // Page size
                        pageNumber, // Page number
                        null, // Order by (unspecified in the test)
                        true
                    ),
                Times.Once
            ); // Verify it was called once
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenNoSearchNoAddressAndReviewScoreZero()
        {
            // Arrange
            int? ageFrom = null; // No ageFrom filter
            int? ageTo = null; // No ageTo filter
            string search = null; // No search term for tutor's name
            string searchAddress = null; // No search address filter
            int reviewScore = 0; // Review score filter set to 0 (should only include tutors with review score 0)
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 0, // Review score matches the filter of 0
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe", // FullName does not need to match any search term
                        Address =
                            "New York" // Address does not need to match any address search term
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5, // Review score should match 0
                    FullName = "John Doe", // FullName does not need to match any search term
                    Address = "New York", // Address does not need to match any address search term
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            // Mock repository to return the tutor data
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No age filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No address filter
                        reviewScore, // Review score filter, should match tutors with score 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No search filter for name
                        It.IsAny<string>(), // No search filter for address
                        9, // Page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // Order by
                        It.IsAny<bool>()
                    )
                ) // Is descending
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify that the repository was called once with the correct parameters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No age filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No address filter
                        It.IsAny<int>(), // Review score should match 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No search filter for name
                        It.IsAny<string>(), // No address filter
                        9, // Page size
                        pageNumber, // Page number
                        null, // Order by (unspecified in the test)
                        true
                    ),
                Times.Once
            ); // Verify it was called once
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenNoSearchNoAddressAndReviewScoreFour()
        {
            // Arrange
            int? ageFrom = null; // No ageFrom filter
            int? ageTo = null; // No ageTo filter
            string search = null; // No search term for tutor's name
            string searchAddress = null; // No search address filter
            int reviewScore = 4; // Review score filter set to 4
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Should match because the review score filter is set to 4
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe", // FullName does not need to match any search term
                        Address =
                            "New York" // Address does not need to match any address search term
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Review score should match 4
                    FullName = "John Doe", // FullName does not need to match any search term
                    Address = "New York", // Address does not need to match any address search term
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            // Mock repository to return the tutor data
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No age filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No address filter
                        reviewScore, // Review score filter, should match tutors with score 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No search filter for name
                        It.IsAny<string>(), // No search filter for address
                        9, // Page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // Order by
                        It.IsAny<bool>()
                    )
                ) // Is descending
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify that the repository was called once with the correct parameters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No age filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No address filter
                        It.IsAny<int>(), // Review score should match 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No search filter for name
                        It.IsAny<string>(), // No address filter
                        9, // Page size
                        pageNumber, // Page number
                        null, // Order by (unspecified in the test)
                        true
                    ),
                Times.Once
            ); // Verify it was called once
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenSearchAndReviewScoreAreProvided_NoAgeAndAddressFilter()
        {
            // Arrange
            int? ageFrom = null; // No ageFrom filter
            int? ageTo = null; // No ageTo filter
            string search = "John"; // Search term for tutor's name
            string searchAddress = null; // No search address filter
            int reviewScore = 4; // Review score filter, should match tutors with score 4
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Should match because the review score filter is set to 4
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe", // FullName should match "John"
                        Address =
                            "New York" // No address filter, so this should be included
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Review score should match 4
                    FullName = "John Doe", // FullName should match "John"
                    Address = "New York", // Address should match "New York"
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            // Mock repository to return the tutor data
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No age filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No address filter
                        reviewScore, // Review score filter, should match tutors with score 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name filter for "John"
                        It.IsAny<string>(), // No address filter
                        9, // Page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // Order by
                        It.IsAny<bool>()
                    )
                ) // Is descending
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify that the repository was called once with the correct parameters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No age filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No address filter
                        It.IsAny<int>(), // Review score should match 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name filter should be used for "John"
                        It.IsAny<string>(), // No address filter
                        9, // Page size
                        pageNumber, // Page number
                        null, // Order by (unspecified in the test)
                        true
                    ),
                Times.Once
            ); // Verify it was called once
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenSearchIsProvided_NoAddressFilter_ReviewScoreZero()
        {
            // Arrange
            int? ageFrom = null; // No ageFrom filter
            int? ageTo = null; // No ageTo filter
            string search = "John"; // Search term for tutor's name
            string searchAddress = null; // No search address filter
            int reviewScore = 0; // Review score filter set to 0
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5, // Should match because the review score filter is set to 0
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe", // FullName should match "John"
                        Address =
                            "New York" // No address filter, so this should be included
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5, // Review score should match 0
                    FullName = "John Doe", // FullName should match "John"
                    Address = "New York", // Address should match "New York"
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            // Mock repository to return the tutor data
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No age filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No address filter
                        reviewScore, // Review score filter, should match tutors with score 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name filter for "John"
                        It.IsAny<string>(), // No address filter
                        9, // Page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // Order by
                        It.IsAny<bool>()
                    )
                ) // Is descending
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify that the repository was called once with the correct parameters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No age filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No address filter
                        It.IsAny<int>(), // Review score should match 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name filter should be used for "John"
                        It.IsAny<string>(), // No address filter
                        9, // Page size
                        pageNumber, // Page number
                        null, // Order by (unspecified in the test)
                        true
                    ),
                Times.Once
            ); // Verify it was called once
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenSearchIsProvided_NoAddressFilter_ReviewScoreFour()
        {
            // Arrange
            int? ageFrom = null; // No ageFrom filter
            int? ageTo = null; // No ageTo filter
            string search = "John"; // Search term for tutor's name
            string searchAddress = null; // No search address filter
            int reviewScore = 4; // Review score filter set to 4
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Should match because the review score filter is set to 4
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe", // FullName should match "John"
                        Address =
                            "New York" // No address filter, so this should be included
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4,
                    FullName = "John Doe", // FullName should match "John"
                    Address = "New York", // Address should match "New York"
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            // Mock repository to return the tutor data
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No age filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No address filter
                        reviewScore, // Review score filter, should match tutors with score 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name filter for "John"
                        It.IsAny<string>(), // No address filter
                        9, // Page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // Order by
                        It.IsAny<bool>()
                    )
                ) // Is descending
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify that the repository was called once with the correct parameters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No age filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No address filter
                        It.IsAny<int>(), // Review score should match 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name filter should be used for "John"
                        It.IsAny<string>(), // No address filter
                        9, // Page size
                        pageNumber, // Page number
                        null, // Order by (unspecified in the test)
                        true
                    ),
                Times.Once
            ); // Verify it was called once
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenSearchAndSearchAddressAreProvided_ReviewScoreFour()
        {
            // Arrange
            int? ageFrom = null; // No ageFrom filter
            int? ageTo = null; // No ageTo filter
            string search = "John"; // Search term for tutor's name
            string searchAddress = "New York"; // Address filter
            int reviewScore = 4; // Review score filter set to 4
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Should match because the review score filter is set to 4
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe", // FullName should match "John"
                        Address =
                            "New York" // Address should match "New York"
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4,
                    FullName = "John Doe", // FullName should match "John"
                    Address = "New York", // Address should match "New York"
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            // Mock repository to return the tutor data
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No age filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No address filter
                        reviewScore, // Review score filter, should match tutors with score 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name filter for "John"
                        It.IsAny<string>(), // Search address filter for "New York"
                        9, // Page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // Order by
                        It.IsAny<bool>()
                    )
                ) // Is descending
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify that the repository was called once with the correct parameters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No age filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No age filter
                        It.IsAny<int>(), // Review score should match 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name filter should be used for "John"
                        It.IsAny<string>(), // Address filter should match "New York"
                        9, // Page size
                        pageNumber, // Page number
                        null, // Order by (unspecified in the test)
                        true
                    ),
                Times.Once
            ); // Verify it was called once
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenSearchAndSearchAddressAreProvided_ReviewScoreZero()
        {
            // Arrange
            int? ageFrom = null; // No ageFrom filter
            int? ageTo = null; // No ageTo filter
            string search = "John"; // Search term for tutor's name
            string searchAddress = "New York"; // Address filter
            int reviewScore = 0; // Review score filter set to 0
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5, // Should match because the review score filter is set to 0
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe", // FullName should match "John"
                        Address =
                            "New York" // Address should match "New York"
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5,
                    FullName = "John Doe", // FullName should match "John"
                    Address = "New York", // Address should match "New York"
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            // Mock repository to return the tutor data
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No age filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No address filter
                        reviewScore, // Review score filter, should match tutors with score 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name filter for "John"
                        It.IsAny<string>(), // Search address filter for "New York"
                        9, // Page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // Order by
                        It.IsAny<bool>()
                    )
                ) // Is descending
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify that the repository was called once with the correct parameters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No age filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No age filter
                        It.IsAny<int>(), // Review score should match 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name filter should be used for "John"
                        It.IsAny<string>(), // Address filter should match "New York"
                        9, // Page size
                        pageNumber, // Page number
                        null, // Order by (unspecified in the test)
                        true
                    ),
                Times.Once
            ); // Verify it was called once
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenNoSearchAndNoSearchAddress_ReviewScoreZero()
        {
            // Arrange
            int ageFrom = -1; // Invalid ageFrom
            int ageTo = 0; // Invalid ageTo
            string search = null; // No search term
            string searchAddress = null; // No search address
            int reviewScore = 0; // Review score filter set to 0
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5, // Should match because the review score filter is set to 0
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe",
                        Address =
                            "New York" // Address is ignored in this case since no address filter is provided
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5,
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            // Mock repository to return the tutor data
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No name filter (search term is null)
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No address filter
                        reviewScore, // Review score filter, should match tutors with score 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age filter (invalid should be reset)
                        It.IsAny<string>(), // Include properties
                        9, // Page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // Order by
                        It.IsAny<bool>()
                    )
                ) // Is descending
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify that the repository was called once with the correct parameters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No search term
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No address filter
                        It.IsAny<int>(), // Review score should match 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age filter (invalid should be reset)
                        It.IsAny<string>(), // Include properties
                        9, // Page size
                        pageNumber, // Page number
                        null, // Order by (unspecified in the test)
                        true
                    ),
                Times.Once
            ); // Verify it was called once
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenNoSearchAndNoSearchAddress_ReviewScoreFour()
        {
            // Arrange
            int ageFrom = -1; // Invalid ageFrom
            int ageTo = 0; // Invalid ageTo
            string search = null; // No search term
            string searchAddress = null; // No search address
            int reviewScore = 4; // Review score filter set to 4
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Should match because the review score filter is set to 4
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe",
                        Address =
                            "New York" // Address is ignored in this case since no address filter is provided
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4,
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            // Mock repository to return the tutor data
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No name filter (search term is null)
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No address filter
                        reviewScore, // Review score filter, should match tutors with score 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age filter (invalid should be reset)
                        It.IsAny<string>(), // Include properties
                        9, // Page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // Order by
                        It.IsAny<bool>()
                    )
                ) // Is descending
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify that the repository was called once with the correct parameters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No search term
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No address filter
                        It.Is<int?>(score => score == reviewScore), // Review score should match 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age filter (invalid should be reset)
                        It.IsAny<string>(), // Include properties
                        9, // Page size
                        pageNumber, // Page number
                        null, // Order by (unspecified in the test)
                        true
                    ),
                Times.Once
            ); // Verify it was called once
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenValidFiltersAreProvided_WithNoSearchAndSearchAddress_ReviewScoreFour()
        {
            // Arrange
            int ageFrom = -1; // Invalid ageFrom
            int ageTo = 0; // Invalid ageTo
            string search = null; // No search term
            string searchAddress = "New York"; // Search address filter
            int reviewScore = 4; // Review score filter set to 4
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Should match because the review score filter is set to 4
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe",
                        Address =
                            "New York" // Address filter should match this tutor's address
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4,
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            // Mock repository to return the tutor data
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No name filter (search term is null)
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Address filter (should contain 'New York')
                        reviewScore, // Review score filter, should match tutors with score 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age filter (invalid should be reset)
                        It.IsAny<string>(), // Include properties
                        9, // Page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // Order by
                        It.IsAny<bool>()
                    )
                ) // Is descending
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify that the repository was called once with the correct parameters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No search term
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Address filter matching 'New York'
                        It.Is<int?>(score => score == reviewScore), // Review score should match 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age filter (invalid should be reset)
                        It.IsAny<string>(), // Include properties
                        9, // Page size
                        pageNumber, // Page number
                        null, // Order by (unspecified in the test)
                        true
                    ),
                Times.Once
            ); // Verify it was called once
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenValidFiltersAreProvided_WithNoSearchAndSearchAddress_ReviewScoreZero()
        {
            // Arrange
            int ageFrom = -1; // Invalid ageFrom
            int ageTo = 0; // Invalid ageTo
            string search = null; // No search term
            string searchAddress = "New York"; // Search address filter
            int reviewScore = 0; // Review score filter set to 0
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5, // Should match because the review score filter is set to 0
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe",
                        Address =
                            "New York" // Address filter should match this tutor's address
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5,
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            // Mock repository to return the tutor data
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No name filter (search term is null)
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Address filter (should contain 'New York')
                        reviewScore, // Review score filter, should match tutors with score 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age filter (invalid should be reset)
                        It.IsAny<string>(), // Include properties
                        9, // Page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // Order by
                        It.IsAny<bool>()
                    )
                ) // Is descending
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify that the repository was called once with the correct parameters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // No search term
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Address filter matching 'New York'
                        It.Is<int?>(score => score == reviewScore), // Review score should match 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age filter (invalid should be reset)
                        It.IsAny<string>(), // Include properties
                        9, // Page size
                        pageNumber, // Page number
                        null, // Order by (unspecified in the test)
                        true
                    ),
                Times.Once
            ); // Verify it was called once
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenValidFiltersAreProvided_WithReviewScoreZero_AndNoSearchAddress()
        {
            // Arrange
            int ageFrom = -1; // Invalid ageFrom
            int ageTo = 0; // Invalid ageTo
            var search = "John"; // Valid search term
            string searchAddress = null; // No address filter
            int reviewScore = 0; // Review score filter set to 0
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 0, // Should match because the review score filter is set to 0
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe",
                        Address =
                            "New York" // Address filter is null, so it shouldn't affect the result
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 5,
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            // Mock repository to return the tutor data
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name filter (search term)
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Address filter (null here)
                        reviewScore, // Review score filter, should match tutors with score 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age filter (invalid ages should be reset)
                        It.IsAny<string>(), // Include properties
                        9, // Page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // Order by
                        It.IsAny<bool>()
                    )
                ) // Is descending
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify that the repository was called once with the correct parameters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name filter (search term)
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Address filter (null here)
                        It.Is<int?>(score => score == reviewScore), // Review score should match 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age filter (invalid should be reset)
                        It.IsAny<string>(), // Include properties
                        9, // Page size
                        pageNumber, // Page number
                        null, // Order by (unspecified in the test)
                        true
                    ),
                Times.Once
            ); // Verify it was called once
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenValidFiltersAreProvided_AndNoSearchAddress()
        {
            // Arrange
            int ageFrom = -1; // Invalid ageFrom
            int ageTo = 0; // Invalid ageTo
            var search = "John"; // Valid search term
            string searchAddress = null; // No address filter
            int reviewScore = 4; // Review score filter set to 4
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Should match because the review score filter is set to 4
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe",
                        Address =
                            "New York" // Address filter is null, so it shouldn't affect the result
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4,
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            // Mock repository to return the tutor data
            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name filter (search term)
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Address filter (null here)
                        reviewScore, // Review score filter, should match tutors with score 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age filter (invalid ages should be reset)
                        It.IsAny<string>(), // Include properties
                        9, // Page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // Order by
                        It.IsAny<bool>()
                    )
                ) // Is descending
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify that the repository was called once with the correct parameters
            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name filter (search term)
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Address filter (null here)
                        It.Is<int?>(score => score == reviewScore), // Review score should match 4
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age filter (invalid should be reset)
                        It.IsAny<string>(), // Include properties
                        9, // Page size
                        pageNumber, // Page number
                        null, // Order by (unspecified in the test)
                        true
                    ),
                Times.Once
            ); // Verify it was called once
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenValidFiltersAreProvided_AndSearchAddressIsNull()
        {
            // Arrange
            int ageFrom = -1; // Invalid ageFrom
            int ageTo = 0; // Invalid ageTo
            var search = "John"; // Valid search term
            string searchAddress = null; // No address filter
            int reviewScore = 0; // Review score is 0, should apply default value
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Should match because reviewScore filter is 0, so no filter is applied
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe",
                        Address =
                            "New York" // Address filter is null, so it shouldn't affect the result
                        ,
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4,
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Address filter (null here)
                        reviewScore, // Review score filter, should allow any tutors since it's 0
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age filter (invalid ages should be reset)
                        It.IsAny<string>(), // Include properties
                        9, // Page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // Order by
                        It.IsAny<bool>()
                    )
                ) // Is descending
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<int?>(), // Review score filter (0 should pass through)
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age filter (invalid should be reset)
                        It.IsAny<string>(),
                        9,
                        pageNumber,
                        null,
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithDefaultAgeRange_WhenInvalidAgeRangeAndReviewScoreZeroAreProvided()
        {
            // Arrange
            int ageFrom = -1; // Invalid ageFrom
            int ageTo = 0; // Invalid ageTo
            var search = "John";
            var searchAddress = "New York";
            int reviewScore = 0; // Default value should be applied
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4, // Should not filter out due to default review score
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe",
                        Address = "New York",
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4,
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Name filter
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Address filter
                        reviewScore,
                        It.IsAny<Expression<Func<Tutor, bool>>>(), // Age filter
                        It.IsAny<string>(), // Include properties
                        9, // Page size
                        pageNumber, // Page number
                        It.IsAny<Expression<Func<Tutor, object>>>(), // Order by
                        It.IsAny<bool>()
                    )
                ) // Is descending
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<int?>(), // Review score
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(),
                        9,
                        pageNumber,
                        null,
                        true
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnTutorsWithPagination_WhenValidFiltersAreProvided()
        {
            // Arrange
            int ageFrom = -1;
            int ageTo = 0;
            var search = "John";
            var searchAddress = "New York";
            int reviewScore = 4;
            int pageNumber = 1;

            var mockTutors = new List<Tutor>
            {
                new Tutor
                {
                    TutorId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4,
                    User = new ApplicationUser
                    {
                        Id = "tutorId",
                        FullName = "John Doe",
                        Address = "New York",
                    },
                    CreatedDate = DateTime.Now.Date,
                },
            };

            var mockTutorDTOs = new List<TutorDTO>
            {
                new TutorDTO
                {
                    UserId = "tutorId",
                    StartAge = 5,
                    EndAge = 10,
                    ReviewScore = 4,
                    FullName = "John Doe",
                    Address = "New York",
                    Certificates = new List<CertificateDTO>(),
                    Curriculums = new List<CurriculumDTO>(),
                    WorkExperiences = new List<WorkExperienceDTO>(),
                    CreatedDate = DateTime.Now.Date,
                },
            };

            _mockTutorRepository
                .Setup(repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        reviewScore,
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(),
                        9,
                        pageNumber,
                        It.IsAny<Expression<Func<Tutor, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((mockTutors.Count, mockTutors));

            // Act
            var result = await _controller.GetAllAsync(
                search,
                searchAddress,
                reviewScore,
                ageFrom,
                ageTo,
                pageNumber
            );

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().BeEquivalentTo(mockTutorDTOs);
            response.Pagination.PageNumber.Should().Be(pageNumber);
            response.Pagination.PageSize.Should().Be(9);
            response.Pagination.Total.Should().Be(mockTutors.Count);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _mockTutorRepository.Verify(
                repo =>
                    repo.GetAllTutorAsync(
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<int?>(),
                        It.IsAny<Expression<Func<Tutor, bool>>>(),
                        It.IsAny<string>(),
                        9,
                        pageNumber,
                        null,
                        true
                    ),
                Times.Once
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
                StatusChange = (int)Status.APPROVE,
            };

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        false,
                        null,
                        null
                    )
                )
                .ReturnsAsync((TutorProfileUpdateRequest)null);

            _mockResourceService
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.UPDATE_PROFILE_REQUEST))
                .Returns("The update profile request was not found.");

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
                StatusChange = (int)Status.APPROVE,
            };

            var mockRequest = new TutorProfileUpdateRequest
            {
                Id = requestId,
                RequestStatus = Status.PENDING,
            };
            var mockRequestReturn = new TutorProfileUpdateRequest
            {
                Id = requestId,
                RequestStatus = Status.REJECT,
            };
            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        false,
                        null,
                        null
                    )
                )
                .ReturnsAsync(mockRequest);

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<TutorProfileUpdateRequest>()))
                .ReturnsAsync(mockRequestReturn);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "staffUserId"),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
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
                StatusChange = (int)Status.REJECT,
            };

            var mockRequest = new TutorProfileUpdateRequest
            {
                Id = requestId,
                RequestStatus = Status.PENDING,
            };

            var mockRequestReturn = new TutorProfileUpdateRequest
            {
                Id = requestId,
                RequestStatus = Status.REJECT,
            };

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<TutorProfileUpdateRequest, bool>>>(),
                        false,
                        null,
                        null
                    )
                )
                .ReturnsAsync(mockRequest);

            _mockTutorProfileUpdateRequestRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<TutorProfileUpdateRequest>()))
                .ReturnsAsync(mockRequestReturn);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "staffUserId"),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
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
