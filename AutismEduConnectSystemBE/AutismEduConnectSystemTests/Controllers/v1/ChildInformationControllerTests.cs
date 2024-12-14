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
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutoMapper;
using Azure;
using FluentAssertions;
using Google.Apis.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

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
                new Claim(ClaimTypes.Role, SD.PARENT_ROLE),
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
                BirthDate = DateTime.Parse("2002/04/04"),
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
            internalServerErrorResult
                .StatusCode.Should()
                .Be((int)HttpStatusCode.InternalServerError);

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
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "testUserId") };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

            var requestPayload = new ChildInformationCreateDTO
            {
                Name = "Dao Van Son",
                isMale = true,
                BirthDate = DateTime.Parse("2002/04/04"),
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
                HttpContext = new DefaultHttpContext { User = claimsPrincipal },
            };

            var requestPayload = new ChildInformationCreateDTO
            {
                Name = "Dao Van Son",
                isMale = true,
                BirthDate = DateTime.Parse("2002/04/04"),
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
                BirthDate = DateTime.Parse("2002/04/04"),
            };
            var childInfo = new ChildInformation
            {
                Id = 1,
                Name = "Dao Van Son",
                isMale = true,
                ParentId = "testUserId",
                BirthDate = DateTime.Parse("2002/04/04"),
            };
            var childInfoDto = new ChildInformationDTO
            {
                Id = 1,
                Name = "Dao Van Son",
                Gender = "Male",
                BirthDate = DateTime.Parse("2002/04/04"),
            };

            _childInfoRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        false,
                        null,
                        null
                    )
                )
                .ReturnsAsync((ChildInformation)null);

            _childInfoRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<ChildInformation>()))
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
            var existingChild = new ChildInformation
            {
                Id = 1,
                Name = "Test Child",
                ParentId = "test-user-id",
            };

            _childInfoRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        false,
                        null,
                        null
                    )
                )
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
        public async Task CreateAsync_ModelStateInValid_ReturnsBadRequestResponse()
        {
            // Arrange
            var dto = new ChildInformationCreateDTO { Name = "Test Child" };
            var existingChild = new ChildInformation
            {
                Id = 1,
                Name = "Test Child",
                ParentId = "test-user-id",
            };

            _childInfoRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        false,
                        null,
                        null
                    )
                )
                .ReturnsAsync((ChildInformation)null);

            _controller.ModelState.AddModelError("Name", "Required");

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
            apiResponse
                .ErrorMessages.Should()
                .Contain(
                    _resourceServiceMock.Object.GetString(SD.BAD_REQUEST_MESSAGE, SD.CHILD_INFO)
                );
        }

        [Fact]
        public async Task GetChildInfo_SuccessfulRetrieval_ReturnsOkResponse()
        {
            // Arrange
            var parentId = "test-parent-id";
            var childInfoList = new List<ChildInformation>
            {
                new ChildInformation
                {
                    Id = 1,
                    Name = "Child 1",
                    ParentId = parentId,
                },
                new ChildInformation
                {
                    Id = 2,
                    Name = "Child 2",
                    ParentId = parentId,
                },
            };
            var childInfoDTOList = new List<ChildInformationDTO>
            {
                new ChildInformationDTO
                {
                    Id = 1,
                    Name = "Child 1",
                    Gender = "Female",
                },
                new ChildInformationDTO
                {
                    Id = 2,
                    Name = "Child 2",
                    Gender = "Female",
                },
            };

            _childInfoRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        "Parent",
                        null,
                        null,
                        true
                    )
                )
                .ReturnsAsync((2, childInfoList)); // Mocking method result

            // Act
            var result = await _controller.GetChildInfo(parentId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetChildInfo_UnauthorizedAccess_ReturnsUnauthorizedResponse()
        {
            // Arrange
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(); // No user claims

            // Act
            var result = await _controller.GetChildInfo("test-parent-id");

            // Assert
            var unauthorizedResult = result.Result as ObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            var apiResponse = unauthorizedResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetChildInfo_ExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            var parentId = "test-parent-id";

            _childInfoRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        "Parent",
                        null,
                        null,
                        true
                    )
                )
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetChildInfo(parentId);

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
        }

        [Fact]
        public async Task GetChildInfo_ParentIdIsEmpty_ReturnsBadRequest()
        {
            // Arrange
            var parentId = "test-parent-id";
            var childInfoList = new List<ChildInformation>
            {
                new ChildInformation
                {
                    Id = 1,
                    Name = "Child 1",
                    ParentId = parentId,
                },
                new ChildInformation
                {
                    Id = 2,
                    Name = "Child 2",
                    ParentId = parentId,
                },
            };
            var childInfoDTOList = new List<ChildInformationDTO>
            {
                new ChildInformationDTO
                {
                    Id = 1,
                    Name = "Child 1",
                    Gender = "Female",
                },
                new ChildInformationDTO
                {
                    Id = 2,
                    Name = "Child 2",
                    Gender = "Female",
                },
            };

            _childInfoRepositoryMock
                .Setup(repo =>
                    repo.GetAllNotPagingAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        "Parent",
                        null,
                        null,
                        true
                    )
                )
                .ReturnsAsync((2, childInfoList)); // Mocking method result

            // Act
            var result = await _controller.GetChildInfo(string.Empty);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateAsync_Unauthorized_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(); // No user claims

            var updateDTO = new ChildInformationUpdateDTO();

            // Act
            var result = await _controller.UpdateAsync(updateDTO);

            // Assert
            var unauthorizedResult = result.Result as ObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateAsync_Forbidden_ReturnsForbidden()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(
                    ClaimTypes.Role,
                    "SomeOtherRole"
                ) // No Parent Role
                ,
            };
            var identity = new ClaimsIdentity(userClaims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext.User = principal;

            var updateDTO = new ChildInformationUpdateDTO();

            // Act
            var result = await _controller.UpdateAsync(updateDTO);

            // Assert
            var forbiddenResult = result.Result as ObjectResult;
            forbiddenResult.Should().NotBeNull();
            forbiddenResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UpdateAsync_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "The Name field is required.");
            var updateDTO = new ChildInformationUpdateDTO();

            // Act
            var result = await _controller.UpdateAsync(updateDTO);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateAsync_ChildNotFound_ReturnsNotFound()
        {
            // Arrange
            var updateDTO = new ChildInformationUpdateDTO { ChildId = 1 };

            _childInfoRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync((ChildInformation)null); // Mock no child found

            // Act
            var result = await _controller.UpdateAsync(updateDTO);

            // Assert
            var notFoundResult = result.Result as ObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateAsync_ChildNameDuplicated_ReturnsBadRequest()
        {
            // Arrange
            var updateDTO = new ChildInformationUpdateDTO
            {
                Name = "ExistingChildName",
                ChildId = 1,
            };

            var existingChild = new ChildInformation
            {
                Name = "ExistingChildName",
                ParentId = "test-user-id",
            };

            _childInfoRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync(
                    new ChildInformation
                    {
                        Id = 1,
                        ParentId = "test-user-id",
                        Name = "OldName",
                    }
                ); // Existing child

            _childInfoRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        false,
                        null,
                        null
                    )
                )
                .ReturnsAsync(existingChild); // Duplicate name

            // Act
            var result = await _controller.UpdateAsync(updateDTO);

            // Assert
            var badRequestResult = result.Result as ObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateAsync_ExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            var updateDTO = new ChildInformationUpdateDTO { ChildId = 1 };

            _childInfoRepositoryMock
                .Setup(repo =>
                    repo.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.UpdateAsync(updateDTO);

            // Assert
            var internalServerErrorResult = result.Result as ObjectResult;
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult
                .StatusCode.Should()
                .Be((int)HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task UpdateAsync_Case1_NameProvided_StudentProfileExists_BirthDateNotProvided_MediaNotProvided()
        {
            // Arrange
            var updateDTO = new ChildInformationUpdateDTO { Name = "John Doe", ChildId = 1 };
            var child = new ChildInformation
            {
                Id = updateDTO.ChildId,
                ParentId = "parent123",
                Name = "Old Name",
            };
            var studentProfile = new StudentProfile { ChildId = child.Id };

            _childInfoRepositoryMock
                .Setup(x =>
                    x.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync(child);
            _studentProfileRepositoryMock
                .Setup(x =>
                    x.GetAsync(It.IsAny<Expression<Func<StudentProfile, bool>>>(), true, null, null)
                )
                .ReturnsAsync(studentProfile);
            // Act
            var result = await _controller.UpdateAsync(updateDTO);

            // Assert
            _studentProfileRepositoryMock.Verify(
                x =>
                    x.UpdateAsync(
                        It.Is<StudentProfile>(sp => sp.StudentCode == "JD" + studentProfile.ChildId)
                    ),
                Times.Once
            );
            _childInfoRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<ChildInformation>()),
                Times.Once
            );

            // Assert
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task UpdateAsync_Case2_NameProvided_StudentProfileExists_BirthDateNotProvided_MediaProvided()
        {
            // Arrange
            var updateDTO = new ChildInformationUpdateDTO
            {
                Name = "John Doe",
                ChildId = 1,
                Media =
                    Mock.Of<IFormFile>() // Mocking the media file
                ,
            };
            var child = new ChildInformation
            {
                Id = updateDTO.ChildId,
                ParentId = "parent123",
                Name = "Old Name",
            };
            var studentProfile = new StudentProfile { ChildId = child.Id };

            // Mocking the repository calls
            _childInfoRepositoryMock
                .Setup(x =>
                    x.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync(child);
            _studentProfileRepositoryMock
                .Setup(x =>
                    x.GetAsync(It.IsAny<Expression<Func<StudentProfile, bool>>>(), true, null, null)
                )
                .ReturnsAsync(studentProfile);

            // Mocking the Blob Storage upload
            _blobStorageRepositoryMock
                .Setup(x => x.Upload(It.IsAny<Stream>(), It.IsAny<string>(), false))
                .ReturnsAsync("http://image.url/test.jpg");

            // Act
            var result = await _controller.UpdateAsync(updateDTO);

            // Assert that media is uploaded
            _blobStorageRepositoryMock.Verify(
                x => x.Upload(It.IsAny<Stream>(), It.IsAny<string>(), false),
                Times.Once
            );

            // Assert the child information is updated with the media URL
            _childInfoRepositoryMock.Verify(
                x =>
                    x.UpdateAsync(
                        It.Is<ChildInformation>(c => c.ImageUrlPath == "http://image.url/test.jpg")
                    ),
                Times.Once
            );

            // Assert the Student Profile is updated with the correct StudentCode
            _studentProfileRepositoryMock.Verify(
                x =>
                    x.UpdateAsync(
                        It.Is<StudentProfile>(sp => sp.StudentCode == "JD" + studentProfile.ChildId)
                    ),
                Times.Once
            );

            // Assert the result is an OK response
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task UpdateAsync_Case4_NameProvided_StudentProfileExists_BirthDateProvided_MediaProvided()
        {
            // Arrange
            var updateDTO = new ChildInformationUpdateDTO
            {
                Name = "John Doe",
                ChildId = 1,
                BirthDate = new DateTime(2010, 5, 1), // Example BirthDate
                Media =
                    Mock.Of<IFormFile>() // Mocking the media file
                ,
            };

            var child = new ChildInformation
            {
                Id = updateDTO.ChildId,
                ParentId = "parent123",
                Name = "Old Name",
                BirthDate = new DateTime(
                    2005,
                    5,
                    5
                ) // Existing BirthDate
                ,
            };

            var studentProfile = new StudentProfile { ChildId = child.Id };

            // Mocking the repository calls
            _childInfoRepositoryMock
                .Setup(x =>
                    x.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync(child);

            _studentProfileRepositoryMock
                .Setup(x =>
                    x.GetAsync(It.IsAny<Expression<Func<StudentProfile, bool>>>(), true, null, null)
                )
                .ReturnsAsync(studentProfile);

            // Mocking the Blob Storage upload
            _blobStorageRepositoryMock
                .Setup(x => x.Upload(It.IsAny<Stream>(), It.IsAny<string>(), false))
                .ReturnsAsync("http://image.url/test.jpg");

            // Act
            var result = await _controller.UpdateAsync(updateDTO);

            // Assert that media is uploaded
            _blobStorageRepositoryMock.Verify(
                x => x.Upload(It.IsAny<Stream>(), It.IsAny<string>(), false),
                Times.Once
            );

            // Assert the child information is updated with the media URL and birthdate
            _childInfoRepositoryMock.Verify(
                x =>
                    x.UpdateAsync(
                        It.Is<ChildInformation>(c =>
                            c.ImageUrlPath == "http://image.url/test.jpg"
                            && c.BirthDate == updateDTO.BirthDate
                        )
                    ),
                Times.Once
            );

            // Assert the Student Profile is updated with the correct StudentCode
            _studentProfileRepositoryMock.Verify(
                x =>
                    x.UpdateAsync(
                        It.Is<StudentProfile>(sp => sp.StudentCode == "JD" + studentProfile.ChildId)
                    ),
                Times.Once
            );

            // Assert the result is an OK response
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task UpdateAsync_Case3_NameProvided_StudentProfileExists_BirthDateProvided_MediaNotProvided()
        {
            // Arrange
            var updateDTO = new ChildInformationUpdateDTO
            {
                Name = "John Doe",
                ChildId = 1,
                BirthDate = new DateTime(
                    2010,
                    5,
                    1
                ) // Example BirthDate
                ,
            };

            var child = new ChildInformation
            {
                Id = updateDTO.ChildId,
                ParentId = "parent123",
                Name = "Old Name",
                BirthDate = new DateTime(
                    2005,
                    5,
                    5
                ) // Existing BirthDate
                ,
            };

            var studentProfile = new StudentProfile { ChildId = child.Id };

            // Mocking the repository calls
            _childInfoRepositoryMock
                .Setup(x =>
                    x.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync(child);

            _studentProfileRepositoryMock
                .Setup(x =>
                    x.GetAsync(It.IsAny<Expression<Func<StudentProfile, bool>>>(), true, null, null)
                )
                .ReturnsAsync(studentProfile);

            // Act
            var result = await _controller.UpdateAsync(updateDTO);

            // Assert that media is not uploaded
            _blobStorageRepositoryMock.Verify(
                x => x.Upload(It.IsAny<Stream>(), It.IsAny<string>(), false),
                Times.Never
            );

            // Assert the child information is updated with the correct birthdate and no media URL change
            _childInfoRepositoryMock.Verify(
                x =>
                    x.UpdateAsync(
                        It.Is<ChildInformation>(c =>
                            c.BirthDate == updateDTO.BirthDate && c.ImageUrlPath == null // No media, so ImageUrlPath should remain null
                        )
                    ),
                Times.Once
            );

            // Assert the Student Profile is updated with the correct StudentCode
            _studentProfileRepositoryMock.Verify(
                x =>
                    x.UpdateAsync(
                        It.Is<StudentProfile>(sp => sp.StudentCode == "JD" + studentProfile.ChildId)
                    ),
                Times.Once
            );

            // Assert the result is an OK response
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task UpdateAsync_Case5_NameProvided_NoStudentProfile_BirthDateNotProvided_MediaNotProvided()
        {
            // Arrange
            var updateDTO = new ChildInformationUpdateDTO { Name = "John Doe", ChildId = 1 };

            var child = new ChildInformation
            {
                Id = updateDTO.ChildId,
                ParentId = "parent123",
                Name = "Old Name",
                BirthDate =
                    null // BirthDate is not provided
                ,
            };

            // No existing student profile
            _childInfoRepositoryMock
                .Setup(x =>
                    x.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync(child);

            _studentProfileRepositoryMock
                .Setup(x =>
                    x.GetAsync(It.IsAny<Expression<Func<StudentProfile, bool>>>(), true, null, null)
                )
                .ReturnsAsync((StudentProfile)null); // No existing student profile

            // Act
            var result = await _controller.UpdateAsync(updateDTO);

            // Assert that the child information is updated
            _childInfoRepositoryMock.Verify(
                x =>
                    x.UpdateAsync(
                        It.Is<ChildInformation>(c =>
                            c.Name == updateDTO.Name && c.BirthDate == null // Name is updated, but BirthDate remains null
                        )
                    ),
                Times.Once
            );

            // Assert that media upload was not triggered
            _blobStorageRepositoryMock.Verify(
                x => x.Upload(It.IsAny<Stream>(), It.IsAny<string>(), false),
                Times.Never
            );

            // Assert the result is an OK response
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task UpdateAsync_Case6_NameProvided_NoStudentProfile_BirthDateNotProvided_MediaProvided()
        {
            // Arrange
            var updateDTO = new ChildInformationUpdateDTO
            {
                Name = "John Doe",
                ChildId = 1,
                Media = new FormFile(
                    Mock.Of<Stream>(),
                    0,
                    100,
                    "file",
                    "filename.jpg"
                ) // Simulating media provided
                ,
            };

            var child = new ChildInformation
            {
                Id = updateDTO.ChildId,
                ParentId = "parent123",
                Name = "Old Name",
                BirthDate =
                    null // BirthDate is not provided
                ,
            };

            // No existing student profile
            _childInfoRepositoryMock
                .Setup(x =>
                    x.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync(child);

            _studentProfileRepositoryMock
                .Setup(x =>
                    x.GetAsync(It.IsAny<Expression<Func<StudentProfile, bool>>>(), true, null, null)
                )
                .ReturnsAsync((StudentProfile)null); // No existing student profile

            _blobStorageRepositoryMock
                .Setup(x => x.Upload(It.IsAny<Stream>(), It.IsAny<string>(), false))
                .ReturnsAsync("http://image.url/test.jpg"); // Mocking the media upload

            // Act
            var result = await _controller.UpdateAsync(updateDTO);

            // Assert that the child information is updated
            _childInfoRepositoryMock.Verify(
                x =>
                    x.UpdateAsync(
                        It.Is<ChildInformation>(c =>
                            c.Name == updateDTO.Name
                            && c.BirthDate == null
                            && c.ImageUrlPath == "http://image.url/test.jpg" // Name is updated, but BirthDate remains null
                        )
                    ),
                Times.Once
            );

            // Assert the result is an OK response
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task UpdateAsync_Case7_NameProvided_NoStudentProfile_BirthDateProvided_MediaProvided()
        {
            // Arrange
            var updateDTO = new ChildInformationUpdateDTO
            {
                Name = "John Doe",
                ChildId = 1,
                BirthDate = new DateTime(2010, 1, 1), // BirthDate is provided
                Media = new FormFile(
                    Mock.Of<Stream>(),
                    0,
                    100,
                    "file",
                    "filename.jpg"
                ) // Simulating media provided
                ,
            };

            var child = new ChildInformation
            {
                Id = updateDTO.ChildId,
                ParentId = "parent123",
                Name = "Old Name",
                BirthDate =
                    null // BirthDate is not provided initially
                ,
            };

            // No existing student profile
            _childInfoRepositoryMock
                .Setup(x =>
                    x.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync(child);

            _studentProfileRepositoryMock
                .Setup(x =>
                    x.GetAsync(It.IsAny<Expression<Func<StudentProfile, bool>>>(), true, null, null)
                )
                .ReturnsAsync((StudentProfile)null); // No existing student profile

            _blobStorageRepositoryMock
                .Setup(x => x.Upload(It.IsAny<Stream>(), It.IsAny<string>(), false))
                .ReturnsAsync("http://image.url/test.jpg"); // Mocking the media upload

            // Act
            var result = await _controller.UpdateAsync(updateDTO);

            // Assert that the child information is updated
            _childInfoRepositoryMock.Verify(
                x =>
                    x.UpdateAsync(
                        It.Is<ChildInformation>(c =>
                            c.Name == updateDTO.Name
                            && c.BirthDate == updateDTO.BirthDate
                            && // BirthDate is updated
                            c.ImageUrlPath == "http://image.url/test.jpg" // Image is uploaded
                        )
                    ),
                Times.Once
            );

            // Assert the result is an OK response
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task UpdateAsync_Case7_NameProvided_NoStudentProfile_BirthDateProvided_NoMedia()
        {
            // Arrange
            var updateDTO = new ChildInformationUpdateDTO
            {
                Name = "John Doe",
                ChildId = 1,
                BirthDate = new DateTime(
                    2010,
                    1,
                    1
                ) // BirthDate is provided
                ,
                // No Media provided
            };

            var child = new ChildInformation
            {
                Id = updateDTO.ChildId,
                ParentId = "parent123",
                Name = "Old Name",
                BirthDate =
                    null // BirthDate is not provided initially
                ,
            };

            // No existing student profile
            _childInfoRepositoryMock
                .Setup(x =>
                    x.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync(child);

            _studentProfileRepositoryMock
                .Setup(x =>
                    x.GetAsync(It.IsAny<Expression<Func<StudentProfile, bool>>>(), true, null, null)
                )
                .ReturnsAsync((StudentProfile)null); // No existing student profile

            // Act
            var result = await _controller.UpdateAsync(updateDTO);

            // Assert that the child information is updated
            _childInfoRepositoryMock.Verify(
                x =>
                    x.UpdateAsync(
                        It.Is<ChildInformation>(c =>
                            c.Name == updateDTO.Name
                            && c.BirthDate == updateDTO.BirthDate
                            && // BirthDate is updated
                            c.ImageUrlPath == null // No image URL, as no media was provided
                        )
                    ),
                Times.Once
            );

            // Assert the result is an OK response
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task UpdateAsync_Case7_NoNameProvided_BirthDateProvided_MediaProvided()
        {
            // Arrange
            var updateDTO = new ChildInformationUpdateDTO
            {
                ChildId = 1,
                BirthDate = new DateTime(2010, 1, 1), // BirthDate is provided
                Media = new FormFile(
                    Mock.Of<Stream>(),
                    0,
                    100,
                    "file",
                    "filename.jpg"
                ) // Simulating media provided
                ,
            };

            var child = new ChildInformation
            {
                Id = updateDTO.ChildId,
                ParentId = "parent123",
                Name = "Old Name", // The name will not be updated, but the existing name is "Old Name"
                BirthDate =
                    null // BirthDate is not provided initially
                ,
            };

            // No existing student profile
            _childInfoRepositoryMock
                .Setup(x =>
                    x.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync(child);

            _studentProfileRepositoryMock
                .Setup(x =>
                    x.GetAsync(It.IsAny<Expression<Func<StudentProfile, bool>>>(), true, null, null)
                )
                .ReturnsAsync((StudentProfile)null); // No existing student profile

            // Mocking the media upload
            _blobStorageRepositoryMock
                .Setup(x => x.Upload(It.IsAny<Stream>(), It.IsAny<string>(), false))
                .ReturnsAsync("http://image.url/test.jpg"); // Media upload URL

            // Act
            var result = await _controller.UpdateAsync(updateDTO);

            // Assert that the child information is updated with new BirthDate and media uploaded
            _childInfoRepositoryMock.Verify(
                x =>
                    x.UpdateAsync(
                        It.Is<ChildInformation>(c =>
                            c.Name == "Old Name"
                            && // Name remains unchanged
                            c.BirthDate == updateDTO.BirthDate
                            && // BirthDate is updated
                            c.ImageUrlPath == "http://image.url/test.jpg" // Media URL is updated
                        )
                    ),
                Times.Once
            );

            // Assert the result is an OK response
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task UpdateAsync_Case8_NoNameProvided_BirthDateProvided_NoMedia()
        {
            // Arrange
            var updateDTO = new ChildInformationUpdateDTO
            {
                ChildId = 1,
                BirthDate = new DateTime(2010, 1, 1), // BirthDate is provided
                Media =
                    null // No media provided
                ,
            };

            var child = new ChildInformation
            {
                Id = updateDTO.ChildId,
                ParentId = "parent123",
                Name = "Old Name", // The name will not be updated, but the existing name is "Old Name"
                BirthDate =
                    null // BirthDate is not provided initially
                ,
            };

            // No existing student profile
            _childInfoRepositoryMock
                .Setup(x =>
                    x.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync(child);

            _studentProfileRepositoryMock
                .Setup(x =>
                    x.GetAsync(It.IsAny<Expression<Func<StudentProfile, bool>>>(), true, null, null)
                )
                .ReturnsAsync((StudentProfile)null); // No existing student profile

            // Act
            var result = await _controller.UpdateAsync(updateDTO);

            // Assert that the child information is updated with new BirthDate, but no media uploaded
            _childInfoRepositoryMock.Verify(
                x =>
                    x.UpdateAsync(
                        It.Is<ChildInformation>(c =>
                            c.Name == "Old Name"
                            && // Name remains unchanged
                            c.BirthDate == updateDTO.BirthDate
                            && // BirthDate is updated
                            c.ImageUrlPath == null // No media URL is set since no media was provided
                        )
                    ),
                Times.Once
            );

            // Assert the result is an OK response
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task UpdateAsync_Case8_NoNameProvided_NoBirthDateProvided_NoMedia()
        {
            // Arrange
            var updateDTO = new ChildInformationUpdateDTO
            {
                ChildId = 1,
                Name = null, // No Name provided
                BirthDate = null, // No BirthDate provided
                Media =
                    null // No media provided
                ,
            };

            var child = new ChildInformation
            {
                Id = updateDTO.ChildId,
                ParentId = "parent123",
                Name = "Old Name", // The name will not be updated, but the existing name is "Old Name"
                BirthDate = new DateTime(2010, 1, 1), // BirthDate is initially provided
                ImageUrlPath =
                    "http://oldimage.url/test.jpg" // Existing media URL
                ,
            };

            // No existing student profile
            _childInfoRepositoryMock
                .Setup(x =>
                    x.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync(child);

            _studentProfileRepositoryMock
                .Setup(x =>
                    x.GetAsync(It.IsAny<Expression<Func<StudentProfile, bool>>>(), true, null, null)
                )
                .ReturnsAsync((StudentProfile)null); // No existing student profile

            // Act
            var result = await _controller.UpdateAsync(updateDTO);

            // Assert that no update is made since no values are provided
            _childInfoRepositoryMock.Verify(
                x =>
                    x.UpdateAsync(
                        It.Is<ChildInformation>(c =>
                            c.Name == "Old Name"
                            && // Name remains unchanged
                            c.BirthDate == child.BirthDate
                            && // BirthDate remains unchanged
                            c.ImageUrlPath == child.ImageUrlPath // Media URL remains unchanged
                        )
                    ),
                Times.Once
            ); // Ensure that UpdateAsync was NOT called

            // Assert the result is an OK response
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task UpdateAsync_Case9_NoNameProvided_NoBirthDateProvided_MediaProvided()
        {
            // Arrange
            var updateDTO = new ChildInformationUpdateDTO
            {
                ChildId = 1,
                Name = null, // No Name provided
                BirthDate = null, // No BirthDate provided
                Media = new FormFile(
                    Mock.Of<Stream>(),
                    0,
                    100,
                    "file",
                    "filename.jpg"
                ) // Simulating media provided
                ,
            };

            var child = new ChildInformation
            {
                Id = updateDTO.ChildId,
                ParentId = "parent123",
                Name = "Old Name", // Existing name will remain unchanged
                BirthDate = new DateTime(2010, 1, 1), // Existing birth date will remain unchanged
                ImageUrlPath =
                    "http://oldimage.url/test.jpg" // Existing media URL
                ,
            };

            // No existing student profile
            _childInfoRepositoryMock
                .Setup(x =>
                    x.GetAsync(
                        It.IsAny<Expression<Func<ChildInformation, bool>>>(),
                        true,
                        null,
                        null
                    )
                )
                .ReturnsAsync(child);

            _studentProfileRepositoryMock
                .Setup(x =>
                    x.GetAsync(It.IsAny<Expression<Func<StudentProfile, bool>>>(), true, null, null)
                )
                .ReturnsAsync((StudentProfile)null); // No existing student profile

            // Mocking the media upload
            _blobStorageRepositoryMock
                .Setup(x => x.Upload(It.IsAny<Stream>(), It.IsAny<string>(), false))
                .ReturnsAsync("http://image.url/test.jpg"); // Media upload URL

            // Act
            var result = await _controller.UpdateAsync(updateDTO);

            // Assert that the child information is updated with the new media URL (no other fields should be updated)
            _childInfoRepositoryMock.Verify(
                x =>
                    x.UpdateAsync(
                        It.Is<ChildInformation>(c =>
                            c.Name == "Old Name"
                            && // Name remains unchanged
                            c.BirthDate == child.BirthDate
                            && // BirthDate remains unchanged
                            c.ImageUrlPath == "http://image.url/test.jpg" // Media URL is updated
                        )
                    ),
                Times.Once
            ); // Ensure that the update is called once

            // Assert the result is an OK response
            var okResult = result.Result as ObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
    }
}
