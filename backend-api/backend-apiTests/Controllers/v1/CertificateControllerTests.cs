using AutoMapper;
using backend_api.Mapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.RabbitMQSender;
using backend_api.Repository.IRepository;
using backend_api.Services.IServices;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using System.Text;
using Xunit;
using static backend_api.SD;

namespace backend_api.Controllers.v1.Tests
{
    public class CertificateControllerTests
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
            _loggerMock = new Mock<ILogger<CertificateController>>();
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
               _resourceServiceMock.Object
           );
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
        public async Task GetActive_ReturnsCertificate_WhenUserIsTutorAndCertificateExists()
        {
            // Arrange
            var certificateId = 1;
            var certificate = new Certificate { Id = certificateId, IsDeleted = false };
            var certificateDTO = new CertificateDTO { Id = certificateId };

            _certificateRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Certificate, bool>>>(), false, "CertificateMedias", null))
                .ReturnsAsync(certificate);

            var userClaims = new List<Claim> { new Claim(ClaimTypes.Role, SD.TUTOR_ROLE) };
            var user = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            // Act
            var result = await _controller.GetActive(certificateId);
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
        public async Task GetActive_ReturnsCertificate_WhenUserIsNotTutorAndCertificateExists()
        {
            // Arrange
            var certificateId = 1;
            var certificate = new Certificate { Id = certificateId };
            var certificateDTO = new CertificateDTO { Id = certificateId };

            _certificateRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Certificate, bool>>>(), false, "CertificateMedias", null))
                .ReturnsAsync(certificate);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>(), "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Act
            var result = await _controller.GetActive(certificateId);
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
        public async Task GetActive_ReturnsNotFound_WhenCertificateDoesNotExist()
        {
            // Arrange
            var certificateId = 1;
            _certificateRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Certificate, bool>>>(), It.IsAny<bool>(), "CertificateMedias", It.IsAny<string>()))
                .ReturnsAsync((Certificate)null);

            _resourceServiceMock.Setup(r => r.GetString(SD.NOT_FOUND_MESSAGE, SD.CERTIFICATE))
                .Returns("Không tìm thấy 'Chứng chỉ'.");

            // Act
            var result = await _controller.GetActive(certificateId);
            var badRequestResult = result as BadRequestObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Không tìm thấy 'Chứng chỉ'.");
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
                SubmiterId = userId
            };

            var certificateDTO = new CertificateDTO
            {
                Id = certificateId
            };

            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE)
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
            _resourceServiceMock.Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.CERTIFICATE))
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
                new Claim(ClaimTypes.Role, SD.TUTOR_ROLE)
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
                .Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE))
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
        public async Task ApproveOrRejectRequest_ApproveStatus_Succeeds()
        {
            // Arrange
            var certificateId = 1;
            var userId = "user123";
            var certificate = new Certificate { Id = certificateId, SubmiterId = userId, RequestStatus = Status.PENDING, CertificateName = "Test Certificate" };
            var tutor = new ApplicationUser { Id = userId, FullName = "Tutor Name", Email = "tutor@example.com" };
            var changeStatusDTO = new ChangeStatusDTO { Id = certificateId, StatusChange = (int)Status.APPROVE };

            _certificateRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Certificate, bool>>>(), false, "CertificateMedias", null))
                .ReturnsAsync(certificate);
            _userRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(tutor);
            _resourceServiceMock.Setup(r => r.GetString(It.IsAny<string>(), It.IsAny<string>())).Returns("Error message");

            // Set user identity
            var userClaims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Role, SD.STAFF_ROLE) };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));

            // Act
            var result = await _controller.ApproveOrRejectRequest(changeStatusDTO);
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
        public async Task ApproveOrRejectRequest_RejectStatus_Succeeds()
        {
            // Arrange
            var certificateId = 1;
            var userId = "user123";
            var certificate = new Certificate { Id = certificateId, SubmiterId = userId, RequestStatus = Status.PENDING, CertificateName = "Test Certificate" };
            var tutor = new ApplicationUser { Id = userId, FullName = "Tutor Name", Email = "tutor@example.com" };
            var changeStatusDTO = new ChangeStatusDTO { Id = certificateId, StatusChange = (int)Status.REJECT, RejectionReason = "Invalid document" };

            _certificateRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Certificate, bool>>>(), false, "CertificateMedias", null))
                .ReturnsAsync(certificate);
            _userRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), false, null))
                .ReturnsAsync(tutor);

            // Set user identity
            var userClaims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Role, SD.STAFF_ROLE) };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "mock"));

            // Act
            var result = await _controller.ApproveOrRejectRequest(changeStatusDTO);
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
        public async Task ApproveOrRejectRequest_ReturnsBadRequest_IfCertificateIsNull()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusDTO { Id = 1, StatusChange = (int)Status.APPROVE };

            _certificateRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Certificate, bool>>>(), false, "CertificateMedias", null))
                .ReturnsAsync((Certificate)null);
            _resourceServiceMock.Setup(r => r.GetString(It.IsAny<string>(), It.IsAny<string>())).Returns("Error message");

            // Act
            var result = await _controller.ApproveOrRejectRequest(changeStatusDTO);
            var badRequestResult = result as BadRequestObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Error message");
        }


        [Fact]
        public async Task ApproveOrRejectRequest_ReturnsBadRequest_IfCertificateStatusIsNotPending()
        {
            // Arrange
            var certificate = new Certificate { Id = 1, RequestStatus = Status.APPROVE };
            var changeStatusDTO = new ChangeStatusDTO { Id = 1, StatusChange = (int)Status.REJECT };

            _certificateRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Certificate, bool>>>(), false, "CertificateMedias", null))
                .ReturnsAsync(certificate);
            _resourceServiceMock.Setup(r => r.GetString(It.IsAny<string>(), It.IsAny<string>())).Returns("Error message");

            // Act
            var result = await _controller.ApproveOrRejectRequest(changeStatusDTO);
            var badRequestResult = result as BadRequestObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            var apiResponse = badRequestResult.Value as APIResponse;
            apiResponse.Should().NotBeNull();
            apiResponse.IsSuccess.Should().BeFalse();
            apiResponse.ErrorMessages.Should().Contain("Error message");
        }

        [Fact]
        public async Task ApproveOrRejectRequest_ReturnsInternalServerError_OnException()
        {
            // Arrange
            var changeStatusDTO = new ChangeStatusDTO { Id = 1, StatusChange = (int)Status.APPROVE };
            _resourceServiceMock.Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE)).Returns("Internal server error");
            _certificateRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Certificate, bool>>>(), false, "CertificateMedias", null))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.ApproveOrRejectRequest(changeStatusDTO);
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

            var certificate = new Certificate { Id = certificateId, SubmiterId = userId, IsDeleted = false };
            var newCertificate = new Certificate { Id = certificateId, SubmiterId = userId, IsDeleted = true };
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
            _resourceServiceMock.Setup(r => r.GetString(SD.BAD_REQUEST_MESSAGE, SD.CERTIFICATE)).Returns("Certificate not found.");
            // Act
            var result = await _controller.DeleteAsync(certificateId);
            var badRequestResult = result.Result as ObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
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
            _resourceServiceMock.Setup(r => r.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE)).Returns("Internal server error");

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
        public async Task GetAllAsync_ShouldReturnFilteredAndSortedResults_WhenUserIsTutorWithSearchOrderAndStatus()
        {
            // Arrange
            var certificates = new List<Certificate>
        {
            new Certificate
            {
                Id = 1,
                SubmiterId = "testUserId",
                CertificateName = "Sample Certificate",
                IsDeleted = false,
                RequestStatus = Status.PENDING,
                CreatedDate = DateTime.Now,
            }
        };

            var pagedResult = (1, certificates);

            _certificateRepositoryMock
                .Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<Certificate, bool>>>(),
                    "Submiter,CertificateMedias",
                    5, // PageSize
                    1, // PageNumber
                    It.IsAny<Expression<Func<Certificate, object>>>(),
                    true)) // Sort descending
                .ReturnsAsync(pagedResult);

            _userRepositoryMock
                .Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), false, null))
                .ReturnsAsync(new ApplicationUser());

            _curriculumRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<Curriculum, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<Curriculum, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<Curriculum>()));

            _workExperienceRepositoryMock
                .Setup(repo => repo.GetAllNotPagingAsync(It.IsAny<Expression<Func<WorkExperience, bool>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Expression<Func<WorkExperience, object>>>(), It.IsAny<bool>()))
                .ReturnsAsync((1, new List<WorkExperience>()));

            // Act
            var result = await _controller.GetAllAsync("Sample", SD.STATUS_PENDING, SD.CREADTED_DATE, SD.ORDER_DESC, 1);
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
                "Submiter,CertificateMedias",
                5, // PageSize
                1, // PageNumber
                It.IsAny<Expression<Func<Certificate, object>>>(),
                true), Times.Once);
        }

    }

}