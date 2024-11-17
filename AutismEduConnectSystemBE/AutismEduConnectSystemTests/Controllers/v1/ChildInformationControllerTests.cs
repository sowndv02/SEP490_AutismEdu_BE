using Xunit;
using AutismEduConnectSystem.Controllers.v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutismEduConnectSystem.Repository.IRepository;
using AutoMapper;
using Moq;
using Microsoft.Extensions.Logging;
using AutismEduConnectSystem.Services.IServices;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using AutismEduConnectSystem.Mapper;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using FluentAssertions;
using AutismEduConnectSystem.Models.DTOs;
using System.Linq.Expressions;

namespace AutismEduConnectSystem.Controllers.v1.Tests
{
    public class ChildInformationControllerTests
    {
        private readonly Mock<IChildInformationRepository> _childInfoRepositoryMock;
        private readonly IMapper _mapperMock;
        private readonly Mock<ILogger<ChildInformationController>> _loggerMock;
        private readonly Mock<IStudentProfileRepository> _studentProfileRepositoryMock;
        private readonly Mock<IBlobStorageRepository> _blobStorageRepositoryMock;
        private readonly Mock<IResourceService> _resourceServiceMock;
        private readonly ChildInformationController _controller;

        public ChildInformationControllerTests()
        {
            // Mock dependencies
            _childInfoRepositoryMock = new Mock<IChildInformationRepository>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingConfig());
            });
            _mapperMock = config.CreateMapper();

            _loggerMock = new Mock<ILogger<ChildInformationController>>();
            _studentProfileRepositoryMock = new Mock<IStudentProfileRepository>();
            _blobStorageRepositoryMock = new Mock<IBlobStorageRepository>();
            _resourceServiceMock = new Mock<IResourceService>();

            // Initialize the controller with mocked dependencies
            _controller = new ChildInformationController(
                _childInfoRepositoryMock.Object,
                _mapperMock,
                _loggerMock.Object,
                _studentProfileRepositoryMock.Object,
                _blobStorageRepositoryMock.Object,
                _resourceServiceMock.Object
            );
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, SD.PARENT_ROLE)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };


        }

        [Fact]
        public async Task CreateAsync_ExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange

            var createDTO = new ChildInformationCreateDTO
            {
                Name = "Dao Van Son",
                isMale = true,
                BirthDate = DateTime.Parse("2002/04/04")
            };

            _childInfoRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<ChildInformation>()))
                .ThrowsAsync(new Exception("Lỗi hệ thống. Vui lòng thử lại sau!"));
            _resourceServiceMock
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Lỗi hệ thống. Vui lòng thử lại sau!");


            var result = await _controller.CreateAsync(createDTO);
            var internalServerErrorResult = result.Result as ObjectResult;

            // Assert
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = internalServerErrorResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.First().Should().Be("Lỗi hệ thống. Vui lòng thử lại sau!");
        }

        [Fact]
        public async Task CreateAsync_ReturnsForbiden_WhenUserDoesNotHaveRequiredRole()
        {
            // Arrange
            _resourceServiceMock
                .Setup(r => r.GetString(SD.FORBIDDEN_MESSAGE))
                .Returns("Forbiden access.");

            // Simulate a user with no valid claims (unauthorized)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId")
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            var requestPayload = new ChildInformationCreateDTO
            {
                Name = "Dao Van Son",
                isMale = true,
                BirthDate = DateTime.Parse("2002/04/04")
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


            var requestPayload = new ChildInformationCreateDTO
            {
                Name = "Dao Van Son",
                isMale = true,
                BirthDate = DateTime.Parse("2002/04/04")
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
        public async Task CreateAsync_SuccessfulCreation_ReturnsCreatedResponse()
        {
            // Arrange
            var dto = new ChildInformationCreateDTO
            {
                Name = "Dao Van Son",
                isMale = true,
                BirthDate = DateTime.Parse("2002/04/04")
            };
            var childInfo = new ChildInformation
            {
                Id = 1,
                Name = "Dao Van Son",
                isMale = true,
                ParentId = "testUserId",
                BirthDate = DateTime.Parse("2002/04/04")
            };
            var childInfoDto = new ChildInformationDTO
            {
                Id = 1,
                Name = "Dao Van Son",
                Gender = "Male",
                BirthDate = DateTime.Parse("2002/04/04")
            };


            _childInfoRepositoryMock.Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ChildInformation, bool>>>(), false, null, null))
                .ReturnsAsync((ChildInformation)null);

            _childInfoRepositoryMock.Setup(repo => repo.CreateAsync(It.IsAny<ChildInformation>()))
                .ReturnsAsync(childInfo);

            // Act
            var result = await _controller.CreateAsync(dto);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            apiResponse.Result.Should().NotBeNull();

        }

        [Fact]
        public async Task CreateAsync_DuplicateChild_ReturnsBadRequestResponse()
        {
            // Arrange
            var dto = new ChildInformationCreateDTO { Name = "Test Child" };
            var existingChild = new ChildInformation { Id = 1, Name = "Test Child", ParentId = "test-user-id" };

            _childInfoRepositoryMock.Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ChildInformation, bool>>>(), false, null, null))
                .ReturnsAsync(existingChild);

            _resourceServiceMock
               .Setup(r => r.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.CHILD_NAME))
               .Returns("Tên trẻ đã tồn tại.");
            // Act
            var result = await _controller.CreateAsync(dto);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            apiResponse.ErrorMessages.First().Should().Be("Tên trẻ đã tồn tại.");
        }

        [Fact]
        public async Task GetParentChildInfo_SuccessfulRetrieval_ReturnsOkResponse()
        {
            // Arrange
            var parentId = "test-parent-id";
            var childInfoList = new List<ChildInformation>
            {
                new ChildInformation { Id = 1, Name = "Child 1", ParentId = parentId },
                new ChildInformation { Id = 2, Name = "Child 2", ParentId = parentId }
            };
            var childInfoDTOList = new List<ChildInformationDTO>
            {
                new ChildInformationDTO { Id = 1, Name = "Child 1" },
                new ChildInformationDTO { Id = 2, Name = "Child 2" }
            };

            _childInfoRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(
                    It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                    "Parent", null, null, true))
                .ReturnsAsync((childInfoList, null)); // Mocking method result

            _mapperMock
                .Setup(mapper => mapper.Map<List<ChildInformationDTO>>(childInfoList))
                .Returns(childInfoDTOList);

            // Act
            var result = await _controller.GetParentChildInfo(parentId);

            // Assert
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            apiResponse.Result.Should().BeEquivalentTo(childInfoDTOList);
        }
    }
}