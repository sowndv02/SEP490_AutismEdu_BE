using Xunit;
using backend_api.Controllers.v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using backend_api.RabbitMQSender;
using backend_api.Repository.IRepository;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using backend_api.Services.IServices;
using backend_api.Mapper;
using backend_api.Models;
using FluentAssertions;
using System.Net;
using Microsoft.Extensions.Logging;

namespace backend_api.Controllers.v1.Tests
{
    public class CurriculumControllerTests
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
        public CurriculumControllerTests()
        {
            _configurationMock.Setup(config => config["APIConfig:PageSize"]).Returns("10");
            _configurationMock.Setup(config => config["RabbitMQSettings:QueueName"]).Returns("TestQueue");
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingConfig());
            });
            _mapper = config.CreateMapper();
            _controller = new CurriculumController(
                _userRepositoryMock.Object,
                _tutorRepositoryMock.Object,
                _mapper,
                _configurationMock.Object,
                _loggerMock.Object,               // Make sure the logger mock is also provided
                _curriculumRepositoryMock.Object,
                _messageBusMock.Object,
                _resourceServiceMock.Object);

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


    }
}