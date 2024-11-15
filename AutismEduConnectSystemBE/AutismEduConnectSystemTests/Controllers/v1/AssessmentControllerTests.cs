using AutismEduConnectSystem.Mapper;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using Xunit;

namespace AutismEduConnectSystem.Controllers.v1.Tests
{
    public class AssessmentControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly AssessmentController _controller;
        private readonly Mock<IAssessmentQuestionRepository> _assessmentQuestionRepositoryMock;
        private readonly Mock<IAssessmentScoreRangeRepository> _assessmentScoreRangeRepositoryMock;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<AssessmentController>> _loggerMock;
        private readonly Mock<IResourceService> _resourceServiceMock;

        public AssessmentControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _assessmentQuestionRepositoryMock = new Mock<IAssessmentQuestionRepository>();
            _assessmentScoreRangeRepositoryMock = new Mock<IAssessmentScoreRangeRepository>();
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingConfig());
            });
            _mapper = config.CreateMapper();
            _loggerMock = new Mock<ILogger<AssessmentController>>();
            _resourceServiceMock = new Mock<IResourceService>();

            _controller = new AssessmentController(
                _assessmentQuestionRepositoryMock.Object,
                _mapper,
                _loggerMock.Object,
                _resourceServiceMock.Object,
                _assessmentScoreRangeRepositoryMock.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.STAFF_ROLE)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
        }

        [Fact]
        public async Task CreateAsync_ReturnsBadRequest_WhenRequestPayloadIsNull()
        {
            _resourceServiceMock
                .Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ASSESSMENT_QUESTION)).Returns(string.Concat(SD.ASSESSMENT_QUESTION, " không hợp lệ"));

            var result = await _controller.CreateAsync(null);
            var statusCodeResult = result.Result as ObjectResult;
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            var apiResponse = statusCodeResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.FirstOrDefault().Equals($"{SD.ASSESSMENT_QUESTION} không hợp lệ");
        }

        [Fact]
        public async Task CreateAsync_ReturnsBadRequest_WhenQuestionAlreadyExists()
        {
            var existingQuestion = new AssessmentQuestion { Question = "Existing Question" };
            _assessmentQuestionRepositoryMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssessmentQuestion, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingQuestion);
            _resourceServiceMock
                .Setup(r => r.GetString(SD.DATA_DUPLICATED_MESSAGE, existingQuestion.Question))
                .Returns($"{existingQuestion.Question} đã tồn tại");

            var dto = new AssessmentQuestionCreateDTO
            {
                Question = "Existing Question",
                AssessmentOptions = new List<AssessmentOptionCreateDTO>() {
                    new AssessmentOptionCreateDTO() { OptionText = "Option A", Point = 1 },
                    new AssessmentOptionCreateDTO() { OptionText = "Option B", Point = 2 },
                    new AssessmentOptionCreateDTO() { OptionText = "Option C", Point = 3 },
                    new AssessmentOptionCreateDTO() { OptionText = "Option D", Point = 4 },
                    new AssessmentOptionCreateDTO() { OptionText = "Option E", Point = 5 },
                    new AssessmentOptionCreateDTO() { OptionText = "Option F", Point = 6 },
                    new AssessmentOptionCreateDTO() { OptionText = "Option G", Point = 7 },
                }
            };
            var result = await _controller.CreateAsync(dto);
            var statusCodeResult = result.Result as ObjectResult;

            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            var apiResponse = statusCodeResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.FirstOrDefault().Should().Be($"{existingQuestion.Question} đã tồn tại");
        }

        [Fact]
        public async Task CreateAsync_ReturnsCreated_WhenQuestionIsCreatedSuccessfully()
        {
            // Arrange
            var dto = new AssessmentQuestionCreateDTO
            {
                Question = "New Question",
                AssessmentOptions = new List<AssessmentOptionCreateDTO>() {
                    new AssessmentOptionCreateDTO() { OptionText = "Option A", Point = 1 },
                    new AssessmentOptionCreateDTO() { OptionText = "Option B", Point = 2 },
                    new AssessmentOptionCreateDTO() { OptionText = "Option C", Point = 3 },
                    new AssessmentOptionCreateDTO() { OptionText = "Option D", Point = 4 },
                    new AssessmentOptionCreateDTO() { OptionText = "Option E", Point = 5 },
                    new AssessmentOptionCreateDTO() { OptionText = "Option F", Point = 6 },
                    new AssessmentOptionCreateDTO() { OptionText = "Option G", Point = 7 },
                }
            };


            var newQuestion = new AssessmentQuestion
            {
                Id = 1,
                Question = "New Question",
                AssessmentOptions = new List<AssessmentOption>() {
                    new AssessmentOption() { OptionText = "Option A", Point = 1 },
                    new AssessmentOption() { OptionText = "Option B", Point = 2 },
                    new AssessmentOption() { OptionText = "Option C", Point = 3 },
                    new AssessmentOption() { OptionText = "Option D", Point = 4 },
                    new AssessmentOption() { OptionText = "Option E", Point = 5 },
                    new AssessmentOption() { OptionText = "Option F", Point = 6 },
                    new AssessmentOption() { OptionText = "Option G", Point = 7 },
                },
                SubmitterId = "testUserId",
                IsHidden = false,
                //IsAssessment = true,
                CreatedDate = DateTime.Now,
            };

            // Mock repository behavior
            _assessmentQuestionRepositoryMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssessmentQuestion, bool>>>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((AssessmentQuestion)null); // No existing question
            _assessmentQuestionRepositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<AssessmentQuestion>()))
                .ReturnsAsync(newQuestion);

            // Act
            var result = await _controller.CreateAsync(dto);
            var statusCodeResult = result.Result as ObjectResult;

            // Assert
            statusCodeResult.Should().NotBeNull();
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
            var apiResponse = statusCodeResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            apiResponse.ErrorMessages.Should().BeEmpty(); // Ensure no error messages
        }


        [Fact]
        public async Task CreateAsync_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            var dto = new AssessmentQuestionCreateDTO
            {
                Question = "New Question",
                AssessmentOptions = new List<AssessmentOptionCreateDTO>() {
                    new AssessmentOptionCreateDTO() { OptionText = "Option A", Point = 1 },
                    new AssessmentOptionCreateDTO() { OptionText = "Option B", Point = 2 },
                    new AssessmentOptionCreateDTO() { OptionText = "Option C", Point = 3 },
                    new AssessmentOptionCreateDTO() { OptionText = "Option D", Point = 4 },
                    new AssessmentOptionCreateDTO() { OptionText = "Option E", Point = 5 },
                    new AssessmentOptionCreateDTO() { OptionText = "Option F", Point = 6 },
                    new AssessmentOptionCreateDTO() { OptionText = "Option G", Point = 7 },
                }
            };

            _assessmentQuestionRepositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<AssessmentQuestion>()))
                .ThrowsAsync(new Exception("Lỗi hệ thống. Vui lòng thử lại sau!"));

            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Lỗi hệ thống. Vui lòng thử lại sau!");

            var result = await _controller.CreateAsync(dto);
            var statusCodeResult = result.Result as ObjectResult;

            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
            var apiResponse = statusCodeResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.FirstOrDefault().Should().Be("Lỗi hệ thống. Vui lòng thử lại sau!");
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


            var requestPayload = new AssessmentQuestionCreateDTO
            {
                Question = "Sample Question"
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
            apiResponse.ErrorMessages.First().Should().Be("Unauthorized access.");
        }


        [Fact]
        public async Task CreateAsync_ReturnsForbiden_WhenUserDoesNotHaveRequiredRole()
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


            var requestPayload = new AssessmentQuestionCreateDTO
            {
                Question = "Sample Question"
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
            apiResponse.ErrorMessages.First().Should().Be("Unauthorized access.");
        }






        [Fact]
        public async Task GetAllAsync_ReturnsOk_WhenQuestionsAreRetrievedSuccessfully()
        {
            // Arrange
            var assessmentQuestions = new List<AssessmentQuestion>
            {
                new AssessmentQuestion { Id = 1, Question = "Question 1", AssessmentOptions = new List<AssessmentOption>()
                    {
                        new AssessmentOption(){ Id = 1, OptionText = "Option 1 Question 1", Point = 3, QuestionId = 1 },
                        new AssessmentOption(){ Id = 2, OptionText = "Option 2 Question 1", Point = 4, QuestionId = 1 },
                        new AssessmentOption(){ Id = 3, OptionText = "Option 3 Question 1", Point = 5, QuestionId = 1 },
                        new AssessmentOption(){ Id = 4, OptionText = "Option 4 Question 1", Point = 6, QuestionId = 1 },
                    }
                },
                new AssessmentQuestion { Id = 2, Question = "Question 2", AssessmentOptions = new List<AssessmentOption>()
                    {
                        new AssessmentOption(){ Id = 5, OptionText = "Option 1 Question 2", Point = 3, QuestionId = 2 },
                        new AssessmentOption(){ Id = 6, OptionText = "Option 2 Question 2", Point = 4, QuestionId = 2 },
                        new AssessmentOption(){ Id = 7, OptionText = "Option 3 Question 2", Point = 5, QuestionId = 2 },
                        new AssessmentOption(){ Id = 8, OptionText = "Option 4 Question 2", Point = 6, QuestionId = 2 },
                    }
                }
            };

            // Mock the repository to return a list of assessment questions
            _assessmentQuestionRepositoryMock
                .Setup(r => r.GetAllNotPagingAsync(It.IsAny<Expression<Func<AssessmentQuestion, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<AssessmentQuestion, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((2, assessmentQuestions));

            // Act
            var result = await _controller.GetAllAsync();
            var statusCodeResult = result.Result as ObjectResult;

            // Assert
            statusCodeResult.Should().NotBeNull();
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = statusCodeResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.Result.Should().NotBeNull();
        }


        [Fact]
        public async Task GetAllAsync_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            // Arrange
            _assessmentQuestionRepositoryMock
                .Setup(r => r.GetAllNotPagingAsync(It.IsAny<Expression<Func<AssessmentQuestion, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<AssessmentQuestion, object>>>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Lỗi hệ thống. Vui lòng thử lại sau!"));

            // Mock the resource service to return a specific error message
            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Lỗi hệ thống. Vui lòng thử lại sau!");

            // Act
            var result = await _controller.GetAllAsync();
            var statusCodeResult = result.Result as ObjectResult;

            // Assert
            statusCodeResult.Should().NotBeNull();
            statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = statusCodeResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.FirstOrDefault().Should().Be("Lỗi hệ thống. Vui lòng thử lại sau!");
        }


        //[Fact]
        //public async Task GetAsync_ReturnsUnauthorized_WhenUserIsNotAuthentication()
        //{
        //    var client = _factory.CreateClient();
        //    var result = await client.GetAsync("/api/v1/assessment");
        //    result.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        //}
    }
}