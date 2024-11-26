using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
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
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static AutismEduConnectSystem.SD;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace AutismEduConnectSystem.Controllers.v1.Tests
{
    public class WorkExperienceControllerTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IWorkExperienceRepository> _workExperienceRepositoryMock;
        private readonly IMapper _mockMapper;
        private readonly Mock<IRabbitMQMessageSender> _mockMessageBus;
        private readonly Mock<IResourceService> _resourceServiceMock;
        private readonly Mock<ILogger<WorkExperienceController>> _mockLogger;
        private readonly Mock<IConfiguration> _configurationMock = new Mock<IConfiguration>();
        private readonly WorkExperienceController _controller;

        public WorkExperienceControllerTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _workExperienceRepositoryMock = new Mock<IWorkExperienceRepository>();
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingConfig());
            });
            _mockMapper = config.CreateMapper();
            _mockMessageBus = new Mock<IRabbitMQMessageSender>();
            _resourceServiceMock = new Mock<IResourceService>();
            _mockLogger = new Mock<ILogger<WorkExperienceController>>();

            _configurationMock.Setup(config => config["APIConfig:PageSize"]).Returns("10");
            _configurationMock
                .Setup(config => config["RabbitMQSettings:QueueName"])
                .Returns("TestQueue");
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller = new WorkExperienceController(
                _mockUserRepository.Object,
                _workExperienceRepositoryMock.Object,
                _mockMapper,
                _configurationMock.Object,
                _mockMessageBus.Object,
                _resourceServiceMock.Object,
                _mockLogger.Object
            );
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_StaffRole_Search_StatusPending_OrderByCreatedDate_Sort_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "staff-user-id";
            var search = "Middle"; // Search string
            var status = STATUS_PENDING; // Status Pending
            var orderBy = nameof(WorkExperience.CreatedDate); // Order by CreatedDate
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the STAFF_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Oldest Company",
                    SubmitterId = "user1",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Middle Company",
                    SubmitterId = "user2",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
                new WorkExperience
                {
                    Id = 3,
                    CompanyName = "Newest Company",
                    SubmitterId = "user3",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.UtcNow,
                },
            };

            // Filter the work experiences based on the search term
            workExperiences = workExperiences
                .Where(w => w.CompanyName.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Sort based on the sort parameter
            if (sort == SD.ORDER_ASC)
                workExperiences = workExperiences.OrderBy(w => w.CreatedDate).ToList();
            else
                workExperiences = workExperiences.OrderByDescending(w => w.CreatedDate).ToList();

            // Set up repository mock to return filtered work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(1); // Only one result matches the search term

            // Verify the result matches the search and order
            var expected = workExperiences.First();
            resultList[0].CompanyName.Should().Be(expected.CompanyName);
            resultList[0].CreatedDate.Should().Be(expected.CreatedDate);
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_StaffRole_WithSearch_StatusAll_OrderByCreatedDate_Sort_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "staff-user-id";
            var search = "Company"; // Search string to filter by CompanyName
            var status = STATUS_ALL; // No filtering by status
            var orderBy = nameof(WorkExperience.CreatedDate); // Order by CreatedDate
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the STAFF_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Oldest Company",
                    SubmitterId = "user1",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Middle Company",
                    SubmitterId = "user2",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
                new WorkExperience
                {
                    Id = 3,
                    CompanyName = "Newest Company",
                    SubmitterId = "user3",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.UtcNow,
                },
            };

            // Filter by search query
            workExperiences = workExperiences
                .Where(w => w.CompanyName.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Sort the results based on the `sort` parameter
            if (sort == SD.ORDER_ASC)
                workExperiences = workExperiences.OrderBy(w => w.CreatedDate).ToList();
            else
                workExperiences = workExperiences.OrderByDescending(w => w.CreatedDate).ToList();

            // Set up repository mock to return filtered and sorted work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(workExperiences.Count); // Number of returned work experiences

            // Verify the order of results
            if (sort == SD.ORDER_ASC)
            {
                resultList[0].CompanyName.Should().Be("Oldest Company");
                resultList[1].CompanyName.Should().Be("Middle Company");
                resultList[2].CompanyName.Should().Be("Newest Company");

                resultList[0].CreatedDate.Should().BeBefore(resultList[1].CreatedDate);
                resultList[1].CreatedDate.Should().BeBefore(resultList[2].CreatedDate);
            }
            else if (sort == SD.ORDER_DESC)
            {
                resultList[0].CompanyName.Should().Be("Newest Company");
                resultList[1].CompanyName.Should().Be("Middle Company");
                resultList[2].CompanyName.Should().Be("Oldest Company");

                resultList[0].CreatedDate.Should().BeAfter(resultList[1].CreatedDate);
                resultList[1].CreatedDate.Should().BeAfter(resultList[2].CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_StaffRole_WithSearch_StatusRejected_OrderByCreatedDate_Sort_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "staff-user-id";
            var search = "Company"; // Search string
            var status = STATUS_REJECT; // Status Rejected
            var orderBy = nameof(WorkExperience.CreatedDate); // Order by CreatedDate
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the STAFF_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Rejected Company A",
                    SubmitterId = "user1",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Rejected Company B",
                    SubmitterId = "user2",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
                new WorkExperience
                {
                    Id = 3,
                    CompanyName = "Rejected Company C",
                    SubmitterId = "user3",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.UtcNow,
                },
            };

            if (sort == SD.ORDER_ASC)
                workExperiences = workExperiences.OrderBy(w => w.CreatedDate).ToList();
            else
                workExperiences = workExperiences.OrderByDescending(w => w.CreatedDate).ToList();

            // Set up repository mock to return filtered work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(3); // All 3 work experiences should be returned

            // Verify the order of results
            if (sort == SD.ORDER_ASC)
            {
                resultList[0].CompanyName.Should().Be("Rejected Company A");
                resultList[1].CompanyName.Should().Be("Rejected Company B");
                resultList[2].CompanyName.Should().Be("Rejected Company C");

                resultList[0].CreatedDate.Should().BeBefore(resultList[1].CreatedDate);
                resultList[1].CreatedDate.Should().BeBefore(resultList[2].CreatedDate);
            }
            else if (sort == SD.ORDER_DESC)
            {
                resultList[0].CompanyName.Should().Be("Rejected Company C");
                resultList[1].CompanyName.Should().Be("Rejected Company B");
                resultList[2].CompanyName.Should().Be("Rejected Company A");

                resultList[0].CreatedDate.Should().BeAfter(resultList[1].CreatedDate);
                resultList[1].CreatedDate.Should().BeAfter(resultList[2].CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_StaffRole_WithSearch_StatusApproved_OrderByCreatedDate_Sort_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "staff-user-id";
            var search = "Company"; // Search string
            var status = STATUS_APPROVE; // Status Approved
            var orderBy = nameof(WorkExperience.CreatedDate); // Order by CreatedDate
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the STAFF_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Oldest Company",
                    SubmitterId = "user1",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Middle Company",
                    SubmitterId = "user2",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
                new WorkExperience
                {
                    Id = 3,
                    CompanyName = "Newest Company",
                    SubmitterId = "user3",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.UtcNow,
                },
            };

            if (sort == SD.ORDER_ASC)
                workExperiences = workExperiences.OrderBy(w => w.CreatedDate).ToList();
            else
                workExperiences = workExperiences.OrderByDescending(w => w.CreatedDate).ToList();

            // Set up repository mock to return filtered work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(3); // All 3 work experiences should be returned

            // Verify the order of results
            if (sort == SD.ORDER_ASC)
            {
                resultList[0].CompanyName.Should().Be("Oldest Company");
                resultList[1].CompanyName.Should().Be("Middle Company");
                resultList[2].CompanyName.Should().Be("Newest Company");

                resultList[0].CreatedDate.Should().BeBefore(resultList[1].CreatedDate);
                resultList[1].CreatedDate.Should().BeBefore(resultList[2].CreatedDate);
            }
            else if (sort == SD.ORDER_DESC)
            {
                resultList[0].CompanyName.Should().Be("Newest Company");
                resultList[1].CompanyName.Should().Be("Middle Company");
                resultList[2].CompanyName.Should().Be("Oldest Company");

                resultList[0].CreatedDate.Should().BeAfter(resultList[1].CreatedDate);
                resultList[1].CreatedDate.Should().BeAfter(resultList[2].CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_StaffRole_NoSearch_StatusAll_OrderByCreatedDate_Sort_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "staff-user-id";
            var search = string.Empty; // No search string
            var status = STATUS_ALL; // Status All (null or no filter)
            var orderBy = nameof(WorkExperience.CreatedDate); // Order by CreatedDate
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the STAFF_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences with different statuses
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Oldest Company",
                    SubmitterId = "user1",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Middle Company",
                    SubmitterId = "user2",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                },
                new WorkExperience
                {
                    Id = 3,
                    CompanyName = "Newest Company",
                    SubmitterId = "user3",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            if (sort == SD.ORDER_ASC)
                workExperiences = workExperiences.OrderBy(w => w.CreatedDate).ToList();
            else
                workExperiences = workExperiences.OrderByDescending(w => w.CreatedDate).ToList();

            // Set up repository mock to return all work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(3); // All 3 work experiences should be returned

            // Verify the order of results
            if (sort == SD.ORDER_ASC)
            {
                resultList[0].CompanyName.Should().Be("Oldest Company");
                resultList[1].CompanyName.Should().Be("Middle Company");
                resultList[2].CompanyName.Should().Be("Newest Company");

                resultList[0].CreatedDate.Should().BeBefore(resultList[1].CreatedDate);
                resultList[1].CreatedDate.Should().BeBefore(resultList[2].CreatedDate);
            }
            else if (sort == SD.ORDER_DESC)
            {
                resultList[0].CompanyName.Should().Be("Newest Company");
                resultList[1].CompanyName.Should().Be("Middle Company");
                resultList[2].CompanyName.Should().Be("Oldest Company");

                resultList[0].CreatedDate.Should().BeAfter(resultList[1].CreatedDate);
                resultList[1].CreatedDate.Should().BeAfter(resultList[2].CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_StaffRole_NoSearch_StatusApprove_OrderByCreatedDate_Sort_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "staff-user-id";
            var search = string.Empty; // No search string
            var status = STATUS_APPROVE; // Status Approve
            var orderBy = nameof(WorkExperience.CreatedDate); // Order by CreatedDate
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the STAFF_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Oldest Approved Company",
                    SubmitterId = "user1",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Middle Approved Company",
                    SubmitterId = "user2",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                },
                new WorkExperience
                {
                    Id = 3,
                    CompanyName = "Newest Approved Company",
                    SubmitterId = "user3",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Sort the work experiences based on the sort parameter
            if (sort == SD.ORDER_ASC)
                workExperiences = workExperiences.OrderBy(w => w.CreatedDate).ToList();
            else
                workExperiences = workExperiences.OrderByDescending(w => w.CreatedDate).ToList();

            // Set up repository mock to return filtered work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(3); // All 3 work experiences should be returned

            // Verify the order of results
            if (sort == SD.ORDER_ASC)
            {
                resultList[0].CompanyName.Should().Be("Oldest Approved Company");
                resultList[1].CompanyName.Should().Be("Middle Approved Company");
                resultList[2].CompanyName.Should().Be("Newest Approved Company");

                resultList[0].CreatedDate.Should().BeBefore(resultList[1].CreatedDate);
                resultList[1].CreatedDate.Should().BeBefore(resultList[2].CreatedDate);
            }
            else if (sort == SD.ORDER_DESC)
            {
                resultList[0].CompanyName.Should().Be("Newest Approved Company");
                resultList[1].CompanyName.Should().Be("Middle Approved Company");
                resultList[2].CompanyName.Should().Be("Oldest Approved Company");

                resultList[0].CreatedDate.Should().BeAfter(resultList[1].CreatedDate);
                resultList[1].CreatedDate.Should().BeAfter(resultList[2].CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_StaffRole_NoSearch_StatusReject_OrderByCreatedDate_Sort_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "staff-user-id";
            var search = string.Empty; // No search string
            var status = STATUS_REJECT; // Status Rejected
            var orderBy = nameof(WorkExperience.CreatedDate); // Order by CreatedDate
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the STAFF_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Oldest Rejected Company",
                    SubmitterId = "user1",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Middle Rejected Company",
                    SubmitterId = "user2",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                },
                new WorkExperience
                {
                    Id = 3,
                    CompanyName = "Newest Rejected Company",
                    SubmitterId = "user3",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.UtcNow,
                },
            };

            if (sort == SD.ORDER_ASC)
                workExperiences = workExperiences.OrderBy(w => w.CreatedDate).ToList();
            else
                workExperiences = workExperiences.OrderByDescending(w => w.CreatedDate).ToList();

            // Set up repository mock to return filtered work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(3); // All 3 work experiences should be returned

            // Verify the order of results
            if (sort == SD.ORDER_ASC)
            {
                resultList[0].CompanyName.Should().Be("Oldest Rejected Company");
                resultList[1].CompanyName.Should().Be("Middle Rejected Company");
                resultList[2].CompanyName.Should().Be("Newest Rejected Company");

                resultList[0].CreatedDate.Should().BeBefore(resultList[1].CreatedDate);
                resultList[1].CreatedDate.Should().BeBefore(resultList[2].CreatedDate);
            }
            else if (sort == SD.ORDER_DESC)
            {
                resultList[0].CompanyName.Should().Be("Newest Rejected Company");
                resultList[1].CompanyName.Should().Be("Middle Rejected Company");
                resultList[2].CompanyName.Should().Be("Oldest Rejected Company");

                resultList[0].CreatedDate.Should().BeAfter(resultList[1].CreatedDate);
                resultList[1].CreatedDate.Should().BeAfter(resultList[2].CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRole_NoSearch_StatusPending_OrderByCreatedDate_Sort_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "test-user-id";
            var search = string.Empty; // No search string
            var status = STATUS_PENDING; // Status Pending
            var orderBy = "CreatedDate"; // Order by CreatedDate
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING, // Status set to PENDING
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING, // Status set to PENDING
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
                new WorkExperience
                {
                    Id = 3,
                    CompanyName = "Pending Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING, // Status set to PENDING
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                },
            };
            if (sort == "asc")
                workExperiences = workExperiences.OrderBy(x => x.CreatedDate).ToList();
            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(3); // All 3 work experiences should be returned

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in the specified sort order
            if (sort == SD.ORDER_ASC)
            {
                workExperienceDtos[0].CompanyName.Should().Be("Pending Company");
                workExperienceDtos[1].CompanyName.Should().Be("Another Company");
                workExperienceDtos[2].CompanyName.Should().Be("Test Company");

                workExperienceDtos[0]
                    .CreatedDate.Should()
                    .BeBefore(workExperienceDtos[1].CreatedDate);
                workExperienceDtos[1]
                    .CreatedDate.Should()
                    .BeBefore(workExperienceDtos[2].CreatedDate);
            }
            else if (sort == SD.ORDER_DESC)
            {
                workExperienceDtos[0].CompanyName.Should().Be("Test Company");
                workExperienceDtos[1].CompanyName.Should().Be("Another Company");
                workExperienceDtos[2].CompanyName.Should().Be("Pending Company");

                workExperienceDtos[0]
                    .CreatedDate.Should()
                    .BeAfter(workExperienceDtos[1].CreatedDate);
                workExperienceDtos[1]
                    .CreatedDate.Should()
                    .BeAfter(workExperienceDtos[2].CreatedDate);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)]
        [InlineData(SD.ORDER_DESC)]
        public async Task GetAllAsync_TutorRole_NoSearch_StatusPending_OrderByNull_Sort_ReturnsOkResponse(
            string sort
        )
        {
            // Arrange
            var userId = "test-user-id";
            var search = string.Empty; // No search string
            var status = STATUS_PENDING; // Status Pending
            var orderBy = (string)null; // No order by (null)
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING, // Status set to PENDING
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING, // Status set to PENDING
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
                new WorkExperience
                {
                    Id = 3,
                    CompanyName = "Pending Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING, // Status set to PENDING
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                },
            };
            if (sort == "asc")
                workExperiences = workExperiences.OrderBy(x => x.CreatedDate).ToList();

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(3); // All 3 work experiences should be returned

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in the specified sort order
            if (sort == SD.ORDER_ASC)
            {
                workExperienceDtos[0].CompanyName.Should().Be("Pending Company");
                workExperienceDtos[1].CompanyName.Should().Be("Another Company");
                workExperienceDtos[2].CompanyName.Should().Be("Test Company");

                workExperienceDtos[0]
                    .CreatedDate.Should()
                    .BeBefore(workExperienceDtos[1].CreatedDate);
                workExperienceDtos[1]
                    .CreatedDate.Should()
                    .BeBefore(workExperienceDtos[2].CreatedDate);
            }
            else if (sort == SD.ORDER_DESC)
            {
                workExperienceDtos[0].CompanyName.Should().Be("Test Company");
                workExperienceDtos[1].CompanyName.Should().Be("Another Company");
                workExperienceDtos[2].CompanyName.Should().Be("Pending Company");

                workExperienceDtos[0]
                    .CreatedDate.Should()
                    .BeAfter(workExperienceDtos[1].CreatedDate);
                workExperienceDtos[1]
                    .CreatedDate.Should()
                    .BeAfter(workExperienceDtos[2].CreatedDate);
            }
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_NoSearch_StatusAll_OrderByNull_SortAscending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = string.Empty; // No search string
            var status = STATUS_ALL; // Status All (includes all statuses)
            var orderBy = (string)null; // No order by (null)
            var sort = SD.ORDER_ASC; // Ascending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.REJECT, // Status set to REJECT
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
                new WorkExperience
                {
                    Id = 3,
                    CompanyName = "Rejected Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync(
                    (workExperiences.Count, workExperiences.OrderBy(x => x.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(3); // All 3 work experiences should be returned

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in ascending order
            workExperienceDtos[0].CompanyName.Should().Be("Rejected Company");
            workExperienceDtos[1].CompanyName.Should().Be("Another Company");
            workExperienceDtos[2].CompanyName.Should().Be("Test Company");

            workExperienceDtos[0].CreatedDate.Should().BeBefore(workExperienceDtos[1].CreatedDate);
            workExperienceDtos[1].CreatedDate.Should().BeBefore(workExperienceDtos[2].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_NoSearch_StatusAll_OrderByNull_SortDescending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = string.Empty; // No search string
            var status = STATUS_ALL; // Status All (includes all statuses)
            var orderBy = (string)null; // No order by (null)
            var sort = SD.ORDER_DESC; // Descending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.REJECT, // Status set to REJECT
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
                new WorkExperience
                {
                    Id = 3,
                    CompanyName = "Rejected Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(3); // All 3 work experiences should be returned

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in descending order
            workExperienceDtos[0].CompanyName.Should().Be("Test Company");
            workExperienceDtos[1].CompanyName.Should().Be("Another Company");
            workExperienceDtos[2].CompanyName.Should().Be("Rejected Company");

            workExperienceDtos[0].CreatedDate.Should().BeAfter(workExperienceDtos[1].CreatedDate);
            workExperienceDtos[1].CreatedDate.Should().BeAfter(workExperienceDtos[2].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_NoSearch_StatusAll_OrderByCreatedDate_SortDescending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = string.Empty; // No search string
            var status = STATUS_ALL; // Status All (includes all statuses)
            var orderBy = "CreatedDate"; // Order by CreatedDate
            var sort = SD.ORDER_DESC; // Descending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.REJECT, // Status set to REJECT
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
                new WorkExperience
                {
                    Id = 3,
                    CompanyName = "Rejected Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(3); // All 3 work experiences should be returned

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in descending order
            workExperienceDtos[0].CompanyName.Should().Be("Test Company");
            workExperienceDtos[1].CompanyName.Should().Be("Another Company");
            workExperienceDtos[2].CompanyName.Should().Be("Rejected Company");

            workExperienceDtos[0].CreatedDate.Should().BeAfter(workExperienceDtos[1].CreatedDate);
            workExperienceDtos[1].CreatedDate.Should().BeAfter(workExperienceDtos[2].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_NoSearch_StatusAll_OrderByCreatedDate_SortAscending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = string.Empty; // No search string
            var status = STATUS_ALL; // Status All (includes all statuses)
            var orderBy = "CreatedDate"; // Order by CreatedDate
            var sort = SD.ORDER_ASC; // Ascending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.REJECT, // Status set to REJECT
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
                new WorkExperience
                {
                    Id = 3,
                    CompanyName = "Rejected Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync(
                    (workExperiences.Count, workExperiences.OrderBy(x => x.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(3); // All 3 work experiences should be returned

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in ascending order
            workExperienceDtos[0].CompanyName.Should().Be("Rejected Company");
            workExperienceDtos[1].CompanyName.Should().Be("Another Company");
            workExperienceDtos[2].CompanyName.Should().Be("Test Company");

            workExperienceDtos[0].CreatedDate.Should().BeBefore(workExperienceDtos[1].CreatedDate);
            workExperienceDtos[1].CreatedDate.Should().BeBefore(workExperienceDtos[2].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_NoSearch_StatusApproved_OrderByCreatedDate_SortAscending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = string.Empty; // No search string
            var status = STATUS_APPROVE; // Status Approved
            var orderBy = "CreatedDate"; // Order by CreatedDate
            var sort = SD.ORDER_ASC; // Ascending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync(
                    (workExperiences.Count, workExperiences.OrderBy(x => x.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in ascending order
            workExperienceDtos[0].CompanyName.Should().Be("Another Company");
            workExperienceDtos[1].CompanyName.Should().Be("Test Company");
            workExperienceDtos[0].CreatedDate.Should().BeBefore(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_NoSearch_StatusApproved_OrderByCreatedDate_SortDescending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = string.Empty; // No search string
            var status = STATUS_APPROVE; // Status Approved
            var orderBy = "CreatedDate"; // Order by CreatedDate
            var sort = SD.ORDER_DESC; // Descending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in descending order
            workExperienceDtos[0].CompanyName.Should().Be("Test Company");
            workExperienceDtos[1].CompanyName.Should().Be("Another Company");
            workExperienceDtos[0].CreatedDate.Should().BeAfter(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_NoSearch_StatusApproved_OrderByNull_SortDescending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = string.Empty; // No search string
            var status = STATUS_APPROVE; // Status Approved
            var orderBy = (string)null; // No order by specified (null)
            var sort = SD.ORDER_DESC; // Descending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in descending order
            workExperienceDtos[0].CompanyName.Should().Be("Test Company");
            workExperienceDtos[1].CompanyName.Should().Be("Another Company");
            workExperienceDtos[0].CreatedDate.Should().BeAfter(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_NoSearch_StatusApproved_OrderByNull_SortAscending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = string.Empty; // No search string
            var status = STATUS_APPROVE; // Status Approved
            var orderBy = (string)null; // No order by specified (null)
            var sort = SD.ORDER_ASC; // Ascending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync(
                    (workExperiences.Count, workExperiences.OrderBy(x => x.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in ascending order
            workExperienceDtos[0].CompanyName.Should().Be("Another Company");
            workExperienceDtos[1].CompanyName.Should().Be("Test Company");
            workExperienceDtos[0].CreatedDate.Should().BeBefore(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_NoSearch_StatusRejected_OrderByNull_SortDescending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = string.Empty; // No search string
            var status = STATUS_REJECT; // Status Rejected
            var orderBy = (string)null; // No order by specified (null)
            var sort = SD.ORDER_DESC; // Descending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.REJECT, // Status set to REJECT
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.REJECT, // Status set to REJECT
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in descending order
            workExperienceDtos[0].CompanyName.Should().Be("Test Company");
            workExperienceDtos[1].CompanyName.Should().Be("Another Company");
            workExperienceDtos[0].CreatedDate.Should().BeAfter(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_NoSearch_StatusRejected_OrderByNull_SortAscending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = string.Empty; // No search string
            var status = STATUS_REJECT; // Status Rejected
            var orderBy = (string)null; // No order by specified (null)
            var sort = SD.ORDER_ASC; // Ascending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.REJECT, // Status set to REJECT
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.REJECT, // Status set to REJECT
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync(
                    (workExperiences.Count, workExperiences.OrderBy(x => x.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in ascending order
            workExperienceDtos[0].CompanyName.Should().Be("Another Company");
            workExperienceDtos[1].CompanyName.Should().Be("Test Company");
            workExperienceDtos[0].CreatedDate.Should().BeBefore(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_NoSearch_StatusRejected_OrderByCreatedDate_SortAscending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = string.Empty; // No search string
            var status = STATUS_REJECT; // Status Rejected
            var orderBy = "CreatedDate"; // Order by CreatedDate
            var sort = SD.ORDER_ASC; // Ascending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.REJECT, // Status set to REJECT
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.REJECT, // Status set to REJECT
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync(
                    (workExperiences.Count, workExperiences.OrderBy(x => x.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in ascending order
            workExperienceDtos[0].CompanyName.Should().Be("Another Company");
            workExperienceDtos[1].CompanyName.Should().Be("Test Company");
            workExperienceDtos[0].CreatedDate.Should().BeBefore(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_NoSearch_StatusRejected_OrderByCreatedDate_SortDescending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = string.Empty; // No search string
            var status = STATUS_REJECT; // Status Rejected
            var orderBy = "CreatedDate"; // Order by CreatedDate
            var sort = SD.ORDER_DESC; // Descending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.REJECT, // Status set to REJECT
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.REJECT, // Status set to REJECT
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in descending order
            workExperienceDtos[0].CompanyName.Should().Be("Test Company");
            workExperienceDtos[1].CompanyName.Should().Be("Another Company");
            workExperienceDtos[0].CreatedDate.Should().BeAfter(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_Search_StatusRejected_OrderByCreatedDate_SortAscending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = "company";
            var status = STATUS_REJECT; // Status Rejected
            var orderBy = "CreatedDate"; // Order by CreatedDate
            var sort = SD.ORDER_ASC; // Ascending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.REJECT, // Status set to REJECT
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.REJECT, // Status set to REJECT
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync(
                    (workExperiences.Count, workExperiences.OrderBy(x => x.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in ascending order
            workExperienceDtos[0].CompanyName.Should().Be("Another Company");
            workExperienceDtos[1].CompanyName.Should().Be("Test Company");
            workExperienceDtos[0].CreatedDate.Should().BeBefore(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_Search_StatusRejected_OrderByCreatedDate_SortDescending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = "company";
            var status = STATUS_REJECT; // Status Rejected
            var orderBy = "CreatedDate"; // Order by CreatedDate
            var sort = SD.ORDER_DESC; // Descending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.REJECT, // Status set to REJECT
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.REJECT, // Status set to REJECT
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in descending order
            workExperienceDtos[0].CompanyName.Should().Be("Test Company");
            workExperienceDtos[1].CompanyName.Should().Be("Another Company");
            workExperienceDtos[0].CreatedDate.Should().BeAfter(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_Search_StatusRejected_OrderByNull_SortDescending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = "company";
            var status = STATUS_REJECT; // Status Rejected
            var orderBy = (string)null; // No order by specified (null)
            var sort = SD.ORDER_DESC; // Descending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.REJECT, // Status set to REJECT
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.REJECT, // Status set to REJECT
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in descending order
            workExperienceDtos[0].CompanyName.Should().Be("Test Company");
            workExperienceDtos[1].CompanyName.Should().Be("Another Company");
            workExperienceDtos[0].CreatedDate.Should().BeAfter(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_Search_StatusRejected_OrderByNull_SortAscending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = "company";
            var status = STATUS_REJECT; // Status Rejected
            var orderBy = (string)null; // No order by specified (null)
            var sort = SD.ORDER_ASC; // Ascending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.REJECT, // Status set to REJECT
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.REJECT, // Status set to REJECT
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync(
                    (workExperiences.Count, workExperiences.OrderBy(x => x.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in ascending order
            workExperienceDtos[0].CompanyName.Should().Be("Another Company");
            workExperienceDtos[1].CompanyName.Should().Be("Test Company");
            workExperienceDtos[0].CreatedDate.Should().BeBefore(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_Search_StatusPending_OrderByNull_SortDescending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = "company";
            var status = STATUS_PENDING; // Status Pending
            var orderBy = (string)null; // No order by specified (null)
            var sort = SD.ORDER_DESC; // Descending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING, // Status set to PENDING
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING, // Status set to PENDING
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in descending order
            workExperienceDtos[0].CompanyName.Should().Be("Test Company");
            workExperienceDtos[1].CompanyName.Should().Be("Another Company");
            workExperienceDtos[0].CreatedDate.Should().BeAfter(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_Search_StatusPending_OrderByCreatedDate_SortDescending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = "company";
            var status = STATUS_PENDING; // Status Pending
            var orderBy = SD.CREATED_DATE; // Order by CreatedDate
            var sort = SD.ORDER_DESC; // Descending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING, // Status set to PENDING
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING, // Status set to PENDING
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in descending order
            workExperienceDtos[0].CompanyName.Should().Be("Test Company");
            workExperienceDtos[1].CompanyName.Should().Be("Another Company");
            workExperienceDtos[0].CreatedDate.Should().BeAfter(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_Search_StatusPending_OrderByNull_SortAscending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = "company";
            var status = STATUS_PENDING; // Status Pending
            var orderBy = (string)null; // No order by specified (null)
            var sort = SD.ORDER_ASC; // Ascending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING, // Status set to PENDING
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING, // Status set to PENDING
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync(
                    (workExperiences.Count, workExperiences.OrderBy(x => x.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in ascending order
            workExperienceDtos[0].CompanyName.Should().Be("Another Company");
            workExperienceDtos[1].CompanyName.Should().Be("Test Company");
            workExperienceDtos[0].CreatedDate.Should().BeBefore(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_Search_StatusPending_OrderByCreatedDate_SortAscending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = "company";
            var status = STATUS_PENDING; // Status Pending
            var orderBy = SD.CREATED_DATE; // Order by CreatedDate
            var sort = SD.ORDER_ASC; // Ascending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING, // Status set to PENDING
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING, // Status set to PENDING
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync(
                    (workExperiences.Count, workExperiences.OrderBy(x => x.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in ascending order
            workExperienceDtos[0].CompanyName.Should().Be("Another Company");
            workExperienceDtos[1].CompanyName.Should().Be("Test Company");
            workExperienceDtos[0].CreatedDate.Should().BeBefore(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_Search_StatusApproved_OrderByCreatedDate_SortDescending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = "company";
            var status = STATUS_APPROVE; // Status Approved
            var orderBy = SD.CREATED_DATE; // Order by CreatedDate
            var sort = SD.ORDER_DESC; // Descending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in descending order
            workExperienceDtos[0].CompanyName.Should().Be("Test Company");
            workExperienceDtos[1].CompanyName.Should().Be("Another Company");
            workExperienceDtos[0].CreatedDate.Should().BeAfter(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_Search_StatusApproved_OrderByCreatedDate_SortAscending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = "company";
            var status = STATUS_APPROVE; // Status Approved
            var orderBy = SD.CREATED_DATE; // Order by CreatedDate
            var sort = SD.ORDER_ASC; // Ascending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experiences to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync(
                    (workExperiences.Count, workExperiences.OrderBy(x => x.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in ascending order
            workExperienceDtos[0].CompanyName.Should().Be("Another Company");
            workExperienceDtos[1].CompanyName.Should().Be("Test Company");
            workExperienceDtos[0].CreatedDate.Should().BeBefore(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_Search_StatusApproved_OrderByNull_SortAscending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = "company";
            var status = STATUS_APPROVE; // Status Approved
            var orderBy = (string)null; // No order by specified (null)
            var sort = SD.ORDER_ASC; // Ascending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experience to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync(
                    (workExperiences.Count, workExperiences.OrderBy(x => x.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in ascending order
            workExperienceDtos[0].CompanyName.Should().Be("Another Company");
            workExperienceDtos[1].CompanyName.Should().Be("Test Company");
            workExperienceDtos[0].CreatedDate.Should().BeBefore(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_Search_StatusApproved_OrderByNull_SortDescending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = "company";
            var status = STATUS_APPROVE; // Status Approved
            var orderBy = (string)null; // No order by specified (null)
            var sort = SD.ORDER_DESC; // Descending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experience to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Status set to APPROVED
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in descending order
            workExperienceDtos[0].CompanyName.Should().Be("Test Company");
            workExperienceDtos[1].CompanyName.Should().Be("Another Company");
            workExperienceDtos[0].CreatedDate.Should().BeAfter(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_Search_StatusAll_OrderByNull_SortAscending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = "company";
            var status = SD.STATUS_ALL; // All status
            var orderBy = (string)null; // No order by specified (null)
            var sort = SD.ORDER_ASC; // Ascending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experience to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync(
                    (workExperiences.Count, workExperiences.OrderBy(x => x.CreatedDate).ToList())
                );

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in ascending order
            workExperienceDtos[0].CompanyName.Should().Be("Another Company");
            workExperienceDtos[1].CompanyName.Should().Be("Test Company");
            workExperienceDtos[0].CreatedDate.Should().BeBefore(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_Search_StatusAll_OrderByNull_SortDescending_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = "company";
            var status = SD.STATUS_ALL; // All status
            var orderBy = (string)null; // No order by specified (null)
            var sort = SD.ORDER_DESC; // Descending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experience to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is in descending order by CreatedDate
            workExperienceDtos[0].CompanyName.Should().Be("Test Company");
            workExperienceDtos[1].CompanyName.Should().Be("Another Company");
            workExperienceDtos[0].CreatedDate.Should().BeAfter(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_Search_StatusAll_OrderByCreatedDateDESC_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = "company";
            var status = SD.STATUS_ALL; // All status
            var orderBy = SD.CREATED_DATE; // Order by Created Date
            var sort = SD.ORDER_DESC; // Descending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experience to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();

            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;

            // Verify that the order of WorkExperienceDTO objects is by CreatedDate in descending order
            workExperienceDtos[0].CompanyName.Should().Be("Test Company");
            workExperienceDtos[1].CompanyName.Should().Be("Another Company");
            workExperienceDtos[0].CreatedDate.Should().BeAfter(workExperienceDtos[1].CreatedDate);
        }

        [Fact]
        public async Task GetAllAsync_TutorRole_Search_StatusAll_OrderByCreatedDateASC_ReturnsOkResponse()
        {
            // Arrange
            var userId = "test-user-id";
            var search = "company";
            var status = SD.STATUS_ALL; // All status
            var orderBy = SD.CREATED_DATE; // Order by Created Date
            var sort = SD.ORDER_ASC; // Ascending order
            var pageNumber = 1;
            var pageSize = 10;

            // Create a mock user with the TUTOR_ROLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Create a list of work experience to be returned from the repository
            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.UtcNow,
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Another Company",
                    SubmitterId = userId,
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                },
            };

            // Set up repository mock to return work experiences
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        pageSize,
                        pageNumber,
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((workExperiences.Count, workExperiences));

            // Act
            var result = await _controller.GetAllAsync(search, status, orderBy, sort, pageNumber);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<APIResponse>().Subject;

            // Assert APIResponse is successful and the result is a list of DTOs
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.Result.Should().BeOfType<List<WorkExperienceDTO>>();
            var resultList = apiResponse.Result as List<WorkExperienceDTO>;
            resultList.Should().HaveCount(2);

            // Check that the returned list contains the expected WorkExperienceDTO objects
            var workExperienceDtos = apiResponse
                .Result.Should()
                .BeOfType<List<WorkExperienceDTO>>()
                .Subject;
            workExperienceDtos
                .Should()
                .Contain(x =>
                    x.CompanyName == "Test Company"
                    && x.CreatedDate == workExperiences[0].CreatedDate
                );
            workExperienceDtos
                .Should()
                .Contain(x =>
                    x.CompanyName == "Another Company"
                    && x.CreatedDate == workExperiences[1].CreatedDate
                );
        }

        [Fact]
        public async Task GetAllAsync_ReturnsUnauthorized_WhenUserIsUnauthorized()
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
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Act
            var result = await _controller.GetAllAsync(null, "all", CREATED_DATE, ORDER_DESC, 1);
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
        public async Task GetAllAsync_ReturnsForbiden_WhenUserDoesNotHaveRequiredRole()
        {
            // Arrange
            _resourceServiceMock
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbiden access.");

            // Simulate a user with no valid claims (unauthorized)
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "testUserId") };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Act
            var result = await _controller.GetAllAsync(null, "all", CREATED_DATE, ORDER_DESC, 1);
            var unauthorizedResult = result.Result as ObjectResult;

            // Assert
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

            var apiResponse = unauthorizedResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            apiResponse.ErrorMessages.First().Should().Be("Forbiden access.");
        }

        [Fact]
        public async Task GetAllAsync_InternalServerError_ReturnsInternalServerError()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(
                    ClaimTypes.Role,
                    SD.TUTOR_ROLE
                ) // Add role for TUTOR
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);
            _controller.ControllerContext.HttpContext = mockHttpContext.Object;

            // Mock repository to throw an exception (simulate an internal server error)
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<Expression<Func<WorkExperience, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ThrowsAsync(new Exception("Database connection error"));

            // Act
            var result = await _controller.GetAllAsync(null);

            // Assert
            var internalServerErrorResult = result.Result as ObjectResult;
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult
                .StatusCode.Should()
                .Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = internalServerErrorResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Internal server error");
        }

        [Fact]
        public async Task UpdateStatusRequest_StatusReject_TemplateDoesNotExistOrTutorNull_DoesNotSendEmail()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusDTO { Id = 1, StatusChange = (int)Status.REJECT };

            var userId = Guid.NewGuid().ToString();
            var workExperience = new WorkExperience
            {
                Id = changeStatusDTO.Id,
                RequestStatus = Status.PENDING,
                CompanyName = "Test Company",
                SubmitterId = Guid.NewGuid().ToString(),
            };

            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Templates",
                "ChangeStatusTemplate.cshtml"
            );

            // Mocking User claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            // Mock HttpContext for the controller
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);
            _controller.ControllerContext.HttpContext = mockHttpContext.Object;

            // Mock repository and dependencies
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        false,
                        null,
                        null
                    )
                )
                .ReturnsAsync(workExperience);
            _mockUserRepository
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), false, null)
                )
                .ReturnsAsync((ApplicationUser)null);
            _workExperienceRepositoryMock
                .Setup(repo => repo.DeactivatePreviousVersionsAsync(It.IsAny<int?>()))
                .Returns(Task.CompletedTask);
            _workExperienceRepositoryMock
                .Setup(repo => repo.UpdateAsync(It.IsAny<WorkExperience>()))
                .ReturnsAsync(workExperience);

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();

            // Verify email was not sent
            _mockMessageBus.Verify(
                bus => bus.SendMessage(It.IsAny<EmailLogger>(), It.IsAny<string>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpdateStatusRequest_StatusReject_TemplateExistsAndTutorNotNull_SendsEmail()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusDTO { Id = 1, StatusChange = (int)Status.REJECT };

            var userId = Guid.NewGuid().ToString();
            var workExperience = new WorkExperience
            {
                Id = changeStatusDTO.Id,
                RequestStatus = Status.PENDING,
                CompanyName = "Test Company",
                SubmitterId = Guid.NewGuid().ToString(),
            };
            var tutor = new ApplicationUser
            {
                Id = workExperience.SubmitterId,
                FullName = "Test Tutor",
                Email = "tutor@test.com",
            };
            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Templates",
                "ChangeStatusTemplate.cshtml"
            );
            var templateContent =
                "Hello @Model.FullName, your request has been @Model.IsApprovedString.";

            // Mocking User claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            // Mock HttpContext for the controller
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);
            _controller.ControllerContext.HttpContext = mockHttpContext.Object;

            // Mock repository and dependencies
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        false,
                        null,
                        null
                    )
                )
                .ReturnsAsync(workExperience);
            _mockUserRepository
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), false, null)
                )
                .ReturnsAsync(tutor);
            _workExperienceRepositoryMock
                .Setup(repo => repo.DeactivatePreviousVersionsAsync(It.IsAny<int?>()))
                .Returns(Task.CompletedTask);
            _workExperienceRepositoryMock
                .Setup(repo => repo.UpdateAsync(It.IsAny<WorkExperience>()))
                .ReturnsAsync(workExperience);

            // Mock template reading

            // Mock message bus
            _mockMessageBus
                .Setup(bus =>
                    bus.SendMessage(
                        It.Is<EmailLogger>(e =>
                            e.Email == tutor.Email
                            && e.Subject.Contains("đã bị từ chối")
                            && e.Message.Contains(tutor.FullName)
                        ),
                        It.IsAny<string>()
                    )
                )
                .Verifiable();

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateStatusRequest_StatusApprove_TemplateDoesNotExistOrTutorNull_DoesNotSendEmail()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusDTO
            {
                Id = 1,
                StatusChange = (int)Status.APPROVE,
            };

            var userId = Guid.NewGuid().ToString();
            var workExperience = new WorkExperience
            {
                Id = changeStatusDTO.Id,
                RequestStatus = Status.PENDING,
                CompanyName = "Test Company",
                SubmitterId = Guid.NewGuid().ToString(),
            };

            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Templates",
                "ChangeStatusTemplate.cshtml"
            );

            // Mocking User claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            // Mock HttpContext for the controller
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);
            _controller.ControllerContext.HttpContext = mockHttpContext.Object;

            // Mock repository and dependencies
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        false,
                        null,
                        null
                    )
                )
                .ReturnsAsync(workExperience);
            _mockUserRepository
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), false, null)
                )
                .ReturnsAsync((ApplicationUser)null);
            _workExperienceRepositoryMock
                .Setup(repo => repo.DeactivatePreviousVersionsAsync(It.IsAny<int?>()))
                .Returns(Task.CompletedTask);
            _workExperienceRepositoryMock
                .Setup(repo => repo.UpdateAsync(It.IsAny<WorkExperience>()))
                .ReturnsAsync(workExperience);

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();

            // Verify email was not sent
            _mockMessageBus.Verify(
                bus => bus.SendMessage(It.IsAny<EmailLogger>(), It.IsAny<string>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpdateStatusRequest_StatusApprove_TemplateExistsAndTutorNotNull_SendsEmail()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusDTO
            {
                Id = 1,
                StatusChange = (int)Status.APPROVE,
            };

            var userId = Guid.NewGuid().ToString();
            var workExperience = new WorkExperience
            {
                Id = changeStatusDTO.Id,
                RequestStatus = Status.PENDING,
                CompanyName = "Test Company",
                SubmitterId = Guid.NewGuid().ToString(),
            };
            var tutor = new ApplicationUser
            {
                Id = workExperience.SubmitterId,
                FullName = "Test Tutor",
                Email = "tutor@test.com",
            };
            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Templates",
                "ChangeStatusTemplate.cshtml"
            );
            var templateContent =
                "Hello @Model.FullName, your request has been @Model.IsApprovedString.";
            // Mocking User claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            // Mock HttpContext for the controller
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);
            _controller.ControllerContext.HttpContext = mockHttpContext.Object;

            // Mock repository and dependencies
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        false,
                        null,
                        null
                    )
                )
                .ReturnsAsync(workExperience);
            _mockUserRepository
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), false, null)
                )
                .ReturnsAsync(tutor);
            _workExperienceRepositoryMock
                .Setup(repo => repo.DeactivatePreviousVersionsAsync(It.IsAny<int?>()))
                .Returns(Task.CompletedTask);
            _workExperienceRepositoryMock
                .Setup(repo => repo.UpdateAsync(It.IsAny<WorkExperience>()))
                .ReturnsAsync(workExperience);

            // Mock message bus
            _mockMessageBus
                .Setup(bus => bus.SendMessage(It.IsAny<EmailLogger>(), It.IsAny<string>()))
                .Verifiable();

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateStatusRequest_InternalServerError_ReturnsInternalServerErrorResponse()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusDTO
            {
                Id = 1,
                StatusChange = (int)Status.APPROVE,
            };

            var userId = Guid.NewGuid().ToString();
            var expectedErrorMessage = "An unexpected error occurred.";

            // Mocking User claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            // Mock HttpContext for the controller
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);
            _controller.ControllerContext.HttpContext = mockHttpContext.Object;

            // Mock the repository to throw an exception
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        false,
                        null,
                        null
                    )
                )
                .ThrowsAsync(new Exception(expectedErrorMessage));
            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("An unexpected error occurred.");
            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);

            // Assert
            result.Should().NotBeNull();
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = objectResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            apiResponse
                .ErrorMessages.Should()
                .ContainSingle()
                .Which.Should()
                .Be(expectedErrorMessage);
        }

        [Fact]
        public async Task UpdateStatusRequest_ShouldReturn_BadRequest_WhenWorkExperienceIsInvalid()
        {
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "staffId"),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(userClaims, "mock")
            );
            // Arrange
            var changeStatusDTO = new ChangeStatusDTO { Id = 1 };

            _workExperienceRepositoryMock
                .Setup(r =>
                    r.GetAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        false,
                        null,
                        null
                    )
                )
                .ReturnsAsync((WorkExperience)null);

            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.WORK_EXPERIENCE))
                .Returns("Work experience not found or already processed.");

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response
                .ErrorMessages.Should()
                .Contain("Work experience not found or already processed.");
        }

        [Fact]
        public async Task UpdateStatusRequest_ShouldReturn_BadRequest_WhenModelStateIsInvalid()
        {
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "staffId"),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE),
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(userClaims, "mock")
            );
            // Arrange
            _controller.ModelState.AddModelError("TestError", "Invalid model state.");
            var changeStatusDTO = new ChangeStatusDTO();

            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.WORK_EXPERIENCE))
                .Returns("Invalid request.");

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("Invalid request.");
        }

        [Fact]
        public async Task UpdateStatusRequest_ShouldReturn_Forbidden_WhenUserRoleIsInvalid()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusDTO();
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "testUserId") };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
            _resourceServiceMock
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbidden.");

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("Forbidden.");
        }

        [Fact]
        public async Task UpdateStatusRequest_ShouldReturn_Unauthorized_WhenUserIdIsMissing()
        {
            var claimsIdentity = new ClaimsIdentity(); // No claims added
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            // Arrange
            var changeStatusDTO = new ChangeStatusDTO();

            _resourceServiceMock
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized.");

            // Act
            var result = await _controller.UpdateStatusRequest(changeStatusDTO);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            var response = objectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessages.Should().Contain("Unauthorized.");
        }

        [Fact]
        public async Task CreateAsync_ReturnsInternalServerError_WhenExceptionOccurs()
        {
            // Arrange
            var createDTO = new WorkExperienceCreateDTO();

            _workExperienceRepositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<WorkExperience>()))
                .ThrowsAsync(new Exception("Database error"));
            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");

            // Act
            var result = await _controller.CreateAsync(createDTO);

            // Assert
            var statusCodeResult = result.Result as ObjectResult;
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = statusCodeResult.Value as APIResponse;
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Internal server error");
        }

        [Fact]
        public async Task CreateAsync_ReturnsCreated_WhenSuccessful_WithOriginalIdDifZero()
        {
            // Arrange
            var createDTO = new WorkExperienceCreateDTO { OriginalId = 1 };
            var newModel = new WorkExperience();

            _workExperienceRepositoryMock
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        null,
                        null,
                        null,
                        true
                    )
                )
                .ReturnsAsync((0, new List<WorkExperience>()));

            _workExperienceRepositoryMock.Setup(r => r.CreateAsync(newModel));

            // Act
            var result = await _controller.CreateAsync(createDTO);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.Result.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateAsync_ReturnsCreated_WhenSuccessful_WithOriginalIdIsZero()
        {
            // Arrange
            var createDTO = new WorkExperienceCreateDTO { OriginalId = 0 };
            var newModel = new WorkExperience();

            _workExperienceRepositoryMock
                .Setup(r =>
                    r.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        null,
                        null,
                        null,
                        true
                    )
                )
                .ReturnsAsync((0, new List<WorkExperience>()));

            _workExperienceRepositoryMock.Setup(r => r.CreateAsync(newModel));

            // Act
            var result = await _controller.CreateAsync(createDTO);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.Result.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateAsync_ReturnsBadRequest_WhenPendingRequestExists()
        {
            // Arrange
            var createDTO = new WorkExperienceCreateDTO { OriginalId = 1 };

            var workExperiences = new List<WorkExperience>
            {
                new WorkExperience
                {
                    Id = 1,
                    CompanyName = "Test 2",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-10),
                    Position = "Position 2",
                },
                new WorkExperience
                {
                    Id = 2,
                    CompanyName = "Test 1",
                    CreatedDate = DateTime.UtcNow,
                    Position = "Position 1",
                },
            };
            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        null,
                        null,
                        null,
                        true
                    )
                )
                .ReturnsAsync(
                    (workExperiences.Count, workExperiences.OrderBy(e => e.CreatedDate).ToList())
                );

            _resourceServiceMock
                .Setup(r => r.GetString(SD.IN_STATUS_PENDING, SD.WORK_EXPERIENCE))
                .Returns("Request is already in pending status");

            // Act
            var result = await _controller.CreateAsync(createDTO);

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Request is already in pending status");
        }

        [Fact]
        public async Task CreateAsync_ReturnsBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Name is required");
            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.WORK_EXPERIENCE))
                .Returns("Invalid data provided");

            // Act
            var result = await _controller.CreateAsync(new WorkExperienceCreateDTO());

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Invalid data provided");
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
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            var requestPayload = new WorkExperienceCreateDTO
            {
                CompanyName = "FPT",
                EndDate = null,
                StartDate = new DateTime(2024, 10, 10),
                OriginalId = 0,
                Position = "Dev",
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
        public async Task CreateAsync_ReturnsForbiden_WhenUserDoesNotHaveRequiredRole()
        {
            // Arrange
            _resourceServiceMock
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbiden access.");

            // Simulate a user with no valid claims (unauthorized)
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "testUserId") };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            var requestPayload = new WorkExperienceCreateDTO
            {
                CompanyName = "FPT",
                EndDate = null,
                StartDate = new DateTime(2024, 10, 10),
                OriginalId = 0,
                Position = "Dev",
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
            apiResponse.ErrorMessages.First().Should().Be("Forbiden access.");
        }

        [Fact]
        public async Task DeleteAsync_ReturnsUnauthorized_WhenUserIsUnauthorized()
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
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            var requestPayload = new WorkExperienceCreateDTO
            {
                CompanyName = "FPT",
                EndDate = null,
                StartDate = new DateTime(2024, 10, 10),
                OriginalId = 0,
                Position = "Dev",
            };

            // Act
            var result = await _controller.DeleteAsync(1);
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
        public async Task DeleteAsync_ReturnsForbiden_WhenUserDoesNotHaveRequiredRole()
        {
            // Arrange
            _resourceServiceMock
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbiden access.");

            // Simulate a user with no valid claims (unauthorized)
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "testUserId") };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            var requestPayload = new WorkExperienceCreateDTO
            {
                CompanyName = "FPT",
                EndDate = null,
                StartDate = new DateTime(2024, 10, 10),
                OriginalId = 0,
                Position = "Dev",
            };

            // Act
            var result = await _controller.DeleteAsync(1);
            var unauthorizedResult = result.Result as ObjectResult;

            // Assert
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

            var apiResponse = unauthorizedResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            apiResponse.ErrorMessages.First().Should().Be("Forbiden access.");
        }

        [Fact]
        public async Task DeleteAsync_InvalidId_ReturnsBadRequest()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(userClaims)
            );

            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Invalid ID");

            // Act
            var result = await _controller.DeleteAsync(0);

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var response = badRequestResult.Value as APIResponse;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.ErrorMessages.Should().Contain("Invalid ID");
        }

        [Fact]
        public async Task DeleteAsync_NotFound_ReturnsNotFound()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(userClaims)
            );

            _resourceServiceMock
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.WORK_EXPERIENCE))
                .Returns("Not Found");

            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync((WorkExperience)null);

            // Act
            var result = await _controller.DeleteAsync(1);

            // Assert
            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

            var response = notFoundResult.Value as APIResponse;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.ErrorMessages.Should().Contain("Not Found");
        }

        [Fact]
        public async Task DeleteAsync_ValidDeletion_ReturnsOk()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(userClaims)
            );

            var workExperience = new WorkExperience
            {
                Id = 1,
                SubmitterId = "test-user-id",
                IsActive = true,
                IsDeleted = false,
            };

            var returnUpdated = new WorkExperience
            {
                Id = 1,
                SubmitterId = "test-user-id",
                IsActive = false,
                IsDeleted = true,
            };

            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync(workExperience);

            _workExperienceRepositoryMock
                .Setup(repo => repo.UpdateAsync(It.IsAny<WorkExperience>()))
                .ReturnsAsync(returnUpdated);

            // Act
            var result = await _controller.DeleteAsync(1);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okResult.Value as APIResponse;
            response.IsSuccess.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            _workExperienceRepositoryMock.Verify(
                repo => repo.UpdateAsync(It.IsAny<WorkExperience>()),
                Times.Once
            );
        }

        [Fact]
        public async Task DeleteAsync_InternalServerError_ReturnsServerError()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(userClaims)
            );

            _workExperienceRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ThrowsAsync(new Exception("Test exception"));

            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal Server Error");

            // Act
            var result = await _controller.DeleteAsync(1);

            // Assert
            var errorResult = result.Result as ObjectResult;
            errorResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var response = errorResult.Value as APIResponse;
            response.IsSuccess.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            response.ErrorMessages.Should().Contain("Internal Server Error");
        }
    }
}
