using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using backend_api.Utils;
using Microsoft.EntityFrameworkCore;
using backend_api.Data;
using backend_api.Models;
using Microsoft.AspNetCore.Identity;
using FluentAssertions;
using System.Linq.Expressions;
using System.Security.Claims;

namespace backend_api.Repository.Tests
{
    public class ClaimRepositoryTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly ClaimRepository _repository;
        private string userId;
        private int total;
        private int defaultPageSize = 10;
        private List<ApplicationClaim> claims = new List<ApplicationClaim>();
        public ClaimRepositoryTests()
        {
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var databaseName = $"SEP490_{Guid.NewGuid()}";
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName) // Use a unique name for each test
                .Options;
            InitializeDatabase();
            // Initialize the repository with the in-memory context
            _repository = new ClaimRepository(new ApplicationDbContext(_options, _mockHttpContextAccessor.Object));
        }

        private void InitializeDatabase()
        {
            using (var context = new ApplicationDbContext(_options, _mockHttpContextAccessor.Object))
            {
                var roleAdmin = new IdentityRole { Id = Guid.NewGuid().ToString(), Name = SD.ADMIN_ROLE, NormalizedName = SD.ADMIN_ROLE.ToUpper() };
                context.Roles.Add(roleAdmin);
                var baseUrl = string.Empty;
                context.SaveChanges();
                if (_mockHttpContextAccessor.Object.HttpContext != null)
                {
                    var httpContext = _mockHttpContextAccessor.Object.HttpContext;
                    baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host.Value}{httpContext.Request.PathBase.Value}";
                }
                var adminUser = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = "admin@admin.com",
                    FullName = "admin",
                    PasswordHash = PasswordGenerator.GeneratePassword(),
                    UserName = "admin@admin.com",
                    CreatedDate = DateTime.Now,
                    ImageLocalPathUrl = @"wwwroot\UserImages\default-avatar.png",
                    ImageLocalUrl = baseUrl + $"/{SD.URL_IMAGE_USER}/" + SD.IMAGE_DEFAULT_AVATAR_NAME,
                    ImageUrl = SD.URL_IMAGE_DEFAULT_BLOB
                };
                context.ApplicationUsers.Add(adminUser);
                context.ApplicationClaims.AddRange(
                    new ApplicationClaim
                    {
                        Id = 1,
                        ClaimType = SD.DEFAULT_CREATE_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_CREATE_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_CREATE_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_CREATE_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        Id = 2,
                        ClaimType = SD.DEFAULT_DELETE_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_DELETE_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_DELETE_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_DELETE_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        Id = 3,
                        ClaimType = SD.DEFAULT_UPDATE_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_UPDATE_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_UPDATE_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_UPDATE_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        Id = 4,
                        ClaimType = SD.DEFAULT_CREATE_CLAIM_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_CREATE_CLAIM_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_CREATE_CLAIM_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_CREATE_CLAIM_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        Id = 5,
                        ClaimType = SD.DEFAULT_DELETE_CLAIM_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_DELETE_CLAIM_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_DELETE_CLAIM_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_DELETE_CLAIM_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        Id = 6,
                        ClaimType = SD.DEFAULT_UPDATE_CLAIM_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_UPDATE_CLAIM_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_UPDATE_CLAIM_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_UPDATE_CLAIM_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        Id = 7,
                        ClaimType = SD.DEFAULT_CREATE_ROLE_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_CREATE_ROLE_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_CREATE_ROLE_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_CREATE_ROLE_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        Id = 8,
                        ClaimType = SD.DEFAULT_DELETE_ROLE_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_DELETE_ROLE_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_DELETE_ROLE_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_DELETE_ROLE_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        Id = 9,
                        ClaimType = SD.DEFAULT_UPDATE_ROLE_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_UPDATE_ROLE_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_UPDATE_ROLE_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_UPDATE_ROLE_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        Id = 10,
                        ClaimType = SD.DEFAULT_CREATE_USER_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_CREATE_USER_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_CREATE_USER_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_CREATE_USER_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        Id = 11,
                        ClaimType = SD.DEFAULT_DELETE_USER_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_DELETE_USER_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_DELETE_USER_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_DELETE_USER_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        Id = 12,
                        ClaimType = SD.DEFAULT_UPDATE_USER_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_UPDATE_USER_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_UPDATE_USER_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_UPDATE_USER_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        Id = 13,
                        ClaimType = SD.DEFAULT_ASSIGN_ROLE_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_ASSIGN_ROLE_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_ASSIGN_ROLE_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_ASSIGN_ROLE_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        Id = 14,
                        ClaimType = SD.DEFAULT_ASSIGN_CLAIM_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_ASSIGN_CLAIM_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_ASSIGN_CLAIM_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_ASSIGN_CLAIM_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    }
                );

                context.SaveChanges();
                userId = adminUser.Id;
                claims = context.ApplicationClaims.ToList();
                total = context.ApplicationClaims.Count();
            }
        }
        [Fact]
        public async Task UpdateAsync_ValidClaim_UpdatesClaimSuccessfully()
        {
            // Arrange
            var existingClaim = new ApplicationClaim { Id = 1, ClaimType = "Update", ClaimValue = "Claim", DefaultClaimType = "Delete", DefaultClaimValue = "Role", UserId = userId };

            // Act
            var result = await _repository.UpdateAsync(existingClaim);

            // Assert
            using (var context = new ApplicationDbContext(_options, _mockHttpContextAccessor.Object))
            {
                var updatedClaim = await context.ApplicationClaims.FindAsync(1);
                updatedClaim.Should().NotBeNull();
                updatedClaim.ClaimType.Should().Be("Update");
                updatedClaim.ClaimValue.Should().Be("Claim");
            }
        }

        [Fact]
        public async Task UpdateAsync_NonExistentClaim_ThrowsException()
        {
            // Arrange
            var nonExistentClaim = new ApplicationClaim { Id = 99, ClaimType = "Update", ClaimValue = "Claim", DefaultClaimType = "Delete", DefaultClaimValue = "Role", UserId = userId };

            // Act & Assert
            await Xunit.Assert.ThrowsAsync<Exception>(async () =>
            {
                await _repository.UpdateAsync(nonExistentClaim);
            });
        }

        [Fact]
        public void UpdateAsync_NullContext_ThrowsArgumentNullException()
        {
            // Act & Assert
            var result = Xunit.Assert.Throws<NullReferenceException>(() => new ClaimRepository(null));
            result.Message.Should().Be("Abc");
        }


        [Fact]
        public void GetTotalClaim_NoUserClaims_ReturnsTotalClaims()
        {
            // Act
            var totalClaims = _repository.GetTotalClaim(null);
            // Assert
            totalClaims.Should().Be(total); // There are three claims in total
        }

        [Fact]
        public void GetTotalClaim_EmptyUserClaims_ReturnsTotalClaims()
        {
            // Act
            var totalClaims = _repository.GetTotalClaim(new List<UserClaim>());
            // Assert
            totalClaims.Should().Be(total); // Should still return total claims as there are no user claims to filter
        }

        [Fact]
        public void GetTotalClaim_WithUserClaims_ReturnsFilteredClaimsCount()
        {
            // Arrange
            var userClaims = new List<UserClaim>
            {
                new UserClaim { ClaimType = SD.DEFAULT_DELETE_USER_CLAIM_TYPE, ClaimValue = SD.DEFAULT_DELETE_USER_CLAIM_VALUE } // This claim should be filtered out
            };
            // Act
            var totalClaims = _repository.GetTotalClaim(userClaims);
            // Assert
            totalClaims.Should().Be(total - 1); // One claim is filtered out, so total should be 2
        }

        [Fact]
        public async Task GetAllAsync_NoFilter_ReturnsAllClaims()
        {
            // Act
            var (totalCount, claims) = await _repository.GetAllAsync();

            // Assert
            totalCount.Should().Be(total);
            claims.Count.Should().Be(defaultPageSize);
        }

        [Fact]
        public async Task GetAllAsync_WithFilter_ReturnsFilteredClaims()
        {
            // Arrange
            Expression<Func<ApplicationClaim, bool>> filter = claim => claim.ClaimType == "Update";

            // Act
            var (totalCount, claims) = await _repository.GetAllAsync(filter);

            // Assert
            totalCount.Should().Be(1);
            claims.Count.Should().Be(1);
        }

        [Fact]
        public async Task GetAllAsync_WithPagination_ReturnsPagedClaims()
        {
            // Arrange
            int pageSize = 10;
            int pageNumber = 5;
            // Act
            var (totalCount, claims) = await _repository.GetAllAsync(pageSize: pageSize, pageNumber: pageNumber);
            int totalReturn = pageSize * (pageNumber-1) - total < 0 ? pageSize : 0;
            // Assert
            totalCount.Should().Be(total);
            claims.Count.Should().Be(totalReturn); // First page should have 2 claims
        }

        [Fact]
        public async Task GetAllAsync_WithUserClaims_FiltersOutClaims()
        {
            // Arrange
            var userClaims = new List<UserClaim>
            {
                new UserClaim { ClaimType = SD.DEFAULT_ASSIGN_CLAIM_CLAIM_TYPE, ClaimValue = SD.DEFAULT_ASSIGN_CLAIM_CLAIM_VALUE } // This claim should be filtered out
            };

            // Act
            var result = await _repository.GetAllAsync(null, null, 10, 1, userClaims);

            // Assert
            result.TotalCount.Should().Be(total - userClaims.Count);
            result.list.Should().HaveCount(defaultPageSize);
        }
        
        [Fact]
        public async Task GetAllAsync_WithPaginationAndUserClaims_ReturnsPagedFilteredClaims()
        {
            // Arrange
            var userClaims = new List<UserClaim>
            {
                new UserClaim { ClaimType = "Edit", ClaimValue = "Article" } // This claim should be filtered out
            };

            // Act
            var (totalCount, claims) = await _repository.GetAllAsync(pageSize: 2, pageNumber: 1, userClaims: userClaims);

            // Assert
            totalCount.Should().Be(2); // Two claims remaining after filtering
            claims.Count.Should().Be(2); // First page should have all remaining claims
        }

        [Fact]
        public async Task GetAllAsync_NoFilter_NoUserClaims_FirstPage_ReturnsAllClaims()
        {
            // Arrange
            // Act
            var result = await _repository.GetAllAsync(null, null, 10, 1, null);

            // Assert
            result.TotalCount.Should().Be(total);
            result.list.Should().HaveCount(10);
        }
    }
}