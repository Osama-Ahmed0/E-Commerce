using ECommerce.Configuration;
using ECommerce.Data.Models;
using ECommerce.Dtos;
using ECommerce.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MockQueryable.Moq;
using Moq;
using System.IdentityModel.Tokens.Jwt;

namespace ECommerce.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<User>> userManagerMock;
        private readonly JwtOptions jwtOptions;
        private readonly AuthService service;

        public AuthServiceTests()
        {
            var store = new Mock<IUserStore<User>>();

            var options = Options.Create(new IdentityOptions());
            var passwordHasher = new PasswordHasher<User>();
            var userValidators = new List<IUserValidator<User>>();
            var passwordValidators = new List<IPasswordValidator<User>>();
            var lookupNormalizer = new Mock<ILookupNormalizer>().Object;
            var errorDescriber = new IdentityErrorDescriber();
            var services = new Mock<IServiceProvider>().Object;
            var logger = new Mock<Microsoft.Extensions.Logging.ILogger<UserManager<User>>>().Object;

            userManagerMock = new Mock<UserManager<User>>(store.Object,
                options,
                passwordHasher,
                userValidators,
                passwordValidators,
                lookupNormalizer,
                errorDescriber,
                services,
                logger);

            jwtOptions = new JwtOptions
            {
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                Lifetime = 60,
                SigningKey = "ThisIsAValidSigningKeyForTesting123456789"
            };

            service = new AuthService(userManagerMock.Object, jwtOptions);
        }

        // =========================================================
        // RegisterAsync
        // =========================================================

        [Fact]
        public async Task RegisterAsync_createUserSucceeded_roleSucceeded_resultSucceeded()
        {
            // Arrange
            var dto = new RegisterDto
            {
                FullName = "John Doe",
                Email = "john@example.com",
                UserName = "john",
                Password = "Password123"
            };

            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                FullName = dto.FullName,
                Role = UserRole.Customer
            };

            userManagerMock
                .Setup(x => x.CreateAsync(
                    It.IsAny<User>(),
                    dto.Password))
                .ReturnsAsync(IdentityResult.Success);

            userManagerMock
                .Setup(x => x.AddToRoleAsync(
                    It.IsAny<User>(),
                    UserRole.Customer.ToString()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await service.RegisterAsync(
                dto,
                UserRole.Customer);

            // Assert
            Assert.True(result.Succeeded);

            userManagerMock.Verify(
                x => x.CreateAsync(
                    It.Is<User>(u =>
                        u.UserName == dto.UserName &&
                        u.Email == dto.Email &&
                        u.FullName == dto.FullName &&
                        u.Role == UserRole.Customer),
                    dto.Password),
                Times.Once);

            userManagerMock.Verify(
                x => x.AddToRoleAsync(
                    It.IsAny<User>(),
                    "Customer"),
                Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_createUserFailed_returnsCreateResult()
        {
            // Arrange
            var dto = new RegisterDto
            {
                FullName = "John Doe",
                Email = "john@example.com",
                UserName = "john",
                Password = "Password123"
            };

            var errors = new[]
            {
                new IdentityError
                {
                    Code = "DuplicateUserName",
                    Description = "Username already exists."
                }
            };

            var createResult = IdentityResult.Failed(errors);

            userManagerMock
                .Setup(x => x.CreateAsync(
                    It.IsAny<User>(),
                    dto.Password))
                .ReturnsAsync(createResult);

            // Act
            var result = await service.RegisterAsync(
                dto,
                UserRole.Customer);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains(
                result.Errors,
                e => e.Code == "DuplicateUserName");

            userManagerMock.Verify(
                x => x.AddToRoleAsync(
                    It.IsAny<User>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_roleAssignmentFailed_deletesUserAndReturnsRoleResult()
        {
            // Arrange
            var dto = new RegisterDto
            {
                FullName = "John Doe",
                Email = "john@example.com",
                UserName = "john",
                Password = "Password123"
            };

            userManagerMock
                .Setup(x => x.CreateAsync(
                    It.IsAny<User>(),
                    dto.Password))
                .ReturnsAsync(IdentityResult.Success);

            var roleResult = IdentityResult.Failed(
                new IdentityError
                {
                    Code = "RoleError",
                    Description = "Could not assign role."
                });

            userManagerMock
                .Setup(x => x.AddToRoleAsync(
                    It.IsAny<User>(),
                    UserRole.Customer.ToString()))
                .ReturnsAsync(roleResult);

            userManagerMock
                .Setup(x => x.DeleteAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await service.RegisterAsync(
                dto,
                UserRole.Customer);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains(
                result.Errors,
                e => e.Code == "RoleError");

            userManagerMock.Verify(
                x => x.DeleteAsync(It.IsAny<User>()),
                Times.Once);
        }

        // =========================================================
        // LoginAsync
        // =========================================================

        [Fact]
        public async Task LoginAsync_validCredentials_returnsAuthenticatedResponse()
        {
            // Arrange
            var user = new User
            {
                Id = "user-1",
                UserName = "john",
                Email = "john@example.com",
                FullName = "John Doe",
                Role = UserRole.Customer,
                RefreshTokens = new List<RefreshToken>()
            };

            var dto = new UserLoginDto
            {
                UserName = "john",
                Password = "Password123"
            };

            userManagerMock
                .Setup(x => x.FindByNameAsync(dto.UserName))
                .ReturnsAsync(user);

            userManagerMock
                .Setup(x => x.CheckPasswordAsync(user, dto.Password))
                .ReturnsAsync(true);

            userManagerMock
                .Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await service.LoginAsync(dto);

            // Assert
            Assert.True(result.IsAuthenticated);
            Assert.NotNull(result.Token);
            Assert.NotEmpty(result.Token);

            Assert.Equal("john", result.UserName);
            Assert.Equal("john@example.com", result.Email);
            Assert.Equal("Customer", result.Role);

            Assert.NotNull(result.RefreshToken);
            Assert.NotEmpty(result.RefreshToken);

            Assert.True(result.RefreshTokenExpiration > DateTime.UtcNow);

            Assert.Single(user.RefreshTokens);

            userManagerMock.Verify(
                x => x.UpdateAsync(user),
                Times.Once);
        }

        [Fact]
        public async Task LoginAsync_userDoesNotExist_returnsInvalidCredentials()
        {
            // Arrange
            var dto = new UserLoginDto
            {
                UserName = "unknown",
                Password = "Password123"
            };

            userManagerMock
                .Setup(x => x.FindByNameAsync(dto.UserName))
                .ReturnsAsync((User?)null);

            // Act
            var result = await service.LoginAsync(dto);

            // Assert
            Assert.False(result.IsAuthenticated);
            Assert.Equal(
                "Invalid credentials",
                result.Message);

            Assert.Null(result.Token);
            Assert.Null(result.RefreshToken);

            userManagerMock.Verify(
                x => x.CheckPasswordAsync(
                    It.IsAny<User>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task LoginAsync_wrongPassword_returnsInvalidCredentials()
        {
            // Arrange
            var user = new User
            {
                Id = "user-1",
                UserName = "john",
                Email = "john@example.com",
                Role = UserRole.Customer
            };

            var dto = new UserLoginDto
            {
                UserName = "john",
                Password = "WrongPassword"
            };

            userManagerMock
                .Setup(x => x.FindByNameAsync(dto.UserName))
                .ReturnsAsync(user);

            userManagerMock
                .Setup(x => x.CheckPasswordAsync(user, dto.Password))
                .ReturnsAsync(false);

            // Act
            var result = await service.LoginAsync(dto);

            // Assert
            Assert.False(result.IsAuthenticated);
            Assert.Equal(
                "Invalid credentials",
                result.Message);

            Assert.Null(result.Token);
            Assert.Null(result.RefreshToken);

            userManagerMock.Verify(
                x => x.UpdateAsync(It.IsAny<User>()),
                Times.Never);
        }

        [Fact]
        public async Task LoginAsync_validCredentials_jwtContainsExpectedClaims()
        {
            // Arrange
            var user = new User
            {
                Id = "user-123",
                UserName = "john",
                Email = "john@example.com",
                Role = UserRole.Admin,
                RefreshTokens = new List<RefreshToken>()
            };

            var dto = new UserLoginDto
            {
                UserName = "john",
                Password = "Password123"
            };

            userManagerMock
                .Setup(x => x.FindByNameAsync(dto.UserName))
                .ReturnsAsync(user);

            userManagerMock
                .Setup(x => x.CheckPasswordAsync(user, dto.Password))
                .ReturnsAsync(true);

            userManagerMock
                .Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await service.LoginAsync(dto);

            // Assert
            Assert.NotNull(result.Token);

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(result.Token);

            Assert.Equal(jwtOptions.Issuer, jwt.Issuer);

            Assert.Equal(jwtOptions.Audience, jwt.Audiences.Single());

            Assert.Equal(user.Id, jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);

            Assert.Equal(user.UserName, jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);

            Assert.Equal(user.Email, jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);

            Assert.Equal("Admin", jwt.Claims.First(c => c.Type == "role").Value);
        }

        // =========================================================
        // RefreshTokenAsync
        // =========================================================

        [Fact]
        public async Task RefreshTokenAsync_nullToken_returnsInvalidRefreshToken()
        {
            // Arrange
            string? token = null;

            // Act
            var result = await service.RefreshTokenAsync(token);

            // Assert
            Assert.False(result.IsAuthenticated);
            Assert.Equal("Invalid refresh token", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("invalid-token")]
        public async Task RefreshTokenAsync_invalidToken_returnsInvalidRefreshToken(string token)
        {
            // Arrange
            SetupUsersQueryable(new List<User>());

            // Act
            var result = await service.RefreshTokenAsync(token);

            // Assert
            Assert.False(result.IsAuthenticated);
            Assert.Equal("Invalid refresh token", result.Message);
        }

        [Fact]
        public async Task RefreshTokenAsync_expiredToken_returnsExpiredMessage()
        {
            // Arrange
            var refreshToken = new RefreshToken
            {
                Token = "expired-token",
                CreatedOn = DateTime.UtcNow.AddDays(-10),
                ExpiresOn = DateTime.UtcNow.AddDays(-1)
            };

            var user = new User
            {
                Id = "user-1",
                UserName = "john",
                Email = "john@example.com",
                Role = UserRole.Customer,
                RefreshTokens = new List<RefreshToken>
                {
                    refreshToken
                }
            };

            SetupUsersQueryable(new List<User> { user });

            // Act
            var result = await service.RefreshTokenAsync(
                "expired-token");

            // Assert
            Assert.False(result.IsAuthenticated);
            Assert.Equal("Refresh token expired", result.Message);
        }

        [Fact]
        public async Task RefreshTokenAsync_revokedToken_returnsExpiredMessage()
        {
            // Arrange
            var refreshToken = new RefreshToken
            {
                Token = "revoked-token",
                CreatedOn = DateTime.UtcNow.AddDays(-1),
                ExpiresOn = DateTime.UtcNow.AddDays(6),
                RevokedOn = DateTime.UtcNow.AddHours(-1)
            };

            var user = new User
            {
                Id = "user-1",
                UserName = "john",
                Email = "john@example.com",
                Role = UserRole.Customer,
                RefreshTokens = new List<RefreshToken>
                {
                    refreshToken
                }
            };

            SetupUsersQueryable(new List<User> { user });

            // Act
            var result = await service.RefreshTokenAsync(
                "revoked-token");

            // Assert
            Assert.False(result.IsAuthenticated);
            Assert.Equal(
                "Refresh token expired",
                result.Message);
        }

        [Fact]
        public async Task RefreshTokenAsync_validToken_returnsNewTokens()
        {
            // Arrange
            var oldRefreshToken = new RefreshToken
            {
                Token = "old-token",
                CreatedOn = DateTime.UtcNow,
                ExpiresOn = DateTime.UtcNow.AddDays(7)
            };

            var user = new User
            {
                Id = "user-1",
                UserName = "john",
                Email = "john@example.com",
                Role = UserRole.Customer,
                RefreshTokens = new List<RefreshToken>
                {
                    oldRefreshToken
                }
            };

            SetupUsersQueryable(new List<User> { user });

            userManagerMock
                .Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await service.RefreshTokenAsync(
                "old-token");

            // Assert
            Assert.True(result.IsAuthenticated);
            Assert.NotNull(result.Token);
            Assert.NotNull(result.RefreshToken);

            Assert.Equal("john", result.UserName);
            Assert.Equal("john@example.com", result.Email);
            Assert.Equal("Customer", result.Role);

            Assert.Equal(
                oldRefreshToken.ExpiresOn.Date,
                result.RefreshTokenExpiration.Date);

            Assert.NotNull(oldRefreshToken.RevokedOn);

            Assert.Equal(
                2,
                user.RefreshTokens!.Count);

            Assert.NotEqual(
                "old-token",
                result.RefreshToken);

            userManagerMock.Verify(
                x => x.UpdateAsync(user),
                Times.Once);
        }

        // =========================================================
        // RevokeTokenAsync
        // =========================================================

        [Fact]
        public async Task RevokeTokenAsync_nullToken_returnsInvalidRefreshToken()
        {
            // Arrange
            string? token = null;

            // Act
            var result = await service.RevokeTokenAsync(token);

            // Assert
            Assert.False(result.IsAuthenticated);
            Assert.Equal(
                "Invalid refresh token",
                result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("invalid-token")]
        public async Task RevokeTokenAsync_invalidToken_returnsInvalidRefreshToken(
            string token)
        {
            // Arrange
            SetupUsersQueryable(new List<User>());

            // Act
            var result = await service.RevokeTokenAsync(token);

            // Assert
            Assert.False(result.IsAuthenticated);
            Assert.Equal(
                "Invalid refresh token",
                result.Message);
        }

        [Fact]
        public async Task RevokeTokenAsync_expiredToken_returnsExpiredMessage()
        {
            // Arrange
            var refreshToken = new RefreshToken
            {
                Token = "expired-token",
                CreatedOn = DateTime.UtcNow.AddDays(-10),
                ExpiresOn = DateTime.UtcNow.AddDays(-1)
            };

            var user = new User
            {
                Id = "user-1",
                UserName = "john",
                Email = "john@example.com",
                Role = UserRole.Customer,
                RefreshTokens = new List<RefreshToken>
                {
                    refreshToken
                }
            };

            SetupUsersQueryable(new List<User> { user });

            // Act
            var result = await service.RevokeTokenAsync(
                "expired-token");

            // Assert
            Assert.False(result.IsAuthenticated);
            Assert.Equal(
                "Refresh token expired",
                result.Message);
        }

        [Fact]
        public async Task RevokeTokenAsync_validToken_revokesToken()
        {
            // Arrange
            var refreshToken = new RefreshToken
            {
                Token = "valid-token",
                CreatedOn = DateTime.UtcNow,
                ExpiresOn = DateTime.UtcNow.AddDays(7)
            };

            var user = new User
            {
                Id = "user-1",
                UserName = "john",
                Email = "john@example.com",
                Role = UserRole.Customer,
                RefreshTokens = new List<RefreshToken>
                {
                    refreshToken
                }
            };

            SetupUsersQueryable(new List<User> { user });

            userManagerMock
                .Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await service.RevokeTokenAsync(
                "valid-token");

            // Assert
            Assert.False(result.IsAuthenticated);
            Assert.Equal(
                "Token revoked",
                result.Message);

            Assert.NotNull(refreshToken.RevokedOn);

            userManagerMock.Verify(
                x => x.UpdateAsync(user),
                Times.Once);
        }

        // =========================================================
        // Helpers
        // =========================================================

        private void SetupUsersQueryable(List<User> users)
        {
            var mockUsers = users.BuildMockDbSet();

            userManagerMock.Setup(x => x.Users)
                .Returns(mockUsers.Object);
        }
    }
}