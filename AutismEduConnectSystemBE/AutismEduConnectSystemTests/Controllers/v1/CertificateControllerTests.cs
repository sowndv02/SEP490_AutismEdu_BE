using AutismEduConnectSystem.Mapper;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Models.DTOs.UpdateDTOs;
using AutismEduConnectSystem.RabbitMQSender;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.SignalR;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using Xunit;
using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.Controllers.v1.Tests
{
    public class CertificateControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {

        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ICertificateRepository> _certificateRepositoryMock;
        private readonly Mock<ICertificateMediaRepository> _certificateMediaRepositoryMock;
        private readonly Mock<ICurriculumRepository> _curriculumRepositoryMock;
        private readonly Mock<IWorkExperienceRepository> _workExperienceRepositoryMock;
        private readonly Mock<IBlobStorageRepository> _blobStorageRepositoryMock;
        private readonly Mock<ILogger<CertificateController>> _loggerMock;
        private readonly IMapper _mapper;
        private readonly Mock<IRabbitMQMessageSender> _messageBusMock;
        private readonly Mock<IResourceService> _resourceServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<INotificationRepository> _notificationRepositoryMock;
        private readonly Mock<IHubContext<NotificationHub>> _hubContextMock;

        private CertificateController _controller;
        private readonly WebApplicationFactory<Program> _factory;

        public CertificateControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _userRepositoryMock = new Mock<IUserRepository>();
            _certificateRepositoryMock = new Mock<ICertificateRepository>();
            _certificateMediaRepositoryMock = new Mock<ICertificateMediaRepository>();
            _curriculumRepositoryMock = new Mock<ICurriculumRepository>();
            _workExperienceRepositoryMock = new Mock<IWorkExperienceRepository>();
            _blobStorageRepositoryMock = new Mock<IBlobStorageRepository>();
            _notificationRepositoryMock = new Mock<INotificationRepository>();
            _loggerMock = new Mock<ILogger<CertificateController>>();
            _hubContextMock = new Mock<IHubContext<NotificationHub>>();
            _messageBusMock = new Mock<IRabbitMQMessageSender>();
            _resourceServiceMock = new Mock<IResourceService>();
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(c => c["APIConfig:PageSize"]).Returns("10");
            _configurationMock.Setup(c => c["RabbitMQSettings:QueueName"]).Returns("TestQueue");
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingConfig());
            });
            _mapper = config.CreateMapper();
            _controller = new CertificateController(
               _userRepositoryMock.Object,
               _certificateRepositoryMock.Object,
               _loggerMock.Object,
               _blobStorageRepositoryMock.Object,
               _mapper,
               _configurationMock.Object,
               _certificateMediaRepositoryMock.Object,
               _curriculumRepositoryMock.Object,
               _workExperienceRepositoryMock.Object,
               _messageBusMock.Object,
               _resourceServiceMock.Object,
               _hubContextMock.Object,
               _notificationRepositoryMock.Object
           );
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, STAFF_ROLE)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
        }


        //[Fact]
        //public async Task CreateAsync_ReturnsUnauthorized_WhenUserIsNotInStaffRole()
        //{
        //    var client = _factory.CreateClient();
        //    var dto = new CertificateCreateDTO { CertificateName = "FPT University", IssuingInstitution = "FPTU", IssuingDate = DateTime.Now, Medias = new List<IFormFile>() };
        //    var content = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
        //    var result = await client.PostAsync("/api/v1/certificate", content);
        //    result.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        //}

        [Fact]
        public async Task CreateAsync_ReturnsCreated_WhenValidRequest()
        {
            // Arrange
            var userId = "testUserId";
            var certificateId = 1;
            var createDTO = new CertificateCreateDTO
            {
                Medias = new List<IFormFile> { Mock.Of<IFormFile>() }
            };

            var newCertificate = new Certificate
            {
                Id = certificateId,
                SubmitterId = userId
            };

            var certificateDTO = new CertificateDTO
            {
                Id = certificateId
            };

            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, TUTOR_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));

            // Mocking required dependencies
            _certificateRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Certificate>())).ReturnsAsync(newCertificate);
            _blobStorageRepositoryMock.Setup(b => b.Upload(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync("http://blobstorage.url/path/to/media");
            _certificateMediaRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<CertificateMedia>()));

            // Setting up the User property in the ControllerContext
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Setting up ModelState as valid
            _controller.ModelState.Clear();

            // Act
            var result = await _controller.CreateAsync(createDTO);
            var okResult = result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.Result.Should().NotBeNull();

            // Verify that media files were uploaded and saved
            _blobStorageRepositoryMock.Verify(b => b.Upload(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
            _certificateMediaRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<CertificateMedia>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ReturnsBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            var createDTO = new CertificateCreateDTO();
            _controller.ModelState.AddModelError("From", "Required");
            _resourceServiceMock.Setup(r => r.GetString(BAD_REQUEST_MESSAGE, CERTIFICATE))
                    .Returns("Your expected error message here");
            // Act
            var result = await _controller.CreateAsync(createDTO);
            var badRequestResult = result as BadRequestObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.First().Should().Be("Your expected error message here");

        }




        [Fact]
        public async Task CreateAsync_ReturnsInternalServerError_WhenExceptionThrown()
        {
            // Arrange
            var userId = "testUserId";
            var createDTO = new CertificateCreateDTO
            {
                Medias = new List<IFormFile> { Mock.Of<IFormFile>() }
            };

            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, TUTOR_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));

            _certificateRepositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<Certificate>()))
                .ThrowsAsync(new Exception("Lỗi hệ thống. Vui lòng thử lại sau!"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            _resourceServiceMock
                .Setup(r => r.GetString(INTERNAL_SERVER_ERROR_MESSAGE))
                .Returns("Lỗi hệ thống. Vui lòng thử lại sau!");

            // Act
            var result = await _controller.CreateAsync(createDTO);
            var internalServerErrorResult = result as ObjectResult;

            // Assert
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = internalServerErrorResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.First().Should().Be("Lỗi hệ thống. Vui lòng thử lại sau!");
        }


        [Fact]
        public async Task UpdateStatusRequest_ApproveStatus_Succeeds()
        {
            // Arrange
            var certificateId = 1;
            var userId = "user123";
            var certificate = new Certificate { Id = certificateId, SubmitterId = userId, RequestStatus = Status.PENDING, CertificateName = "Test Certificate" };
            var tutor = new ApplicationUser { Id = userId, FullName = "Tutor Name", Email = "tutor@example.com" };
            var changeStatusDTO = new ChangeStatusDTO { Id = certificateId, StatusChange = (int)Status.APPROVE };

            _certificateRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Certificate, bool>>>(), false, "CertificateMedias", null))
                .ReturnsAsync(certificate);
            _userRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(tutor);
            _resourceServiceMock.Setup(r => r.GetString(It.IsAny<string>(), It.IsAny<string>())).Returns("Error message");
            // Set user identity
            var userClaims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Role, STAFF_ROLE) };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));

            // Act
            var result = await _controller.UpdateStatusRequest(certificateId, changeStatusDTO);
            var okResult = result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.Result.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateStatusRequest_RejectStatus_Succeeds()
        {
            // Arrange
            var certificateId = 1;
            var userId = "user123";
            var certificate = new Certificate { Id = certificateId, SubmitterId = userId, RequestStatus = Status.PENDING, CertificateName = "Test Certificate" };
            var tutor = new ApplicationUser { Id = userId, FullName = "Tutor Name", Email = "tutor@example.com" };
            var changeStatusDTO = new ChangeStatusDTO { Id = certificateId, StatusChange = (int)Status.REJECT, RejectionReason = "Invalid document" };

            _certificateRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Certificate, bool>>>(), false, "CertificateMedias", null))
                .ReturnsAsync(certificate);
            _userRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), false, null))
                .ReturnsAsync(tutor);
            // Set user identity
            var userClaims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Role, STAFF_ROLE) };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));

            // Act
            var result = await _controller.UpdateStatusRequest(certificateId, changeStatusDTO);
            var okResult = result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var apiResponse = okResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.Result.Should().NotBeNull();
        }


        [Fact]
        public async Task UpdateStatusRequest_ReturnsBadRequest_IfCertificateIsNull()
        {
            // Arrange
            var certificateId = 1;
            var changeStatusDTO = new ChangeStatusDTO { Id = 1, StatusChange = (int)Status.APPROVE };

            _certificateRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Certificate, bool>>>(), false, "CertificateMedias", null))
                .ReturnsAsync((Certificate)null);
            _resourceServiceMock.Setup(r => r.GetString(It.IsAny<string>(), It.IsAny<string>())).Returns("Error message");

            // Act
            var result = await _controller.UpdateStatusRequest(certificateId, changeStatusDTO);
            var badRequestResult = result as BadRequestObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Error message");
        }


        [Fact]
        public async Task UpdateStatusRequest_ReturnsBadRequest_IfCertificateStatusIsNotPending()
        {
            // Arrange
            var certificateId = 1;
            var certificate = new Certificate { Id = 1, RequestStatus = Status.APPROVE };
            var changeStatusDTO = new ChangeStatusDTO { Id = 1, StatusChange = (int)Status.REJECT };

            _certificateRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Certificate, bool>>>(), false, "CertificateMedias", null))
                .ReturnsAsync(certificate);
            _resourceServiceMock.Setup(r => r.GetString(It.IsAny<string>(), It.IsAny<string>())).Returns("Error message");

            // Act
            var result = await _controller.UpdateStatusRequest(certificateId, changeStatusDTO);
            var badRequestResult = result as BadRequestObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Error message");
        }

        [Fact]
        public async Task UpdateStatusRequest_ReturnsInternalServerError_OnException()
        {
            // Arrange
            var certificateId = 1;
            var changeStatusDTO = new ChangeStatusDTO { Id = 1, StatusChange = (int)Status.APPROVE };
            _resourceServiceMock.Setup(r => r.GetString(INTERNAL_SERVER_ERROR_MESSAGE)).Returns("Internal server error");
            _certificateRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Certificate, bool>>>(), false, "CertificateMedias", null))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateStatusRequest(certificateId, changeStatusDTO);
            var internalServerErrorResult = result as ObjectResult;

            // Assert
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            var apiResponse = internalServerErrorResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Internal server error");
        }

        private void SetUpUser(string userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }


        [Fact]
        public async Task DeleteAsync_ReturnsNoContent_WhenCertificateIsDeletedSuccessfully()
        {
            // Arrange
            int certificateId = 1;
            string userId = "test-user-id";
            SetUpUser(userId);

            var certificate = new Certificate { Id = certificateId, SubmitterId = userId, IsDeleted = false };
            var newCertificate = new Certificate { Id = certificateId, SubmitterId = userId, IsDeleted = true };
            _certificateRepositoryMock.Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<Certificate, bool>>>(), false, null, null))
                .ReturnsAsync(certificate);
            _certificateRepositoryMock.Setup(repo => repo.UpdateAsync(certificate)).ReturnsAsync(newCertificate);

            // Act
            var result = await _controller.DeleteAsync(certificateId);
            var noContentResult = result.Result as ObjectResult;

            // Assert
            noContentResult.Should().NotBeNull();
            noContentResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
            var apiResponse = noContentResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }


        [Fact]
        public async Task DeleteAsync_ReturnsBadRequest_WhenCertificateNotFound()
        {
            // Arrange
            int certificateId = 1;
            string userId = "test-user-id";
            SetUpUser(userId);

            _certificateRepositoryMock.Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<Certificate, bool>>>(), false, null, null))
                .ReturnsAsync((Certificate)null);
            _resourceServiceMock.Setup(r => r.GetString(NOT_FOUND_MESSAGE, CERTIFICATE)).Returns("Certificate not found.");
            // Act
            var result = await _controller.DeleteAsync(certificateId);
            var badRequestResult = result.Result as ObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.First().Should().Contain("Certificate not found."); // Adjust this to match your actual error message
        }

        [Fact]
        public async Task DeleteAsync_ReturnsInternalServerError_OnException()
        {
            // Arrange
            int certificateId = 1;
            string userId = "test-user-id";
            SetUpUser(userId);

            _certificateRepositoryMock.Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<Certificate, bool>>>(), false, null, null))
                .ThrowsAsync(new Exception("Database error"));
            _resourceServiceMock.Setup(r => r.GetString(INTERNAL_SERVER_ERROR_MESSAGE)).Returns("Internal server error");

            // Act
            var result = await _controller.DeleteAsync(certificateId);
            var internalServerErrorResult = result.Result as ObjectResult;

            // Assert
            internalServerErrorResult.Should().NotBeNull();
            internalServerErrorResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
            var apiResponse = internalServerErrorResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Internal server error"); // Adjust this to match your actual error message
        }

        [Fact]
        public async Task DeleteAsync_ReturnsBadRequest_WhenIdIsZero()
        {
            // Arrange
            _resourceServiceMock.Setup(r => r.GetString(BAD_REQUEST_MESSAGE, ID)).Returns("Invalid ID.");

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
        public async Task GetAllAsync_ShouldReturnFilteredAndSortedResults_WhenUserIsTutorWithSearchOrderDESCAndStatusIsPending()
        {
            // Arrange

            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, TUTOR_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Sample Certificate",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.Now,
                    Submitter = new Tutor() {TutorId = "testUserId"},
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (1, certificates);

            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<Certificate, bool>>>(),
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    true)) // Sort descending
                .ReturnsAsync(pagedResult);

            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("Sample", STATUS_PENDING, CREATED_DATE, ORDER_DESC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First()) // Apply filter to check it includes only "testUserId" and status "Pending"
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                true), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnFilteredAndSortedResults_WhenUserIsTutorWithSearchOrderDESCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, TUTOR_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Sample Certificate Reject",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    Submitter = new Tutor() {TutorId = "testUserId"},
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (1, certificates);

            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<Certificate, bool>>>(),
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    true)) // Sort descending
                .ReturnsAsync(pagedResult);

            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("Sample", STATUS_REJECT, CREATED_DATE, ORDER_DESC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First()) // Apply filter to check it includes only "testUserId" and status "Reject"
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                true), Times.Once);
        }


        [Fact]
        public async Task GetAllAsync_ShouldReturnFilteredAndSortedResults_WhenUserIsTutorWithSearchOrderDESCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, TUTOR_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Sample Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.Now.AddDays(-2),
                    Submitter = new Tutor() { TutorId = "testUserId" },
                    CertificateMedias = new List<CertificateMedia>() // Initialize as empty list
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Sample Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    Submitter = new Tutor() { TutorId = "testUserId" },
                    CertificateMedias = new List<CertificateMedia>() // Initialize as empty list
                }
            };

            var pagedResult = (2, certificates); // Assuming 2 items for the test

            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<Certificate, bool>>>(),
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    true)) // Sort descending
                .ReturnsAsync(pagedResult);

            _userRepositoryMock
                .Setup(repo => repo.GetAsync(
                    It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(
                    It.IsAny<Expression<Func<Curriculum, bool>>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Expression<Func<Curriculum, object>>>(),
                    It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(
                    It.IsAny<Expression<Func<WorkExperience, bool>>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Expression<Func<WorkExperience, object>>>(),
                    It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("Sample", STATUS_ALL, CREATED_DATE, ORDER_DESC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();
            response.Pagination.Total.Should().Be(2);

            // Verify that the repository was called with the correct parameters
            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    // Since status is "All", we expect the filter to only include IsDeleted = false and match the search term
                    filter.Compile().Invoke(certificates[0]) &&
                    filter.Compile().Invoke(certificates[1])
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                true), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnFilteredAndSortedResults_WhenUserIsTutorWithSearchOrderDESCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, TUTOR_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 4,
                    SubmitterId = "testUserId",
                    CertificateName = "Approved Certificate",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.Now.AddDays(-3),
                    Submitter = new Tutor() { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (1, certificates);

            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<Certificate, bool>>>(),
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    true)) // Sort descending
                .ReturnsAsync(pagedResult);

            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("Approved", STATUS_APPROVE, CREATED_DATE, ORDER_DESC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First()) // Apply filter to check it includes only "testUserId" and status "Approve"
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                true), Times.Once);
        }


        [Fact]
        public async Task GetAllAsync_ShouldReturnFilteredAndSortedResults_WhenUserIsTutorWithSearchOrderASCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, TUTOR_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 5,
                    SubmitterId = "testUserId",
                    CertificateName = "Rejected Certificate",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.Now.AddDays(-5),
                    Submitter = new Tutor() { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (1, certificates);

            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<Certificate, bool>>>(),
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    false)) // Sort ascending
                .ReturnsAsync(pagedResult);

            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("Rejected", STATUS_REJECT, CREATED_DATE, ORDER_ASC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First()) // Apply filter to check it includes only "testUserId" and status "Reject"
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                false), Times.Once); // Ensure sorting is ascending
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnFilteredAndSortedResults_WhenUserIsTutorWithSearchOrderASCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, TUTOR_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 6,
                    SubmitterId = "testUserId",
                    CertificateName = "Approved Certificate",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.Now.AddDays(-7),
                    Submitter = new Tutor() { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (1, certificates);

            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<Certificate, bool>>>(),
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    false)) // Sort ascending
                .ReturnsAsync(pagedResult);

            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("Approved", STATUS_APPROVE, CREATED_DATE, ORDER_ASC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First()) // Apply filter to check it includes only "testUserId" and status "Approve"
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                false), Times.Once); // Ensure sorting is ascending
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnFilteredAndSortedResults_WhenUserIsTutorWithSearchOrderASCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, TUTOR_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 7,
                    SubmitterId = "testUserId",
                    CertificateName = "Pending Certificate",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.Now.AddDays(-5),
                    Submitter = new Tutor() { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (1, certificates);

            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<Certificate, bool>>>(),
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    false)) // Sort ascending
                .ReturnsAsync(pagedResult);

            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("Pending", STATUS_PENDING, CREATED_DATE, ORDER_ASC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First()) // Apply filter to check it includes only "testUserId" and status "Pending"
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                false), Times.Once); // Ensure sorting is ascending
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnFilteredAndSortedResults_WhenUserIsTutorWithSearchOrderASCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, TUTOR_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "All Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.Now.AddDays(-10),
                    Submitter = new Tutor() { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "All Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.Now.AddDays(-5),
                    Submitter = new Tutor() { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (2, certificates);

            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<Certificate, bool>>>(),
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    false)) // Sort ascending
                .ReturnsAsync(pagedResult);

            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("All", STATUS_ALL, CREATED_DATE, ORDER_ASC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First()) // Applies the filter to check it includes "testUserId" and no specific status
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                false), Times.Once); // Ensure sorting is ascending
        }


        [Fact]
        public async Task GetAllAsync_ShouldReturnFilteredAndSortedResults_WhenUserIsTutorWithNoSearchOrderDESCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, TUTOR_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Pending Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Pending Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.Now.AddDays(-2),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (2, certificates);

            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<Certificate, bool>>>(),
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    true)) // Sort descending
                .ReturnsAsync(pagedResult);

            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync(null, STATUS_PENDING, CREATED_DATE, ORDER_DESC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First())), // Filter should include only certificates with "testUserId" and status "Pending"
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                true), Times.Once); // Ensure sorting is descending
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedResults_WhenUserIsTutorWithNoSearchOrderDESCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, TUTOR_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Approved Certificate",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Pending Certificate",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.Now.AddDays(-2),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 3,
                    SubmitterId = "testUserId",
                    CertificateName = "Rejected Certificate",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.Now.AddDays(-3),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<Certificate, bool>>>(),
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    true)) // Sort descending
                .ReturnsAsync(pagedResult);

            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync(null, STATUS_ALL, CREATED_DATE, ORDER_DESC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter => filter == null || filter.Compile().Invoke(certificates.First())), // Ensures no specific filtering is applied
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                true), Times.Once); // Ensure sorting is descending
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedResults_WhenUserIsTutorWithNoSearchOrderDESCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, TUTOR_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Rejected Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Rejected Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.Now.AddDays(-2),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.Is<Expression<Func<Certificate, bool>>>(filter =>
                        filter.Compile().Invoke(certificates.First())), // Filter for REJECT status
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    true)) // Sort descending
                .ReturnsAsync(pagedResult);

            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync(null, STATUS_REJECT, CREATED_DATE, ORDER_DESC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First())), // Filter checks for REJECT status only
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                true), Times.Once); // Ensure sorting is descending
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedResults_WhenUserIsTutorWithNoSearchOrderDESCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, TUTOR_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Approved Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Approved Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.Now.AddDays(-2),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.Is<Expression<Func<Certificate, bool>>>(filter =>
                        filter.Compile().Invoke(certificates.First())), // Filter for APPROVE status
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    true)) // Sort descending
                .ReturnsAsync(pagedResult);

            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync(null, STATUS_APPROVE, CREATED_DATE, ORDER_DESC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First())), // Filter checks for APPROVE status only
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                true), Times.Once); // Ensure sorting is descending
        }


        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedResults_WhenUserIsTutorWithNoSearchOrderASCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, TUTOR_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Rejected Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.Now.AddDays(1), // Newer certificate
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Rejected Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.Now.AddDays(2), // Older certificate
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.Is<Expression<Func<Certificate, bool>>>(filter =>
                        filter.Compile().Invoke(certificates.First())), // Filter for REJECT status
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    false)) // Sort ascending (CreatedDate)
                .ReturnsAsync(pagedResult);

            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync(null, STATUS_REJECT, CREATED_DATE, ORDER_ASC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First())), // Filter checks for REJECT status only
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                false), Times.Once); // Ensure sorting is ascending (CreatedDate)
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedResults_WhenUserIsTutorWithNoSearchOrderASCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, TUTOR_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Pending Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.Now.AddDays(1), // Newer certificate
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Pending Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.Now.AddDays(2), // Older certificate
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.Is<Expression<Func<Certificate, bool>>>(filter =>
                        filter.Compile().Invoke(certificates.First())), // Filter for PENDING status
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    false)) // Sort ascending (CreatedDate)
                .ReturnsAsync(pagedResult);

            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync(null, STATUS_PENDING, CREATED_DATE, ORDER_ASC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First())), // Filter checks for PENDING status only
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                false), Times.Once); // Ensure sorting is ascending (CreatedDate)
        }


        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSortedResults_WhenUserIsTutorWithNoSearchOrderASCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, TUTOR_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.Now.AddDays(1), // Newer certificate
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.Now.AddDays(2), // Older certificate
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.Is<Expression<Func<Certificate, bool>>>(filter =>
                        filter.Compile().Invoke(certificates.First())), // No specific status filter for "All"
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    false)) // Sort ascending (CreatedDate)
                .ReturnsAsync(pagedResult);

            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync(null, STATUS_ALL, CREATED_DATE, ORDER_ASC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First())), // No filter, for all statuses
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                false), Times.Once); // Ensure sorting is ascending (CreatedDate)
        }


        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedResults_WhenUserIsTutorWithNoSearchOrderASCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Role, TUTOR_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Approved Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.Now.AddDays(1), // Newer approved certificate
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Approved Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.Now.AddDays(2), // Older approved certificate
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.Is<Expression<Func<Certificate, bool>>>(filter =>
                        filter.Compile().Invoke(certificates.First())), // Filter by APPROVE status
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    false)) // Sort ascending (CreatedDate)
                .ReturnsAsync(pagedResult);

            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync(null, STATUS_APPROVE, CREATED_DATE, ORDER_ASC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First()) && filter.Compile().Invoke(certificates.Last()) // Ensure both certificates are approved
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                false), Times.Once); // Ensure sorting is ascending (CreatedDate)
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedSortedResults_WhenUserIsStaffWithSearchOrderASCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(ClaimTypes.Role, STAFF_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Sample data
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Rejected Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.Now.AddDays(2), // Older rejected certificate
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Rejected Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.Now.AddDays(1), // Newer rejected certificate
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            // Mock repository to return the filtered and sorted data
            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.Is<Expression<Func<Certificate, bool>>>(filter =>
                        filter.Compile().Invoke(certificates.First()) && filter.Compile().Invoke(certificates.Last()) // Filter by REJECT status
                    ),
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    false)) // Sort ascending (CreatedDate)
                .ReturnsAsync(pagedResult);

            // Mock user repository to return a staff user
            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("Rejected", STATUS_REJECT, CREATED_DATE, ORDER_ASC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First()) && filter.Compile().Invoke(certificates.Last()) // Ensure both certificates are rejected
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                false), Times.Once); // Ensure sorting is ascending (CreatedDate)
        }


        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingSortedResults_WhenUserIsStaffWithSearchOrderASCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(ClaimTypes.Role, STAFF_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Sample data
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Pending Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.Now.AddDays(2), // Older pending certificate
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Pending Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.Now.AddDays(1), // Newer pending certificate
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            // Mock repository to return the filtered and sorted data
            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.Is<Expression<Func<Certificate, bool>>>(filter =>
                        filter.Compile().Invoke(certificates.First()) && filter.Compile().Invoke(certificates.Last()) // Filter by PENDING status
                    ),
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    false)) // Sort ascending (CreatedDate)
                .ReturnsAsync(pagedResult);

            // Mock user repository to return a staff user
            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("Pending", STATUS_PENDING, CREATED_DATE, ORDER_ASC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First()) && filter.Compile().Invoke(certificates.Last()) // Ensure both certificates are pending
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                false), Times.Once); // Ensure sorting is ascending (CreatedDate)
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedSortedResults_WhenUserIsStaffWithSearchOrderASCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(ClaimTypes.Role, STAFF_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Sample data
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Approved Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.Now.AddDays(2), // Older approved certificate
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Approved Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.Now.AddDays(1), // Newer approved certificate
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            // Mock repository to return the filtered and sorted data
            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.Is<Expression<Func<Certificate, bool>>>(filter =>
                        filter.Compile().Invoke(certificates.First()) && filter.Compile().Invoke(certificates.Last()) // Filter by APPROVE status
                    ),
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    false)) // Sort ascending (CreatedDate)
                .ReturnsAsync(pagedResult);

            // Mock user repository to return a staff user
            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("Approved", STATUS_APPROVE, CREATED_DATE, ORDER_ASC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First()) && filter.Compile().Invoke(certificates.Last()) // Ensure both certificates are approved
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                false), Times.Once); // Ensure sorting is ascending (CreatedDate)
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnSortedResults_WhenUserIsStaffWithSearchOrderASCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(ClaimTypes.Role, STAFF_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Sample data for All status (no filter on status)
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Approved Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Approved status
                    CreatedDate = DateTime.Now.AddDays(2),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Pending Certificate",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING, // Pending status
                    CreatedDate = DateTime.Now.AddDays(1),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            // Mock repository to return the data for 'All' status (no filtering on status)
            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<Certificate, bool>>>(), // No status filter
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    false)) // Sort ascending (CreatedDate)
                .ReturnsAsync(pagedResult);

            // Mock user repository to return a staff user
            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("Certificate", STATUS_ALL, CREATED_DATE, ORDER_ASC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.IsAny<Expression<Func<Certificate, bool>>>(), // Ensure no status filter
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                false), Times.Once); // Ensure sorting is ascending (CreatedDate)
        }
        [Fact]
        public async Task GetAllAsync_ShouldReturnFilteredAndSortedResults_WhenUserIsStaffWithSearchOrderDESCAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(ClaimTypes.Role, STAFF_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Sample data for PENDING status
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Pending Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING, // Pending status
                    CreatedDate = DateTime.Now.AddDays(-1),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Pending Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING, // Pending status
                    CreatedDate = DateTime.Now.AddDays(-2),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            // Mock repository to return the data for 'PENDING' status
            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.Is<Expression<Func<Certificate, bool>>>(filter =>
                        filter.Compile().Invoke(certificates.First()) &&
                        filter.Compile().Invoke(certificates.Last()) // Ensure filtering by PENDING status
                    ),
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    true)) // Sort descending (CreatedDate)
                .ReturnsAsync(pagedResult);

            // Mock user repository to return a staff user
            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("Pending", STATUS_PENDING, CREATED_DATE, ORDER_DESC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First()) &&
                    filter.Compile().Invoke(certificates.Last()) // Ensure filtering by PENDING status
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                true), Times.Once); // Ensure sorting is descending (CreatedDate)
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnFilteredAndSortedResults_WhenUserIsStaffWithSearchOrderDESCAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(ClaimTypes.Role, STAFF_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Sample data for ALL status (no filtering on status)
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING, // Any status
                    CreatedDate = DateTime.Now.AddDays(-1),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // Any status
                    CreatedDate = DateTime.Now.AddDays(-2),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 3,
                    SubmitterId = "testUserId",
                    CertificateName = "Certificate 3",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT, // Any status
                    CreatedDate = DateTime.Now.AddDays(-3),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            // Mock repository to return the data for ALL status
            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<Certificate, bool>>>(), // No filter on status
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    true)) // Sort descending (CreatedDate)
                .ReturnsAsync(pagedResult);

            // Mock user repository to return a staff user
            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("Certificate", STATUS_ALL, CREATED_DATE, ORDER_DESC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.IsAny<Expression<Func<Certificate, bool>>>(), // No filter on status (All)
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                true), Times.Once); // Ensure sorting is descending (CreatedDate)
        }
        [Fact]
        public async Task GetAllAsync_ShouldReturnFilteredAndSortedResults_WhenUserIsStaffWithSearchOrderDESCAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(ClaimTypes.Role, STAFF_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Sample data for REJECT status
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Rejected Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Rejected Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.Now.AddDays(-2),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            // Mock repository to return the data for REJECT status
            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.Is<Expression<Func<Certificate, bool>>>(filter =>
                        filter.Compile().Invoke(certificates.First()) // Ensures filter applies status REJECT
                    ),
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    true)) // Sort descending (CreatedDate)
                .ReturnsAsync(pagedResult);

            // Mock user repository to return a staff user
            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("Rejected", STATUS_REJECT, CREATED_DATE, ORDER_DESC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify that the GetAllAsync method was called with correct filter for REJECT status
            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First()) // Ensure filter applies status REJECT
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                true), Times.Once); // Ensure sorting is descending (CreatedDate)
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnFilteredAndSortedResults_WhenUserIsStaffWithSearchOrderDESCAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(ClaimTypes.Role, STAFF_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Sample data for APPROVE status
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Approved Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Approved Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.Now.AddDays(-2),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            // Mock repository to return the data for APPROVE status
            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.Is<Expression<Func<Certificate, bool>>>(filter =>
                        filter.Compile().Invoke(certificates.First()) // Ensures filter applies status APPROVE
                    ),
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    true)) // Sort descending (CreatedDate)
                .ReturnsAsync(pagedResult);

            // Mock user repository to return a staff user
            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("Approved", STATUS_APPROVE, CREATED_DATE, ORDER_DESC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify that the GetAllAsync method was called with correct filter for APPROVE status
            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First()) // Ensure filter applies status APPROVE
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                true), Times.Once); // Ensure sorting is descending (CreatedDate)
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllCertificatesSortedByCreatedDateASC_WhenUserIsStaffWithNoSearchAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(ClaimTypes.Role, STAFF_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Sample data for All status (No filtering on status)
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.Now.AddDays(-2),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            // Mock repository to return all certificates without filtering on status (All status)
            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<Certificate, bool>>>(), // No filtering on status (All status)
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    false)) // Sort ascending by CreatedDate (ASC)
                .ReturnsAsync(pagedResult);

            // Mock user repository to return a staff user
            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("", STATUS_ALL, CREATED_DATE, ORDER_ASC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify that the GetAllAsync method was called with no filter (All status) and sorted by CreatedDate ASC
            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.IsAny<Expression<Func<Certificate, bool>>>(), // No filter applied for status
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                false), Times.Once); // Ensure sorting is ascending (CreatedDate)
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedCertificatesSortedByCreatedDateASC_WhenUserIsStaffWithNoSearchAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(ClaimTypes.Role, STAFF_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Sample data for APPROVE status (filtering only certificates with status APPROVE)
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.Now.AddDays(-2),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            // Mock repository to return only APPROVE status certificates and sorted by CreatedDate in ascending order
            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.Is<Expression<Func<Certificate, bool>>>(filter => filter.Compile().Invoke(certificates.First())), // Filter for APPROVE status
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    false)) // Sort ascending by CreatedDate (ASC)
                .ReturnsAsync(pagedResult);

            // Mock user repository to return a staff user
            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("", STATUS_APPROVE, CREATED_DATE, ORDER_ASC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify that the GetAllAsync method was called with APPROVE status filter and sorted by CreatedDate ASC
            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First()) && // Apply filter to check it includes only APPROVE status certificates
                    filter.Compile().Invoke(certificates.Last())
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                false), Times.Once); // Ensure sorting is ascending (CreatedDate)
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedCertificatesSortedByCreatedDateASC_WhenUserIsStaffWithNoSearchAndStatusIsReject()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(ClaimTypes.Role, STAFF_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Sample data for REJECT status (filtering only certificates with status REJECT)
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.Now.AddDays(-2),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            // Mock repository to return only REJECT status certificates and sorted by CreatedDate in ascending order
            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.Is<Expression<Func<Certificate, bool>>>(filter => filter.Compile().Invoke(certificates.First())), // Filter for REJECT status
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    false)) // Sort ascending by CreatedDate (ASC)
                .ReturnsAsync(pagedResult);

            // Mock user repository to return a staff user
            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("", STATUS_REJECT, CREATED_DATE, ORDER_ASC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify that the GetAllAsync method was called with REJECT status filter and sorted by CreatedDate ASC
            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First()) && // Apply filter to check it includes only REJECT status certificates
                    filter.Compile().Invoke(certificates.Last())
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                false), Times.Once); // Ensure sorting is ascending (CreatedDate)
        }
        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingCertificatesSortedByCreatedDateASC_WhenUserIsStaffWithNoSearchAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(ClaimTypes.Role, STAFF_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Sample data for PENDING status (filtering only certificates with status PENDING)
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.Now.AddDays(-2),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            // Mock repository to return only PENDING status certificates and sorted by CreatedDate in ascending order
            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.Is<Expression<Func<Certificate, bool>>>(filter => filter.Compile().Invoke(certificates.First())), // Filter for PENDING status
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    false)) // Sort ascending by CreatedDate (ASC)
                .ReturnsAsync(pagedResult);

            // Mock user repository to return a staff user
            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("", STATUS_PENDING, CREATED_DATE, ORDER_ASC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify that the GetAllAsync method was called with PENDING status filter and sorted by CreatedDate ASC
            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First()) && // Apply filter to check it includes only PENDING status certificates
                    filter.Compile().Invoke(certificates.Last())
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                false), Times.Once); // Ensure sorting is ascending (CreatedDate)
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnRejectedCertificatesSortedByCreatedDateDESC_WhenUserIsStaffWithNoSearchAndStatusIsRejected()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(ClaimTypes.Role, STAFF_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Sample data for REJECTED status (filtering only certificates with status REJECT)
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.Now.AddDays(-2),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.REJECT,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            // Mock repository to return only REJECT status certificates and sorted by CreatedDate in descending order
            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.Is<Expression<Func<Certificate, bool>>>(filter => filter.Compile().Invoke(certificates.First())), // Filter for REJECT status
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    true)) // Sort descending by CreatedDate (DESC)
                .ReturnsAsync(pagedResult);

            // Mock user repository to return a staff user
            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("", STATUS_REJECT, CREATED_DATE, ORDER_DESC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify that the GetAllAsync method was called with REJECT status filter and sorted by CreatedDate DESC
            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First()) && // Apply filter to check it includes only REJECT status certificates
                    filter.Compile().Invoke(certificates.Last())
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                true), Times.Once); // Ensure sorting is descending (CreatedDate)
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPendingCertificatesSortedByCreatedDateDESC_WhenUserIsStaffWithNoSearchAndStatusIsPending()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(ClaimTypes.Role, STAFF_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Sample data for PENDING status (filtering only certificates with status PENDING)
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId",
                    CertificateName = "Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.Now.AddDays(-2),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId",
                    CertificateName = "Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    Submitter = new Tutor { TutorId = "testUserId" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            // Mock repository to return only PENDING status certificates and sorted by CreatedDate in descending order
            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.Is<Expression<Func<Certificate, bool>>>(filter => filter.Compile().Invoke(certificates.First())), // Filter for PENDING status
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    true)) // Sort descending by CreatedDate (DESC)
                .ReturnsAsync(pagedResult);

            // Mock user repository to return a staff user
            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("", STATUS_PENDING, CREATED_DATE, ORDER_DESC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify that the GetAllAsync method was called with PENDING status filter and sorted by CreatedDate DESC
            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First()) && // Apply filter to check it includes only PENDING status certificates
                    filter.Compile().Invoke(certificates.Last())
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                true), Times.Once); // Ensure sorting is descending (CreatedDate)
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllCertificatesSortedByCreatedDateDESC_WhenUserIsStaffWithNoSearchAndStatusIsAll()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(ClaimTypes.Role, STAFF_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Sample data for ALL status (no filter applied)
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId1",
                    CertificateName = "Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.PENDING, // This status could vary as we are fetching "All"
                    CreatedDate = DateTime.Now.AddDays(-2),
                    Submitter = new Tutor { TutorId = "testUserId1" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId2",
                    CertificateName = "Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE, // This status could vary as we are fetching "All"
                    CreatedDate = DateTime.Now.AddDays(-1),
                    Submitter = new Tutor { TutorId = "testUserId2" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            // Mock repository to return all certificates sorted by CreatedDate in descending order
            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.Is<Expression<Func<Certificate, bool>>>(filter => filter.Compile().Invoke(certificates.First()) && filter.Compile().Invoke(certificates.Last())), // No specific filter as status is ALL
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    true)) // Sort descending by CreatedDate (DESC)
                .ReturnsAsync(pagedResult);

            // Mock user repository to return a staff user
            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("", STATUS_ALL, CREATED_DATE, ORDER_DESC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify that the GetAllAsync method was called with no specific filter (All status) and sorted by CreatedDate DESC
            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First()) && // No specific status filter as status is All
                    filter.Compile().Invoke(certificates.Last())
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                true), Times.Once); // Ensure sorting is descending (CreatedDate)
        }


        [Fact]
        public async Task GetAllAsync_ShouldReturnApprovedCertificatesSortedByCreatedDateDESC_WhenUserIsStaffWithNoSearchAndStatusIsApprove()
        {
            // Arrange
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testStaffId"),
                new Claim(ClaimTypes.Role, STAFF_ROLE)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Sample data for APPROVE status (only fetching APPROVE status)
            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    SubmitterId = "testUserId1",
                    CertificateName = "Certificate 1",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.Now.AddDays(-2),
                    Submitter = new Tutor { TutorId = "testUserId1" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                },
                new Certificate
                {
                    Id = 2,
                    SubmitterId = "testUserId2",
                    CertificateName = "Certificate 2",
                    IsDeleted = false,
                    RequestStatus = Status.APPROVE,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    Submitter = new Tutor { TutorId = "testUserId2" },
                    CertificateMedias = It.IsAny<List<CertificateMedia>>()
                }
            };

            var pagedResult = (certificates.Count, certificates);

            // Mock repository to return certificates with APPROVE status sorted by CreatedDate in descending order
            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.Is<Expression<Func<Certificate, bool>>>(filter => filter.Compile().Invoke(certificates.First()) && filter.Compile().Invoke(certificates.Last())), // Filter for APPROVE status
                    "Submitter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    true)) // Sort descending by CreatedDate (DESC)
                .ReturnsAsync(pagedResult);

            // Mock user repository to return a staff user
            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(It.IsAny<ApplicationUser>());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("", STATUS_APPROVE, CREATED_DATE, ORDER_DESC, 1);
            var okObjectResult = result.Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = okObjectResult.Value as APIResponse;
            response.Should().NotBeNull();
            response.Result.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Pagination.Should().NotBeNull();

            // Verify that the GetAllAsync method was called with APPROVE status and sorted by CreatedDate DESC
            _certificateRepositoryMock.Verify(repo => repo.GetAllAsync(
                It.Is<Expression<Func<Certificate, bool>>>(filter =>
                    filter.Compile().Invoke(certificates.First()) && // Only APPROVE status certificates
                    filter.Compile().Invoke(certificates.Last())
                ),
                "Submitter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                true), Times.Once); // Ensure sorting is descending (CreatedDate)
        }


    }

}