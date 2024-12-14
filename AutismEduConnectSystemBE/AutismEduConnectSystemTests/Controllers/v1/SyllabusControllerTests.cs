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
using AutismEduConnectSystem.DTOs;
using AutismEduConnectSystem.DTOs.CreateDTOs;
using AutismEduConnectSystem.DTOs.UpdateDTOs;
using AutismEduConnectSystem.Repository;
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

namespace AutismEduConnectSystem.Controllers.v1.Tests
{
    public class SyllabusControllerTests
    {
        private readonly Mock<ISyllabusRepository> _mockSyllabusRepository;
        private readonly Mock<IExerciseRepository> _mockExerciseRepository;
        private readonly Mock<ISyllabusExerciseRepository> _mockSyllabusExerciseRepository;
        private readonly Mock<IExerciseTypeRepository> _mockExerciseTypeRepository;
        private readonly Mock<ILogger<SyllabusController>> _mockLogger;
        private readonly IMapper _mockMapper;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IResourceService> _mockResourceService;

        private readonly SyllabusController _controller;

        public SyllabusControllerTests()
        {
            _mockSyllabusRepository = new Mock<ISyllabusRepository>();
            _mockExerciseRepository = new Mock<IExerciseRepository>();
            _mockSyllabusExerciseRepository = new Mock<ISyllabusExerciseRepository>();
            _mockExerciseTypeRepository = new Mock<IExerciseTypeRepository>();
            _mockLogger = new Mock<ILogger<SyllabusController>>();
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingConfig());
            });
            _mockMapper = config.CreateMapper();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockResourceService = new Mock<IResourceService>();

            _mockConfiguration.SetupGet(c => c["APIConfig:PageSize"]).Returns("10");

            _controller = new SyllabusController(
                _mockSyllabusRepository.Object,
                _mockLogger.Object,
                _mockMapper,
                _mockConfiguration.Object,
                _mockExerciseRepository.Object,
                _mockExerciseTypeRepository.Object,
                _mockSyllabusExerciseRepository.Object,
                _mockResourceService.Object
            );
        }

        [Fact]
        public async Task DeleteAsync_ReturnsUnauthorized_WhenUserIsUnauthorized()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");

            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity()); // No claims
            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal,
            };

            // Act
            var result = await _controller.DeleteAsync(1);

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
        public async Task DeleteAsync_ReturnsForbidden_WhenUserDoesNotHaveRequiredRole()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbidden access.");

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "testUserId") };
            var identity = new ClaimsIdentity(claims);
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Act
            var result = await _controller.DeleteAsync(1);

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
        public async Task DeleteAsync_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Invalid ID.");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Act
            var result = await _controller.DeleteAsync(0); // Invalid ID

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            apiResponse.ErrorMessages.First().Should().Be("Invalid ID.");
        }

        [Fact]
        public async Task DeleteAsync_ReturnsInternalServerError_WhenExceptionOccurs()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error.");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Simulate an exception during deletion
            _mockSyllabusRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeleteAsync(1);

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
            apiResponse.ErrorMessages.Should().Contain("Internal server error.");
        }

        [Fact]
        public async Task DeleteAsync_ReturnsNotFound_WhenSyllabusDoesNotExist()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.SYLLABUS))
                .Returns("Syllabus not found.");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Simulate repository returning null (not found)
            _mockSyllabusRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((Syllabus)null);

            // Act
            var result = await _controller.DeleteAsync(1);

            // Assert
            var notFoundResult = result.Result as ObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

            var apiResponse = notFoundResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
            apiResponse.ErrorMessages.First().Should().Be("Syllabus not found.");
        }

        [Fact]
        public async Task DeleteAsync_ReturnsNoContent_WhenDeletionIsSuccessful()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            var syllabus = new Syllabus
            {
                Id = 1,
                TutorId = "testUserId",
                IsDeleted = false,
            };

            // Simulate repository returning the syllabus
            _mockSyllabusRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(syllabus);

            // Act
            var result = await _controller.DeleteAsync(1);

            // Assert
            var noContentResult = result.Result as ObjectResult;
            noContentResult.Should().NotBeNull();
            noContentResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = noContentResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsForbidden_WhenUserIsNotAuthorized()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbidden.");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(
                    ClaimTypes.Role,
                    "NonTutorRole"
                ) // Unauthorized role
                ,
            };
            var identity = new ClaimsIdentity(claims);
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Act
            var result = await _controller.GetAllAsync();

            // Assert
            var forbiddenResult = result.Result as ObjectResult;
            forbiddenResult.Should().NotBeNull();
            forbiddenResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

            var apiResponse = forbiddenResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            apiResponse.ErrorMessages.First().Should().Be("Forbidden.");
        }

        [Fact]
        public async Task GetAllAsync_ReturnsUnauthorized_WhenUserIsNotAuthenticated()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized.");

            var claims = new List<Claim>(); // No claims (unauthenticated user)
            var identity = new ClaimsIdentity(claims);
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Act
            var result = await _controller.GetAllAsync();

            // Assert
            var unauthorizedResult = result.Result as ObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            var apiResponse = unauthorizedResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            apiResponse.ErrorMessages.First().Should().Be("Unauthorized.");
        }

        [Fact]
        public async Task GetAllAsync_ReturnsInternalServerError_WhenExceptionOccurs()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal Server Error.");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Simulate an exception
            _mockSyllabusRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        null,
                        null,
                        It.IsAny<Expression<Func<Syllabus, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ThrowsAsync(new Exception("Some error"));

            // Act
            var result = await _controller.GetAllAsync();

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
            apiResponse.ErrorMessages.First().Should().Be("Internal Server Error.");
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Testing ascending order
        [InlineData(SD.ORDER_DESC)] // Testing descending order
        public async Task GetAllAsync_ReturnsSortedResults_WhenOrderByAgeFromIsSpecified(
            string sort
        )
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal Server Error.");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
            var syllabus1 = new Syllabus
            {
                Id = 1,
                AgeFrom = 5,
                AgeEnd = 10,
                TutorId = "Tutor1",
            };

            var syllabus2 = new Syllabus
            {
                Id = 2,
                AgeFrom = 11,
                AgeEnd = 15,
                TutorId = "Tutor1",
            };

            // Create mock ExerciseType objects
            var exerciseType1 = new ExerciseType { Id = 1, ExerciseTypeName = "Physical" };

            var exerciseType2 = new ExerciseType { Id = 2, ExerciseTypeName = "Mental" };

            // Create mock Exercise objects
            var exercise1 = new Exercise
            {
                Id = 1,
                ExerciseName = "Push-up",
                Description = "A physical exercise",
            };

            var exercise2 = new Exercise
            {
                Id = 2,
                ExerciseName = "Puzzle",
                Description = "A mental exercise",
            };
            // Arrange mock data for syllabi
            var syllabusList = new List<Syllabus>
            {
                new Syllabus { AgeFrom = 5 },
                new Syllabus { AgeFrom = 10 },
                new Syllabus { AgeFrom = 8 },
            };

            var syllabusExercises = new List<SyllabusExercise>
            {
                new SyllabusExercise
                {
                    SyllabusId = 1,
                    Syllabus = syllabus1,
                    ExerciseTypeId = 1,
                    ExerciseType = exerciseType1,
                    ExerciseId = 1,
                    Exercise = exercise1,
                    CreatedDate = DateTime.Now.AddMinutes(-10),
                },
                new SyllabusExercise
                {
                    SyllabusId = 1,
                    Syllabus = syllabus1,
                    ExerciseTypeId = 2,
                    ExerciseType = exerciseType2,
                    ExerciseId = 2,
                    Exercise = exercise2,
                    CreatedDate = DateTime.Now.AddMinutes(-5),
                },
                new SyllabusExercise
                {
                    SyllabusId = 2,
                    Syllabus = syllabus2,
                    ExerciseTypeId = 1,
                    ExerciseType = exerciseType1,
                    ExerciseId = 1,
                    Exercise = exercise1,
                    CreatedDate = DateTime.Now,
                },
            };

            var sortedList =
                sort == SD.ORDER_ASC
                    ? syllabusList.OrderBy(x => x.AgeFrom).ToList()
                    : syllabusList.OrderByDescending(x => x.AgeFrom).ToList();
            // Mock the repository to return the sorted syllabi
            _mockSyllabusRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<Syllabus, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((sortedList.Count, sortedList));
            _mockSyllabusExerciseRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<SyllabusExercise, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<SyllabusExercise, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((syllabusExercises.Count, syllabusExercises));

            // Act
            var result = await _controller.GetAllAsync(orderBy: SD.AGE_FROM, sort: sort);

            // Assert
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();

            var syllabusResult = apiResponse.Result as List<SyllabusDTO>;
            syllabusResult.Should().NotBeNull();
            syllabusResult.Count.Should().Be(3);

            // Assert that the results are sorted correctly
            if (sort == SD.ORDER_ASC)
            {
                syllabusResult.First().AgeFrom.Should().Be(5);
                syllabusResult.Last().AgeFrom.Should().Be(10);
            }
            else if (sort == SD.ORDER_DESC)
            {
                syllabusResult.First().AgeFrom.Should().Be(10);
                syllabusResult.Last().AgeFrom.Should().Be(5);
            }
        }

        [Theory]
        [InlineData(SD.ORDER_ASC)] // Testing ascending order
        [InlineData(SD.ORDER_DESC)] // Testing descending order
        public async Task GetAllAsync_ReturnsSortedResults_WhenOrderByCreatedDateIsSpecified(
            string sort
        )
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal Server Error.");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            // Create mock Syllabus objects
            var syllabus1 = new Syllabus
            {
                Id = 1,
                AgeFrom = 5,
                AgeEnd = 10,
                TutorId = "Tutor1",
            };

            var syllabus2 = new Syllabus
            {
                Id = 2,
                AgeFrom = 11,
                AgeEnd = 15,
                TutorId = "Tutor1",
            };

            // Create mock ExerciseType objects
            var exerciseType1 = new ExerciseType { Id = 1, ExerciseTypeName = "Physical" };
            var exerciseType2 = new ExerciseType { Id = 2, ExerciseTypeName = "Mental" };

            // Create mock Exercise objects
            var exercise1 = new Exercise
            {
                Id = 1,
                ExerciseName = "Push-up",
                Description = "A physical exercise",
            };

            var exercise2 = new Exercise
            {
                Id = 2,
                ExerciseName = "Puzzle",
                Description = "A mental exercise",
            };

            // Create mock SyllabusExercise objects
            var syllabusExercises = new List<SyllabusExercise>
            {
                new SyllabusExercise
                {
                    SyllabusId = 1,
                    Syllabus = syllabus1,
                    ExerciseTypeId = 1,
                    ExerciseType = exerciseType1,
                    ExerciseId = 1,
                    Exercise = exercise1,
                    CreatedDate = DateTime.Now.AddMinutes(-10), // Created 10 minutes ago
                },
                new SyllabusExercise
                {
                    SyllabusId = 1,
                    Syllabus = syllabus1,
                    ExerciseTypeId = 2,
                    ExerciseType = exerciseType2,
                    ExerciseId = 2,
                    Exercise = exercise2,
                    CreatedDate = DateTime.Now.AddMinutes(-20), // Created 5 minutes ago
                },
                new SyllabusExercise
                {
                    SyllabusId = 2,
                    Syllabus = syllabus2,
                    ExerciseTypeId = 1,
                    ExerciseType = exerciseType1,
                    ExerciseId = 1,
                    Exercise = exercise1,
                    CreatedDate = DateTime.Now.AddMinutes(-3), // Created now
                },
            };

            // Create a list of syllabi sorted by CreatedDate
            var syllabusList = new List<Syllabus> { syllabus1, syllabus2 };

            var sortedList =
                sort == SD.ORDER_ASC
                    ? syllabusList
                        .OrderBy(x =>
                            syllabusExercises.First(s => s.SyllabusId == x.Id).CreatedDate
                        )
                        .ToList()
                    : syllabusList
                        .OrderByDescending(x =>
                            syllabusExercises.First(s => s.SyllabusId == x.Id).CreatedDate
                        )
                        .ToList();

            // Mock the repository to return the sorted syllabi
            _mockSyllabusRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<Syllabus, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((sortedList.Count, sortedList));

            // Mock SyllabusExercise repository to return the same exercises
            _mockSyllabusExerciseRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<SyllabusExercise, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<SyllabusExercise, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((syllabusExercises.Count, syllabusExercises));

            // Act
            var result = await _controller.GetAllAsync(orderBy: SD.CREATED_DATE, sort: sort);

            // Assert
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();

            var syllabusResult = apiResponse.Result as List<SyllabusDTO>;
            syllabusResult.Should().NotBeNull();
            syllabusResult.Count.Should().Be(2); // Ensure correct count based on your mock data

            // Assert that the results are sorted by CreatedDate
            if (sort == SD.ORDER_ASC)
            {
                // For ascending order, ensure that the first item has an earlier CreatedDate than the last item
                syllabusResult
                    .First()
                    .CreatedDate.Should()
                    .BeBefore(syllabusResult.Last().CreatedDate);
            }
            else if (sort == SD.ORDER_DESC)
            {
                // For descending order, ensure that the first item has a later CreatedDate than the last item
                syllabusResult
                    .First()
                    .CreatedDate.Should()
                    .BeAfter(syllabusResult.Last().CreatedDate);
            }
        }

        [Fact]
        public async Task GetActive_ReturnsBadRequest_WhenIdIsLessThanOrEqualToZero()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID))
                .Returns("Invalid ID.");

            // Act
            var result = await _controller.GetActive(0);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            apiResponse.ErrorMessages.Should().Contain("Invalid ID.");
        }

        [Fact]
        public async Task GetActive_ReturnsInternalServerError_WhenExceptionOccurs()
        {
            // Arrange
            var id = 1;
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal Server Error.");

            // Simulate an exception by making the repository throw an exception
            _mockSyllabusRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ThrowsAsync(new Exception("Some error"));

            // Act
            var result = await _controller.GetActive(id);

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
            apiResponse.ErrorMessages.Should().Contain("Internal Server Error.");
        }

        [Fact]
        public async Task GetActive_ReturnsNotFound_WhenSyllabusNotFound()
        {
            // Arrange
            var id = 1;
            _mockResourceService
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.SYLLABUS))
                .Returns("Syllabus not found.");

            // Simulate the case where the syllabus is not found in the repository
            _mockSyllabusRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((Syllabus)null); // No syllabus found

            // Act
            var result = await _controller.GetActive(id);

            // Assert
            var notFoundResult = result.Result as ObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

            var apiResponse = notFoundResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
            apiResponse.ErrorMessages.Should().Contain("Syllabus not found.");
        }

        [Fact]
        public async Task GetActive_ReturnsOk_WhenSyllabusFound()
        {
            // Arrange
            var id = 1;
            var syllabus = new Syllabus
            {
                Id = id,
                AgeFrom = 3,
                AgeEnd = 5,
                IsDeleted = false,
                SyllabusExercises = new List<SyllabusExercise>
                {
                    new SyllabusExercise
                    {
                        ExerciseTypeId = 1,
                        SyllabusId = id,
                        ExerciseId = 1,
                    },
                },
            };

            // Setup the repository to return the syllabus
            _mockSyllabusRepository
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(syllabus); // Return the valid syllabus

            // Setup the mapper to return a valid DTO

            // Act
            var result = await _controller.GetActive(id);

            // Assert
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateAsync_ReturnsUnauthorized_WhenUserIsUnauthorized()
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

            var updatePayload = new SyllabusUpdateDTO { AgeFrom = 5, AgeEnd = 10 };

            // Act
            var result = await _controller.UpdateAsync(1, updatePayload);
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
        public async Task UpdateAsync_ReturnsForbidden_WhenUserDoesNotHaveRequiredRole()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbidden access.");

            // Simulate a user without the required role (not a tutor)
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "testUserId") };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            var updatePayload = new SyllabusUpdateDTO { AgeFrom = 5, AgeEnd = 10 };

            // Act
            var result = await _controller.UpdateAsync(1, updatePayload);
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
        public async Task UpdateAsync_ReturnsBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            var updatePayload = new SyllabusUpdateDTO { AgeFrom = 5, AgeEnd = 10 };

            // Simulate a valid user (Tutor role)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _mockResourceService
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.SYLLABUS))
                .Returns("Invalid model state.");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            _controller.ModelState.AddModelError("AgeFrom", "AgeFrom is required");

            // Act
            var result = await _controller.UpdateAsync(1, updatePayload);
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
        public async Task UpdateAsync_ReturnsNotFound_WhenSyllabusDoesNotExist()
        {
            // Arrange
            var updatePayload = new SyllabusUpdateDTO
            {
                Id = 1,
                AgeFrom = 5,
                AgeEnd = 10,
            };

            // Simulate a valid user (Tutor role)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE),
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _mockResourceService
                .Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.SYLLABUS))
                .Returns("Syllabus not found.");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };

            _mockSyllabusRepository
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<Syllabus, bool>>>(), false, null, null)
                )
                .ReturnsAsync((Syllabus)null);

            // Act
            var result = await _controller.UpdateAsync(1, updatePayload);
            var notFoundResult = result.Result as ObjectResult;

            // Assert
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

            var apiResponse = notFoundResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
            apiResponse.ErrorMessages.First().Should().Be("Syllabus not found.");
        }

        [Fact]
        public async Task UpdateAsync_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            // Arrange
            var updatePayload = new SyllabusUpdateDTO
            {
                Id = 1,
                AgeFrom = 5,
                AgeEnd = 10,
            };

            // Simulate a valid user (Tutor role)
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
            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");
            // Simulate an exception during the process
            _mockSyllabusRepository
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<Syllabus, bool>>>(), false, null, null)
                )
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateAsync(1, updatePayload);
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
        public async Task UpdateAsync_ReturnsOk_WhenUpdateIsSuccessful()
        {
            // Arrange
            var syllabusId = 1;
            var updatePayload = new SyllabusUpdateDTO
            {
                Id = 1,
                AgeFrom = 5,
                AgeEnd = 10,
                SyllabusExercises = new List<SyllabusExerciseCreateDTO>(),
            };

            // Simulate a valid user (Tutor role)
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

            var existingSyllabus = new Syllabus
            {
                Id = syllabusId,
                AgeFrom = 3,
                AgeEnd = 6,
            };

            _mockSyllabusRepository
                .Setup(repo =>
                    repo.GetAsync(It.IsAny<Expression<Func<Syllabus, bool>>>(), false, null, null)
                )
                .ReturnsAsync(existingSyllabus);

            _mockSyllabusRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Syllabus>()))
                .ReturnsAsync(existingSyllabus);
            _mockSyllabusRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<Syllabus, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((0, new List<Syllabus>()));
            _mockSyllabusExerciseRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<SyllabusExercise, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<SyllabusExercise, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((0, new List<SyllabusExercise>()));
            // Act
            var result = await _controller.UpdateAsync(syllabusId, updatePayload);
            var okResult = result.Result as ObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
            apiResponse.Result.Should().NotBeNull();
            apiResponse.ErrorMessages.Should().BeNullOrEmpty();
            apiResponse
                .Result.Should()
                .BeEquivalentTo(
                    new
                    {
                        Id = syllabusId,
                        AgeFrom = updatePayload.AgeFrom,
                        AgeEnd = updatePayload.AgeEnd,
                    }
                );
        }

        [Fact]
        public async Task CreateAsync_ReturnsUnauthorized_WhenUserIsUnauthorized()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.UNAUTHORIZED_MESSAGE))
                .Returns("Unauthorized access.");

            // Simulate a user with no valid claims
            var claimsIdentity = new ClaimsIdentity(); // No claims added
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            var requestPayload = new SyllabusCreateDTO { AgeFrom = 10, AgeEnd = 15 };

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
        public async Task CreateAsync_ReturnsForbidden_WhenUserDoesNotHaveRequiredRole()
        {
            // Arrange
            _mockResourceService
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbidden access.");

            // Simulate a user with the wrong role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, "OtherRole"),
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            var requestPayload = new SyllabusCreateDTO { AgeFrom = 10, AgeEnd = 15 };

            // Act
            var result = await _controller.CreateAsync(requestPayload);
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
        public async Task CreateAsync_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            // Arrange
            var requestPayload = new SyllabusCreateDTO { AgeFrom = 10, AgeEnd = 15 };

            // Simulate a valid user with the correct role
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

            _mockResourceService
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Internal server error");

            // Simulate an exception in the repository method
            _mockSyllabusRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Syllabus>()))
                .ThrowsAsync(new Exception("Database error"));
            _mockSyllabusRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<Syllabus, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((0, new List<Syllabus>()));
            // Act
            var result = await _controller.CreateAsync(requestPayload);
            var internalServerErrorResult = result.Result as ObjectResult;

            // Assert
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
        public async Task CreateAsync_ReturnsBadRequest_WhenAgeRangeAlreadyExists()
        {
            // Arrange
            var requestPayload = new SyllabusCreateDTO { AgeFrom = 10, AgeEnd = 15 };

            // Simulate a valid user with the correct role
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

            var existingSyllabus = new Syllabus
            {
                AgeFrom = 10,
                AgeEnd = 15,
                TutorId = "testUserId",
                IsDeleted = false,
            };

            _mockResourceService
                .Setup(r => r.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.AGE))
                .Returns("Age range already exists");

            _mockSyllabusRepository
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<Syllabus, bool>>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<Syllabus, object>>>(),
                        It.IsAny<bool>()
                    )
                )
                .ReturnsAsync((1, new List<Syllabus> { existingSyllabus }));

            // Act
            var result = await _controller.CreateAsync(requestPayload);
            var badRequestResult = result.Result as ObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            apiResponse.ErrorMessages.Should().Contain("Age range already exists");
        }

        [Fact]
        public async Task CreateAsync_ReturnsCreated_WhenSyllabusIsValid()
        {
            // Arrange
            var requestPayload = new SyllabusCreateDTO
            {
                AgeFrom = 10,
                AgeEnd = 15,
                SyllabusExercises = new List<SyllabusExerciseCreateDTO>
        {
            new SyllabusExerciseCreateDTO
            {
                ExerciseTypeId = 1,
                ExerciseIds = new List<int> { 101, 102 }
            }
        }
            };

            var userId = "testUserId";
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userId),
        new Claim(ClaimTypes.Role, SD.TUTOR_ROLE)
    };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var createdSyllabus = new Syllabus
            {
                Id = 1,
                AgeFrom = 10,
                AgeEnd = 15,
                TutorId = userId,
                SyllabusExercises = new List<SyllabusExercise>()
            };

            var syllabusExercises = new List<SyllabusExercise>
    {
        new SyllabusExercise
        {
            ExerciseId = 101,
            ExerciseTypeId = 1,
            Exercise = new Exercise { Id = 101, ExerciseName = "Exercise 1", Description = "Description 1" },
            ExerciseType = new ExerciseType { Id = 1, ExerciseTypeName = "Type 1" }
        },
        new SyllabusExercise
        {
            ExerciseId = 102,
            ExerciseTypeId = 1,
            Exercise = new Exercise { Id = 102, ExerciseName = "Exercise 2", Description = "Description 2" },
            ExerciseType = new ExerciseType { Id = 1, ExerciseTypeName = "Type 1" }
        }
    };

            _mockSyllabusRepository
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Syllabus, bool>>>(), null, null, null, false))
                .ReturnsAsync((0, new List<Syllabus>()));

            _mockSyllabusRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Syllabus>()))
                .ReturnsAsync(createdSyllabus);

            _mockSyllabusExerciseRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<SyllabusExercise>()))
                .ReturnsAsync((SyllabusExercise)null);

            _mockSyllabusExerciseRepository
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<SyllabusExercise, bool>>>(), "Exercise,ExerciseType", null, null, true))
                .ReturnsAsync((2, syllabusExercises));

            _mockResourceService
                .Setup(r => r.GetString(It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns("Mocked message");

            // Act
            var result = await _controller.CreateAsync(requestPayload);
            var okResult = result.Result as ObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var syllabusDTO = apiResponse.Result as SyllabusDTO;
            syllabusDTO.Should().NotBeNull();
            syllabusDTO.AgeFrom.Should().Be(10);
            syllabusDTO.AgeEnd.Should().Be(15);
            syllabusDTO.ExerciseTypes.Should().NotBeEmpty();
            syllabusDTO.ExerciseTypes.First().Exercises.Count.Should().Be(2);
        }





    }
}
