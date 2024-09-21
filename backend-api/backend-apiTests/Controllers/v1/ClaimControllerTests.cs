using Xunit;
using backend_api.Controllers.v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using backend_api.Repository.IRepository;
using backend_api.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Microsoft.Extensions.Configuration;
using backend_api.Models.DTOs;
using backend_api.Models;
using System.Net;
using FluentAssertions;
using System.Linq.Expressions;

namespace backend_api.Controllers.v1.Tests
{
    public class ClaimControllerTests
    {
        private readonly Mock<IClaimRepository> _mockClaimRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly ClaimController _controller;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<FormatString> _mockFormatString;
        private readonly APIResponse _response;


        public ClaimControllerTests()
        {
            _mockClaimRepository = new Mock<IClaimRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockFormatString = new Mock<FormatString>();

            // Setup the configuration to return a specific page size
            _mockConfiguration.Setup(config => config["APIConfig:PageSize"]).Returns("10");
            _mockConfiguration.Setup(config => config["APIConfig:TakeValue"]).Returns("5");

            _controller = new ClaimController(
                _mockClaimRepository.Object,
                _mockMapper.Object,
                _mockConfiguration.Object,
                _mockUserRepository.Object,
                _mockFormatString.Object
                );
            _response = new APIResponse();
            // Setup a mock HttpContext to avoid null reference exceptions
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        //[Fact()]
        //public async Task GetAllClaimsAsyncTestAsync()
        //{
        //    // Arrange
        //    var filter = It.IsAny<Expression<Func<ApplicationClaim, bool>>>();
        //    var claims = new List<ApplicationClaim>
        //    {
        //        new ApplicationClaim { Id = 1, ClaimType = "create", ClaimValue = "claim1" },
        //        new ApplicationClaim { Id = 2, ClaimType = "update", ClaimValue = "claim2" }
        //    };
        //    var claimDTOs = new List<ClaimDTO>
        //    {
        //        new ClaimDTO { Id = 1, ClaimType = "create", ClaimValue = "claim1" },
        //        new ClaimDTO { Id = 2, ClaimType = "update", ClaimValue = "claim2" }
        //    };

        //    var userClaims = new List<UserClaim>
        //    {
        //        new UserClaim { Id = 1, UserId = "1" }
        //    };

        //    var users = new List<ApplicationUser>
        //    {
        //        new ApplicationUser { Id = "1", Email = "user1@test.com" }
        //    };

        //    var pagination = new Pagination { PageNumber = 1, PageSize = 10, Total = 2 };

        //    // Mock the repository methods
        //    _mockClaimRepository.Setup(repo => repo.GetAllAsync(filter, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), userClaims))
        //        .ReturnsAsync((2, claims));

        //    _mockUserRepository.Setup(repo => repo.GetClaimByUserIdAsync(It.IsAny<string>()))
        //        .ReturnsAsync(userClaims);

        //    _mockUserRepository.Setup(repo => repo.GetUsersForClaimAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
        //        .ReturnsAsync((1, users)); 


        //    _mockMapper.Setup(mapper => mapper.Map<List<ClaimDTO>>(claims))
        //        .Returns(claimDTOs);

        //    _mockMapper.Setup(mapper => mapper.Map<List<ApplicationUserDTO>>(users))
        //        .Returns(new List<ApplicationUserDTO>
        //        {
        //        new ApplicationUserDTO { Id = "1", Email = "user1@test.com" }
        //        });

        //    // Act
        //    var result = await _controller.GetAllClaimsAsync(null, null, 1, null);

        //    // Assert
        //    var okResult = result.Result as OkObjectResult;
        //    okResult.Should().NotBeNull();
        //    okResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

        //    var apiResponse = okResult.Value as APIResponse;
        //    apiResponse.Should().NotBeNull();
        //    apiResponse.IsSuccess.Should().BeTrue();
        //    apiResponse.Result.Should().BeEquivalentTo(claimDTOs);
        //    apiResponse.Pagination.Should().BeEquivalentTo(pagination);
        //}

        //[Fact]
        //public async Task GetAllClaimsAsync_ShouldReturnInternalServerError_WhenExceptionThrown()
        //{
        //    // Arrange
        //    _mockClaimRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<ApplicationClaim, bool>>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), null))
        //        .ThrowsAsync(new Exception("Database error"));

        //    // Act
        //    var result = await _controller.GetAllClaimsAsync(null, null, 1, null);

        //    // Assert
        //    var statusCodeResult = result.Result as ObjectResult;
        //    statusCodeResult.Should().NotBeNull();
        //    statusCodeResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

        //    var apiResponse = statusCodeResult.Value as APIResponse;
        //    apiResponse.Should().NotBeNull();
        //    apiResponse.IsSuccess.Should().BeFalse();
        //    apiResponse.ErrorMessages.Should().Contain("Database error");
        //}
    }
}