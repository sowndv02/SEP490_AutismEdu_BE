using Xunit;
using AutismEduConnectSystem.Controllers.v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using AutismEduConnectSystem.RabbitMQSender;
using AutismEduConnectSystem.Repository.IRepository;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.Mapper;
using AutismEduConnectSystem.Models;
using FluentAssertions;
using System.Net;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc.Testing;
using AutismEduConnectSystem.SignalR;
using Microsoft.AspNetCore.SignalR;
using AutismEduConnectSystem;

namespace AutismEduConnectSystemTests.Controllers.v1
{
    public class CurriculumControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {


        private readonly Mock<IUserRepository> _userRepositoryMock = new Mock<IUserRepository>();
        private readonly Mock<ITutorRepository> _tutorRepositoryMock = new Mock<ITutorRepository>();
        private readonly Mock<ICurriculumRepository> _curriculumRepositoryMock = new Mock<ICurriculumRepository>();
        private readonly Mock<IRabbitMQMessageSender> _messageBusMock = new Mock<IRabbitMQMessageSender>();
        private readonly Mock<IResourceService> _resourceServiceMock = new Mock<IResourceService>();
        private readonly Mock<ILogger<CurriculumController>> _loggerMock = new Mock<ILogger<CurriculumController>>();
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
            _configurationMock.Setup(config => config["RabbitMQSettings:QueueName"]).Returns("TestQueue");
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
                _loggerMock.Object,               // Make sure the logger mock is also provided
                _curriculumRepositoryMock.Object,
                _messageBusMock.Object,
                _resourceServiceMock.Object,
                _notificationRepositoryMock.Object,
                _hubContextMock.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                    {
                    new Claim(ClaimTypes.NameIdentifier, "testUserId")
                    }))
                }
            };
        }

        public Mock<IResourceService> ResourceServiceMock => _resourceServiceMock;

        [Fact]
        public async Task DeleteAsync_ReturnsBadRequest_WhenIdIsZero()
        {
            // Arrange
            _resourceServiceMock.Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID)).Returns("Invalid ID.");

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
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), false, null, null))
                .ReturnsAsync((Curriculum)null);
            _resourceServiceMock.Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.CURRICULUM)).Returns("Curriculum not found.");

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
            var curriculum = new Curriculum { Id = 1, SubmitterId = "testUserId", IsActive = true, IsDeleted = false };
            _curriculumRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), false, null, null))
                .ReturnsAsync(curriculum);
            var newCurriculum = new Curriculum { Id = 1, SubmitterId = "testUserId", IsActive = true, IsDeleted = true };
            _curriculumRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Curriculum>())).ReturnsAsync(newCurriculum);

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
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), false, null, null))
                .ThrowsAsync(new Exception());
            _resourceServiceMock.Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE)).Returns("An error occurred.");

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
    }
}